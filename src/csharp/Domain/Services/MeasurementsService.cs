using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using REWApi.Infrastructure;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// REW Measurements management with high-level workflow methods
    /// Maps to /measurements/* endpoints plus common automation patterns
    /// </summary>
    public class MeasurementsService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ArrayCodec _arrayCodec;
        private readonly ILogger<MeasurementsService>? _logger;

        internal MeasurementsService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            ILogger<MeasurementsService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
            _arrayCodec = new ArrayCodec();
        }

        /// <summary>
        /// Complete measurement workflow: sweep + process + export
        /// Handles hardware setup, timing, and error recovery
        /// </summary>
        public async Task<MeasurementResult> RunSweepAndExport(
            SweepConfig config,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return await _commandGate.ExecuteCommand(async () =>
            {
                _logger?.LogInformation("Starting sweep measurement: {StartFreq}Hz - {EndFreq}Hz, {Length}k samples",
                    config.StartFreq, config.EndFreq, config.Length);

                // Start sweep measurement
                var sweepData = new
                {
                    startFreq = config.StartFreq,
                    endFreq = config.EndFreq,
                    length = config.Length,
                    level = config.Level,
                    repetitions = config.Repetitions,
                    startDelay = config.StartDelay,
                    captureNoiseFloor = config.CaptureNoiseFloor,
                    notes = config.Notes ?? string.Empty
                };

                var json = JsonSerializer.Serialize(sweepData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/sweep", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                string measurementId;
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted) // 202 - Long running
                {
                    var jobId = responseData.GetProperty("jobId").GetString()!;
                    _logger?.LogInformation("Sweep measurement started as job {JobId}", jobId);

                    // Create long-running job to poll for completion
                    var job = LongRunningJob.Create(
                        jobId,
                        "Sweep Measurement",
                        async (ct) =>
                        {
                            // Poll for job completion
                            var pollResponse = await _httpClient.GetAsync($"{_baseUrl}/jobs/{jobId}", ct);
                            pollResponse.EnsureSuccessStatusCode();

                            var pollJson = await pollResponse.Content.ReadAsStringAsync(ct);
                            var pollData = JsonSerializer.Deserialize<JsonElement>(pollJson);

                            if (pollData.GetProperty("status").GetString() == "completed")
                            {
                                return pollData.GetProperty("measurementId").GetString()!;
                            }
                            else if (pollData.GetProperty("status").GetString() == "failed")
                            {
                                var error = pollData.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
                                throw new REWJobException(jobId, "Sweep Measurement", error!);
                            }

                            // Still running, wait and try again
                            await Task.Delay(1000, ct);
                            return await PollJobCompletion(jobId, ct);
                        },
                        async () =>
                        {
                            try
                            {
                                await _httpClient.DeleteAsync($"{_baseUrl}/jobs/{jobId}");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "Failed to cancel job {JobId}", jobId);
                            }
                        },
                        _logger);

                    measurementId = await job.AwaitCompletion(cancellationToken);
                }
                else // 200 - Immediate completion
                {
                    measurementId = responseData.GetProperty("measurementId").GetString()!;
                    _logger?.LogInformation("Sweep measurement completed immediately: {MeasurementId}", measurementId);
                }

                // Get measurement summary
                var summary = await GetMeasurementSummary(measurementId, cancellationToken);

                // Export if path specified
                string? exportPath = null;
                if (!string.IsNullOrEmpty(config.OutputPath))
                {
                    await ExportMeasurement(measurementId, config.OutputPath, ExportFormat.MDAT, cancellationToken);
                    exportPath = config.OutputPath;
                    _logger?.LogInformation("Exported measurement {MeasurementId} to {Path}", measurementId, exportPath);
                }

                // Calculate basic metrics
                var metrics = await CalculateBasicMetrics(measurementId, cancellationToken);

                var result = new MeasurementResult
                {
                    MeasurementId = measurementId,
                    Summary = summary,
                    ExportPath = exportPath,
                    Metrics = metrics
                };

                _logger?.LogInformation("Completed sweep measurement workflow: {MeasurementId} (SNR: {SNR:F1}dB)",
                    measurementId, metrics.SNR);

                return result;
            }, "Run Sweep and Export", cancellationToken);
        }

        /// <summary>
        /// Batch process multiple measurements with common settings
        /// Efficient for room correction workflows
        /// </summary>
        public async Task<ProcessingResult[]> ProcessBatch(
            string[] measurementIds,
            ProcessingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("Measurement IDs cannot be null or empty", nameof(measurementIds));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _logger?.LogInformation("Starting batch processing of {Count} measurements", measurementIds.Length);

            var results = new List<ProcessingResult>();

            foreach (var measurementId in measurementIds)
            {
                try
                {
                    var result = await ProcessSingleMeasurement(measurementId, options, cancellationToken);
                    results.Add(result);
                    
                    _logger?.LogDebug("Processed measurement {MeasurementId}: {Status}", 
                        measurementId, result.Success ? "Success" : "Failed");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to process measurement {MeasurementId}", measurementId);
                    results.Add(new ProcessingResult
                    {
                        ProcessName = "Batch Processing",
                        Message = ex.Message,
                        Success = false
                    });
                }
            }

            var successCount = results.Count(r => r.Success);
            _logger?.LogInformation("Batch processing completed: {Success}/{Total} successful", 
                successCount, measurementIds.Length);

            return results.ToArray();
        }

        /// <summary>
        /// List all measurements with optional filtering
        /// Maps to GET /measurements
        /// </summary>
        public async Task<MeasurementSummary[]> ListMeasurements(
            MeasurementFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measurements";
            var queryParams = new List<string>();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.GroupId))
                    queryParams.Add($"groupId={Uri.EscapeDataString(filter.GroupId)}");
                if (filter.FromDate.HasValue)
                    queryParams.Add($"fromDate={filter.FromDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
                if (filter.ToDate.HasValue)
                    queryParams.Add($"toDate={filter.ToDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
                if (!string.IsNullOrEmpty(filter.TitleContains))
                    queryParams.Add($"titleContains={Uri.EscapeDataString(filter.TitleContains)}");
                if (filter.IsSelected.HasValue)
                    queryParams.Add($"isSelected={filter.IsSelected.Value.ToString().ToLowerInvariant()}");
            }

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var measurements = new List<MeasurementSummary>();
            if (data.TryGetProperty("measurements", out var measurementsArray))
            {
                foreach (var measurement in measurementsArray.EnumerateArray())
                {
                    measurements.Add(ParseMeasurementSummary(measurement));
                }
            }

            return measurements.ToArray();
        }

        /// <summary>
        /// Get full measurement data including arrays
        /// Maps to GET /measurements/:id with data decoding
        /// </summary>
        public async Task<MeasurementData> GetMeasurement(string measurementId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var response = await _httpClient.GetAsync($"{_baseUrl}/measurements/{measurementId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new MeasurementData
            {
                Summary = ParseMeasurementSummary(data),
                FrequencyResponse = data.TryGetProperty("frequencyResponse", out var freqResp) ? ParseFrequencyResponse(freqResp) : null,
                ImpulseResponse = data.TryGetProperty("impulseResponse", out var impResp) ? ParseImpulseResponse(impResp) : null
            };
        }

        /// <summary>
        /// Delete measurement
        /// Maps to DELETE /measurements/:id
        /// </summary>
        public async Task DeleteMeasurement(string measurementId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/measurements/{measurementId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                _logger?.LogInformation("Deleted measurement {MeasurementId}", measurementId);
            }, "Delete Measurement", cancellationToken);
        }

        /// <summary>
        /// Generate waterfall plot with progress tracking
        /// Maps to POST /measurements/:id/command "Generate waterfall"
        /// </summary>
        public async Task<LongRunningJob<WaterfallResult>> GenerateWaterfall(
            string measurementId,
            WaterfallOptions options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Generate waterfall",
                    parameters = new
                    {
                        mode = options.Mode.ToString(),
                        slices = options.Slices,
                        ppo = options.PPO,
                        windowType = options.WindowType.ToString(),
                        timeRangeMs = options.TimeRangeMs
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) // 200 - Immediate
                {
                    var result = ParseWaterfallResult(responseData, options);
                    return LongRunningJob.Completed(result, "Generate Waterfall");
                }
                else // 202 - Long running
                {
                    var jobId = responseData.GetProperty("jobId").GetString()!;
                    return LongRunningJob.Create(
                        jobId,
                        "Generate Waterfall",
                        async (ct) =>
                        {
                            var waterfallData = await PollJobResult<JsonElement>(jobId, ct);
                            return ParseWaterfallResult(waterfallData, options);
                        },
                        () => CancelJob(jobId),
                        _logger);
                }
            }, "Generate Waterfall", cancellationToken);
        }

        /// <summary>
        /// Generate RT60 analysis
        /// Maps to POST /measurements/:id/command "Generate RT60"
        /// </summary>
        public async Task<LongRunningJob<RT60Result>> GenerateRT60(
            string measurementId,
            RT60Options options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Generate RT60",
                    parameters = new
                    {
                        octaveFraction = options.OctaveFraction,
                        startFrequency = options.StartFrequency,
                        endFrequency = options.EndFrequency,
                        method = options.Method.ToString(),
                        enableSmoothing = options.EnableSmoothing
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) // 200 - Immediate
                {
                    var result = ParseRT60Result(responseData, options);
                    return LongRunningJob.Completed(result, "Generate RT60");
                }
                else // 202 - Long running
                {
                    var jobId = responseData.GetProperty("jobId").GetString()!;
                    return LongRunningJob.Create(
                        jobId,
                        "Generate RT60",
                        async (ct) =>
                        {
                            var rt60Data = await PollJobResult<JsonElement>(jobId, ct);
                            return ParseRT60Result(rt60Data, options);
                        },
                        () => CancelJob(jobId),
                        _logger);
                }
            }, "Generate RT60", cancellationToken);
        }

        /// <summary>
        /// Apply smoothing to measurement
        /// Maps to POST /measurements/:id/command "Smooth"
        /// </summary>
        public async Task<LongRunningJob<ProcessingResult>> Smooth(
            string measurementId,
            SmoothingOptions options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Smooth",
                    parameters = new
                    {
                        octaveFraction = options.OctaveFraction,
                        type = options.Type.ToString(),
                        applyToPhase = options.ApplyToPhase
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) // 200 - Immediate
                {
                    var result = ParseProcessingResult(responseData);
                    return LongRunningJob.Completed(result, "Smooth");
                }
                else // 202 - Long running
                {
                    var jobId = responseData.GetProperty("jobId").GetString()!;
                    return LongRunningJob.Create(
                        jobId,
                        "Smooth",
                        async (ct) =>
                        {
                            var processingData = await PollJobResult<JsonElement>(jobId, ct);
                            return ParseProcessingResult(processingData);
                        },
                        () => CancelJob(jobId),
                        _logger);
                }
            }, "Smooth Measurement", cancellationToken);
        }

        /// <summary>
        /// Get frequency response with decoded float arrays
        /// Maps to GET /measurements/:id/frequency-response
        /// </summary>
        public async Task<FrequencyResponseData> GetFrequencyResponse(
            string measurementId,
            string unit = "SPL",
            string? smoothing = null,
            int? ppo = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/frequency-response?unit={Uri.EscapeDataString(unit)}";
            if (!string.IsNullOrEmpty(smoothing))
                url += $"&smoothing={Uri.EscapeDataString(smoothing)}";
            if (ppo.HasValue)
                url += $"&ppo={ppo.Value}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Get impulse response with decoded data
        /// Maps to GET /measurements/:id/impulse-response
        /// </summary>
        public async Task<ImpulseResponseData> GetImpulseResponse(
            string measurementId,
            string unit = "Percent",
            bool windowed = false,
            bool normalised = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/impulse-response?unit={Uri.EscapeDataString(unit)}&windowed={windowed.ToString().ToLowerInvariant()}&normalised={normalised.ToString().ToLowerInvariant()}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseImpulseResponse(data);
        }

        /// <summary>
        /// Export measurement to file
        /// Maps to POST /measurements/:id/export
        /// </summary>
        public async Task ExportMeasurement(
            string measurementId,
            string filePath,
            ExportFormat format = ExportFormat.MDAT,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            await _commandGate.ExecuteCommand(async () =>
            {
                var exportData = new
                {
                    filePath,
                    format = format.ToString().ToUpperInvariant()
                };

                var json = JsonSerializer.Serialize(exportData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/export", content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }, "Export Measurement", cancellationToken);
        }

        // Internal helper methods

        private async Task<string> PollJobCompletion(string jobId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/jobs/{jobId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var status = data.GetProperty("status").GetString();
                
                if (status == "completed")
                {
                    return data.GetProperty("result").GetProperty("measurementId").GetString()!;
                }
                else if (status == "failed")
                {
                    var error = data.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
                    throw new REWJobException(jobId, "Job", error!);
                }

                await Task.Delay(1000, cancellationToken);
            }

            throw new OperationCanceledException();
        }

        private async Task<T> PollJobResult<T>(string jobId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/jobs/{jobId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var status = data.GetProperty("status").GetString();
                
                if (status == "completed")
                {
                    var result = data.GetProperty("result");
                    return JsonSerializer.Deserialize<T>(result.GetRawText())!;
                }
                else if (status == "failed")
                {
                    var error = data.TryGetProperty("error", out var err) ? err.GetString() : "Unknown error";
                    throw new REWJobException(jobId, "Job", error!);
                }

                await Task.Delay(1000, cancellationToken);
            }

            throw new OperationCanceledException();
        }

        private async Task CancelJob(string jobId)
        {
            try
            {
                await _httpClient.DeleteAsync($"{_baseUrl}/jobs/{jobId}");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to cancel job {JobId}", jobId);
            }
        }

        private async Task<MeasurementSummary> GetMeasurementSummary(string measurementId, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measurements/{measurementId}/summary", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseMeasurementSummary(data);
        }

        private async Task<ProcessingResult> ProcessSingleMeasurement(
            string measurementId, 
            ProcessingOptions options, 
            CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Apply smoothing if specified
                if (!string.IsNullOrEmpty(options.Smoothing))
                {
                    var smoothingOpts = new SmoothingOptions
                    {
                        OctaveFraction = double.Parse(options.Smoothing.Replace("1/", "").Replace("/", "")),
                        Type = SmoothingType.Octave
                    };
                    var smoothJob = await Smooth(measurementId, smoothingOpts, cancellationToken);
                    await smoothJob.AwaitCompletion(cancellationToken);
                }

                // Generate RT60 if requested
                if (options.GenerateRT60)
                {
                    var rt60Opts = new RT60Options
                    {
                        OctaveFraction = options.RT60OctaveFraction,
                        Method = RT60Method.T20
                    };
                    var rt60Job = await GenerateRT60(measurementId, rt60Opts, cancellationToken);
                    await rt60Job.AwaitCompletion(cancellationToken);
                }

                var processingTime = DateTime.UtcNow - startTime;

                return new ProcessingResult
                {
                    ProcessName = "Batch Processing",
                    Message = "Processing completed successfully",
                    Success = true,
                    ProcessingTime = processingTime,
                    Results = new Dictionary<string, string>
                    {
                        ["measurementId"] = measurementId,
                        ["processingTimeMs"] = processingTime.TotalMilliseconds.ToString("F0")
                    }
                };
            }
            catch (Exception ex)
            {
                var processingTime = DateTime.UtcNow - startTime;
                return new ProcessingResult
                {
                    ProcessName = "Batch Processing",
                    Message = ex.Message,
                    Success = false,
                    ProcessingTime = processingTime
                };
            }
        }

        private async Task<ProcessingMetrics> CalculateBasicMetrics(string measurementId, CancellationToken cancellationToken)
        {
            try
            {
                // Get frequency response to calculate basic metrics
                var freqResp = await GetFrequencyResponse(measurementId, "SPL", null, null, cancellationToken);
                
                var magnitudes = freqResp.Magnitudes;
                if (magnitudes.Length == 0)
                {
                    return new ProcessingMetrics { SNR = 0, THD = 0, PeakLevel = 0, RMSLevel = 0 };
                }

                var peakLevel = magnitudes.Max();
                var rmsLevel = Math.Sqrt(magnitudes.Select(m => m * m).Average());
                
                // Simplified SNR calculation (difference between peak and noise floor estimate)
                var sortedMags = magnitudes.OrderBy(m => m).ToArray();
                var noiseFloor = sortedMags.Take(sortedMags.Length / 10).Average(); // Bottom 10% as noise estimate
                var snr = peakLevel - noiseFloor;

                return new ProcessingMetrics
                {
                    SNR = snr,
                    THD = 0, // Would need harmonic analysis
                    PeakLevel = peakLevel,
                    RMSLevel = (float)rmsLevel
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to calculate metrics for measurement {MeasurementId}", measurementId);
                return new ProcessingMetrics();
            }
        }

        // Parsing methods

        private static MeasurementSummary ParseMeasurementSummary(JsonElement data)
        {
            return new MeasurementSummary
            {
                UUID = data.GetProperty("uuid").GetString() ?? string.Empty,
                Index = data.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0,
                Title = data.GetProperty("title").GetString() ?? string.Empty,
                Notes = data.TryGetProperty("notes", out var notes) ? notes.GetString() : null,
                Date = data.TryGetProperty("date", out var date) && date.TryGetDateTime(out var dateTime) 
                    ? dateTime : DateTime.MinValue,
                IsSelected = data.TryGetProperty("isSelected", out var sel) && sel.GetBoolean(),
                StartFreq = data.TryGetProperty("startFreq", out var startF) ? startF.GetDouble() : 0,
                EndFreq = data.TryGetProperty("endFreq", out var endF) ? endF.GetDouble() : 0,
                Inverted = data.TryGetProperty("inverted", out var inv) && inv.GetBoolean(),
                REWVersion = data.TryGetProperty("rewVersion", out var ver) ? ver.GetString() : null,
                TimingReference = data.TryGetProperty("timingReference", out var timing) ? timing.GetString() : null,
                Delay = data.TryGetProperty("delay", out var del) ? del.GetDouble() : 0,
                TimingOffset = data.TryGetProperty("timingOffset", out var offset) ? offset.GetDouble() : 0,
                SignalToNoisedB = data.TryGetProperty("signalToNoisedB", out var snr) ? snr.GetDouble() : 0,
                SPLOffsetdB = data.TryGetProperty("splOffsetdB", out var splOff) ? splOff.GetDouble() : 0,
                AlignSPLOffsetdB = data.TryGetProperty("alignSPLOffsetdB", out var alignOff) ? alignOff.GetDouble() : 0,
                Group = data.TryGetProperty("group", out var group) ? ParseGroupInfo(group) : null
            };
        }

        private static GroupInfo? ParseGroupInfo(JsonElement data)
        {
            return new GroupInfo
            {
                UUID = data.GetProperty("uuid").GetString() ?? string.Empty,
                Name = data.GetProperty("name").GetString() ?? string.Empty,
                Notes = data.TryGetProperty("notes", out var notes) ? notes.GetString() : null
            };
        }

        private FrequencyResponseData ParseFrequencyResponse(JsonElement data)
        {
            var magnitudeData = data.TryGetProperty("magnitudeData", out var magData) ? magData.GetString() : null;
            var phaseData = data.TryGetProperty("phaseData", out var phData) ? phData.GetString() : null;

            var result = new FrequencyResponseData
            {
                Smoothing = data.TryGetProperty("smoothing", out var smooth) ? smooth.GetString() ?? string.Empty : string.Empty,
                StartFrequency = data.TryGetProperty("startFrequency", out var startF) ? startF.GetDouble() : 0,
                PointsPerOctave = data.TryGetProperty("pointsPerOctave", out var ppo) ? ppo.GetInt32() : null,
                FrequencyStep = data.TryGetProperty("frequencyStep", out var step) ? step.GetDouble() : null,
                Magnitudes = !string.IsNullOrEmpty(magnitudeData) ? _arrayCodec.DecodeFloatArray(magnitudeData) : Array.Empty<float>(),
                Phases = !string.IsNullOrEmpty(phaseData) ? _arrayCodec.DecodeFloatArray(phaseData) : Array.Empty<float>()
            };

            // Calculate frequency array
            if (result.Magnitudes.Length > 0)
            {
                result.Frequencies = GenerateFrequencyArray(
                    result.StartFrequency,
                    result.Magnitudes.Length,
                    result.PointsPerOctave,
                    result.FrequencyStep);
            }

            return result;
        }

        private ImpulseResponseData ParseImpulseResponse(JsonElement data)
        {
            var responseData = data.TryGetProperty("data", out var irData) ? irData.GetString() : null;

            return new ImpulseResponseData
            {
                StartTime = data.TryGetProperty("startTime", out var startT) ? startT.GetDouble() : 0,
                TimingReference = data.TryGetProperty("timingReference", out var timing) ? timing.GetString() ?? string.Empty : string.Empty,
                SampleInterval = data.TryGetProperty("sampleInterval", out var interval) ? interval.GetDouble() : 0,
                SampleRate = data.TryGetProperty("sampleRate", out var rate) ? rate.GetInt32() : 0,
                Data = !string.IsNullOrEmpty(responseData) ? _arrayCodec.DecodeFloatArray(responseData) : Array.Empty<float>()
            };
        }

        private WaterfallResult ParseWaterfallResult(JsonElement data, WaterfallOptions options)
        {
            var frequenciesData = data.TryGetProperty("frequencies", out var freqs) ? freqs.GetString() : null;
            var timesData = data.TryGetProperty("times", out var times) ? times.GetString() : null;
            var splData = data.TryGetProperty("splData", out var spl) ? spl.GetString() : null;

            var frequencies = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeDoubleArray(frequenciesData) : Array.Empty<double>();
            var timeArray = !string.IsNullOrEmpty(timesData) ? _arrayCodec.DecodeDoubleArray(timesData) : Array.Empty<double>();
            
            // For 2D SPL data, we'd need a more complex decoding scheme
            var splArray = new float[timeArray.Length, frequencies.Length];
            if (!string.IsNullOrEmpty(splData))
            {
                var flatSpl = _arrayCodec.DecodeFloatArray(splData);
                int idx = 0;
                for (int t = 0; t < timeArray.Length && idx < flatSpl.Length; t++)
                {
                    for (int f = 0; f < frequencies.Length && idx < flatSpl.Length; f++)
                    {
                        splArray[t, f] = flatSpl[idx++];
                    }
                }
            }

            return new WaterfallResult
            {
                Frequencies = frequencies,
                Times = timeArray,
                DecodedSPLData = splArray,
                Mode = options.Mode,
                Slices = options.Slices,
                PPO = options.PPO
            };
        }

        private RT60Result ParseRT60Result(JsonElement data, RT60Options options)
        {
            var frequenciesData = data.TryGetProperty("frequencies", out var freqs) ? freqs.GetString() : null;
            var rt60Data = data.TryGetProperty("rt60Values", out var rt60) ? rt60.GetString() : null;
            var t20Data = data.TryGetProperty("t20Values", out var t20) ? t20.GetString() : null;
            var t30Data = data.TryGetProperty("t30Values", out var t30) ? t30.GetString() : null;
            var edtData = data.TryGetProperty("edtValues", out var edt) ? edt.GetString() : null;

            return new RT60Result
            {
                Frequencies = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeDoubleArray(frequenciesData) : Array.Empty<double>(),
                RT60Values = !string.IsNullOrEmpty(rt60Data) ? _arrayCodec.DecodeDoubleArray(rt60Data) : Array.Empty<double>(),
                T20Values = !string.IsNullOrEmpty(t20Data) ? _arrayCodec.DecodeDoubleArray(t20Data) : Array.Empty<double>(),
                T30Values = !string.IsNullOrEmpty(t30Data) ? _arrayCodec.DecodeDoubleArray(t30Data) : Array.Empty<double>(),
                EDTValues = !string.IsNullOrEmpty(edtData) ? _arrayCodec.DecodeDoubleArray(edtData) : Array.Empty<double>(),
                Method = options.Method,
                OctaveFraction = options.OctaveFraction
            };
        }

        private static ProcessingResult ParseProcessingResult(JsonElement data)
        {
            return new ProcessingResult
            {
                ProcessName = data.TryGetProperty("processName", out var procName) ? procName.GetString() ?? string.Empty : string.Empty,
                Message = data.TryGetProperty("message", out var msg) ? msg.GetString() ?? string.Empty : string.Empty,
                Success = data.TryGetProperty("success", out var success) ? success.GetBoolean() : true,
                Results = data.TryGetProperty("results", out var results) 
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(results.GetRawText()) ?? new Dictionary<string, string>()
                    : new Dictionary<string, string>()
            };
        }

        private static double[] GenerateFrequencyArray(double startFreq, int length, int? ppo, double? step)
        {
            var frequencies = new double[length];
            
            if (ppo.HasValue) // Log spacing
            {
                for (int i = 0; i < length; i++)
                {
                    frequencies[i] = startFreq * Math.Pow(2.0, (double)i / ppo.Value);
                }
            }
            else if (step.HasValue) // Linear spacing
            {
                for (int i = 0; i < length; i++)
                {
                    frequencies[i] = startFreq + i * step.Value;
                }
            }
            else // Default linear spacing
            {
                var defaultStep = (20000.0 - startFreq) / (length - 1);
                for (int i = 0; i < length; i++)
                {
                    frequencies[i] = startFreq + i * defaultStep;
                }
            }
            
            return frequencies;
        }
    }

    /// <summary>
    /// Complete measurement data including arrays
    /// </summary>
    public class MeasurementData
    {
        public MeasurementSummary Summary { get; set; } = new();
        public FrequencyResponseData? FrequencyResponse { get; set; }
        public ImpulseResponseData? ImpulseResponse { get; set; }
    }
}