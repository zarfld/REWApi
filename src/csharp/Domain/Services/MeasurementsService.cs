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
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var measurements = new List<MeasurementSummary>();
            
            // REW API returns object with numeric keys like: {"1": {...}, "2": {...}}
            // NOT an array with "measurements" property
            if (data.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in data.EnumerateObject())
                {
                    // Skip non-numeric keys if any
                    if (int.TryParse(property.Name, out _))
                    {
                        measurements.Add(ParseMeasurementSummary(property.Value));
                    }
                }
            }
            // Also support wrapped format {"measurements": [...]} if REW changes in future
            else if (data.TryGetProperty("measurements", out var measurementsArray))
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
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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
        /// Maps to DELETE /measurements/{id}
        /// </summary>
        /// <param name="measurementId">UUID of measurement to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted successfully, false if measurement not found</returns>
        public async Task<bool> DeleteMeasurement(string measurementId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/measurements/{measurementId}", cancellationToken);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning("Measurement {MeasurementId} not found for deletion", measurementId);
                    return false;
                }
                
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
                _logger?.LogInformation("Deleted measurement {MeasurementId}", measurementId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger?.LogWarning(ex, "Measurement {MeasurementId} not found for deletion", measurementId);
                return false;
            }
        }

        /// <summary>
        /// Delete all measurements from REW
        /// Maps to DELETE /measurements
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted successfully</returns>
        public async Task<bool> DeleteAllMeasurements(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/measurements", cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
                
                _logger?.LogInformation("Deleted all measurements");
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to delete all measurements");
                return false;
            }
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
                var commandData = new Dictionary<string, object>
                {
                    ["command"] = "Generate waterfall",
                    ["parameters"] = new Dictionary<string, string>
                    {
                        ["mode"] = options.Mode.ToString(),
                        ["slices"] = options.Slices.ToString(),
                        ["left window type"] = options.WindowType.ToString(),
                        ["right window type"] = options.WindowType.ToString(),
                        ["window width ms"] = "300",
                        ["time range ms"] = options.TimeRangeMs.ToString(),
                        ["rise time ms"] = "150",
                        ["use csd mode"] = "false",
                        ["ppo"] = options.PPO.ToString(),
                        ["smoothing"] = "None"
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) // 200 - Immediate
                {
                    // ProcessResult structure: {"processName": "...", "results": {"0": {...}}}
                    // Check if this is a ProcessResult with nested "results" -> "0" structure
                    if (responseData.TryGetProperty("results", out var results) && results.TryGetProperty("0", out var resultData))
                    {
                        var result = ParseWaterfallResult(resultData, options);
                        return LongRunningJob.Completed(result, "Generate Waterfall");
                    }
                    else
                    {
                        // Direct result structure - responseData IS the result
                        var result = ParseWaterfallResult(responseData, options);
                        return LongRunningJob.Completed(result, "Generate Waterfall");
                    }
                }
                else // 202 - Long running
                {
                    // AsyncStatus structure: {"jobId": "...", "status": "..."} OR {"message": "Generate waterfall ID 1234 in progress"}
                    string? jobId = null;
                    
                    if (responseData.TryGetProperty("jobId", out var jobIdElement))
                    {
                        jobId = jobIdElement.GetString();
                    }
                    else if (responseData.TryGetProperty("message", out var messageElement))
                    {
                        // Extract ID from message like "Generate waterfall ID 1119 in progress"
                        var message = messageElement.GetString() ?? "";
                        var match = System.Text.RegularExpressions.Regex.Match(message, @"ID (\d+)");
                        if (match.Success)
                        {
                            jobId = match.Groups[1].Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(jobId))
                    {
                        return LongRunningJob.Create(
                            jobId,
                            "Generate Waterfall",
                            async (ct) =>
                            {
                                var waterfallData = await PollJobResult<JsonElement>(jobId, ct);
                                // When polling completes, result will be in key "0"
                                if (waterfallData.TryGetProperty("0", out var resultData))
                                {
                                    return ParseWaterfallResult(resultData, options);
                                }
                                else
                                {
                                    return ParseWaterfallResult(waterfallData, options);
                                }
                            },
                            () => CancelJob(jobId),
                            _logger);
                    }
                    else
                    {
                        throw new REWApiException($"Unexpected response structure for Generate Waterfall command: {responseJson}", "/measurements/:id/command");
                    }
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

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }

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
        /// Generate Spectrogram analysis
        /// Maps to POST /measurements/:id/command "Generate spectrogram"
        /// </summary>
        public async Task<LongRunningJob<SpectrogramResult>> GenerateSpectrogram(
            string measurementId,
            SpectrogramOptions options,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return await _commandGate.ExecuteCommand(async () =>
            {
                // Convert mode enum to API string format
                string modeString = options.Mode switch
                {
                    SpectrogramMode.Fourier => "Fourier",
                    SpectrogramMode.Wavelet => "Wavelet",
                    SpectrogramMode.AiryCWT => "Airy CWT",
                    SpectrogramMode.MorletCWT => "Morlet CWT",
                    SpectrogramMode.BurstDecay => "Burst decay",
                    _ => options.Mode.ToString()
                };
                
                // Build parameters based on mode
                var parameters = new Dictionary<string, string>
                {
                    ["mode"] = modeString,
                    ["slices"] = options.Slices.ToString(),
                    ["amplitude"] = options.Amplitude,
                    ["ppo"] = options.PPO.ToString()
                };

                // Add mode-specific parameters
                switch (options.Mode)
                {
                    case SpectrogramMode.Fourier:
                        parameters["window type"] = options.WindowType.ToString();
                        parameters["window width ms"] = options.WindowWidthMs.ToString();
                        parameters["before ms"] = options.BeforeMs.ToString();
                        parameters["after ms"] = options.AfterMs.ToString();
                        break;
                        
                    case SpectrogramMode.BurstDecay:
                        parameters["bandwidth"] = options.Bandwidth;
                        parameters["periods"] = options.Periods.ToString();
                        break;
                        
                    case SpectrogramMode.Wavelet:
                    case SpectrogramMode.AiryCWT:
                    case SpectrogramMode.MorletCWT:
                        parameters["bandwidth"] = options.Bandwidth;
                        parameters["before ms"] = options.BeforeMs.ToString();
                        parameters["after ms"] = options.AfterMs.ToString();
                        break;
                }

                var commandData = new Dictionary<string, object>
                {
                    ["command"] = "Generate spectrogram",
                    ["parameters"] = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) // 200 - Immediate
                {
                    // ProcessResult structure: {"processName": "...", "results": {"0": {...}}}
                    // Check if this is a ProcessResult with nested "results" -> "0" structure
                    if (responseData.TryGetProperty("results", out var results) && results.TryGetProperty("0", out var resultData))
                    {
                        var result = ParseSpectrogramResult(resultData, options);
                        return LongRunningJob.Completed(result, "Generate Spectrogram");
                    }
                    else
                    {
                        // Direct result structure - responseData IS the result
                        var result = ParseSpectrogramResult(responseData, options);
                        return LongRunningJob.Completed(result, "Generate Spectrogram");
                    }
                }
                else // 202 - Long running
                {
                    // AsyncStatus structure: {"jobId": "...", "status": "..."} OR {"message": "Generate spectrogram ID 1234 in progress"}
                    string? jobId = null;
                    
                    if (responseData.TryGetProperty("jobId", out var jobIdElement))
                    {
                        jobId = jobIdElement.GetString();
                    }
                    else if (responseData.TryGetProperty("message", out var messageElement))
                    {
                        // Extract ID from message like "Generate spectrogram ID 1121 in progress"
                        var message = messageElement.GetString() ?? "";
                        var match = System.Text.RegularExpressions.Regex.Match(message, @"ID (\d+)");
                        if (match.Success)
                        {
                            jobId = match.Groups[1].Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(jobId))
                    {
                        return LongRunningJob.Create(
                            jobId,
                            "Generate Spectrogram",
                            async (ct) =>
                            {
                                var spectrogramData = await PollJobResult<JsonElement>(jobId, ct);
                                // When polling completes, result will be in key "0"
                                if (spectrogramData.TryGetProperty("0", out var resultData))
                                {
                                    return ParseSpectrogramResult(resultData, options);
                                }
                                else
                                {
                                    return ParseSpectrogramResult(spectrogramData, options);
                                }
                            },
                            () => CancelJob(jobId),
                            _logger);
                    }
                    else
                    {
                        throw new REWApiException($"Unexpected response structure for Generate Spectrogram command: {responseJson}", "/measurements/:id/command");
                    }
                }
            }, "Generate Spectrogram", cancellationToken);
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

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }

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

        #region Individual Measurement Commands (Phase 2)

        /// <summary>
        /// Save a single measurement to a specified file
        /// Maps to POST /measurements/:id/command "Save"
        /// Note: REW API requires filename parameter
        /// </summary>
        public async Task SaveMeasurement(
            string measurementId,
            string filename,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Save",
                    parameters = new { filename }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Save Measurement", cancellationToken);
        }

        /// <summary>
        /// Invert the polarity of a measurement (180 degree phase shift)
        /// Maps to POST /measurements/:id/command "Invert"
        /// </summary>
        public async Task Invert(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Invert"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Invert Measurement", cancellationToken);
        }

        /// <summary>
        /// Create a copy of the measurement response
        /// Maps to POST /measurements/:id/command "Response copy"
        /// </summary>
        public async Task ResponseCopy(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Response copy"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Response Copy", cancellationToken);
        }

        /// <summary>
        /// Create a copy of the measurement magnitude response only (without phase)
        /// Maps to POST /measurements/:id/command "Response magnitude copy"
        /// </summary>
        public async Task ResponseMagnitudeCopy(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Response magnitude copy"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Response Magnitude Copy", cancellationToken);
        }

        /// <summary>
        /// Wrap the phase response to -180/+180 degrees
        /// Maps to POST /measurements/:id/command "Wrap phase"
        /// </summary>
        public async Task WrapPhase(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Wrap phase"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Wrap Phase", cancellationToken);
        }

        /// <summary>
        /// Unwrap the phase response (remove discontinuities)
        /// Maps to POST /measurements/:id/command "Unwrap phase"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="frequency">Reference frequency where unwrapped phase will be within -180..180 degrees (must be within measurement range)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnwrapPhase(
            string measurementId,
            double frequency = 1000.0,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Unwrap phase",
                    parameters = new
                    {
                        frequency = frequency
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Unwrap Phase", cancellationToken);
        }

        /// <summary>
        /// Create a minimum phase version of the measurement
        /// Maps to POST /measurements/:id/command "Minimum phase version"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="includeCal">Include calibration data</param>
        /// <param name="appendLfTail">Append low frequency tail</param>
        /// <param name="appendHfTail">Append high frequency tail</param>
        /// <param name="lfTailStart">Low frequency tail start (Hz) - required if appendLfTail is true</param>
        /// <param name="lfTailSlope">Low frequency tail slope (dB/octave) - must be ≥ 0</param>
        /// <param name="hfTailStart">High frequency tail start (Hz) - required if appendHfTail is true</param>
        /// <param name="hfTailSlope">High frequency tail slope (dB/octave) - must be ≤ 0</param>
        /// <param name="replicateData">Replicate data - required if appendLfTail or appendHfTail is false</param>
        public async Task MinimumPhaseVersion(
            string measurementId,
            bool includeCal = true,
            bool appendLfTail = false,
            bool appendHfTail = false,
            double lfTailStart = 27.0,
            double lfTailSlope = 12.0,
            double hfTailStart = 15000.0,
            double hfTailSlope = -18.0,
            bool replicateData = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["include cal"] = includeCal,
                    ["append lf tail"] = appendLfTail,
                    ["append hf tail"] = appendHfTail
                };

                if (appendLfTail)
                {
                    parameters["lf tail start"] = lfTailStart;
                    parameters["lf tail slope"] = lfTailSlope;
                }

                if (appendHfTail)
                {
                    parameters["hf tail start"] = hfTailStart;
                    parameters["hf tail slope"] = hfTailSlope;
                }

                if (!appendLfTail && !appendHfTail)
                {
                    parameters["replicate data"] = replicateData;
                }

                var commandData = new
                {
                    command = "Minimum phase version",
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Minimum Phase Version", cancellationToken);
        }

        /// <summary>
        /// Create an excess phase version of the measurement
        /// Maps to POST /measurements/:id/command "Excess phase version"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="includeCal">Include calibration data</param>
        /// <param name="appendLfTail">Append low frequency tail</param>
        /// <param name="appendHfTail">Append high frequency tail</param>
        /// <param name="lfTailStart">Low frequency tail start (Hz) - required if appendLfTail is true</param>
        /// <param name="lfTailSlope">Low frequency tail slope (dB/octave) - must be ≥ 0</param>
        /// <param name="hfTailStart">High frequency tail start (Hz) - required if appendHfTail is true</param>
        /// <param name="hfTailSlope">High frequency tail slope (dB/octave) - must be ≤ 0</param>
        /// <param name="frequencyWarping">Use frequency warping for HF tail</param>
        /// <param name="replicateData">Replicate data - required if appendLfTail or appendHfTail is false</param>
        public async Task ExcessPhaseVersion(
            string measurementId,
            bool includeCal = true,
            bool appendLfTail = false,
            bool appendHfTail = false,
            double lfTailStart = 27.0,
            double lfTailSlope = 12.0,
            double hfTailStart = 15000.0,
            double hfTailSlope = -18.0,
            bool frequencyWarping = false,
            bool replicateData = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["include cal"] = includeCal,
                    ["append lf tail"] = appendLfTail,
                    ["append hf tail"] = appendHfTail
                };

                if (appendLfTail)
                {
                    parameters["lf tail start"] = lfTailStart;
                    parameters["lf tail slope"] = lfTailSlope;
                }

                if (appendHfTail)
                {
                    parameters["hf tail start"] = hfTailStart;
                    parameters["hf tail slope"] = hfTailSlope;
                    parameters["frequency warping"] = frequencyWarping;
                }

                if (!appendLfTail || !appendHfTail)
                {
                    parameters["replicate data"] = replicateData;
                }

                var commandData = new
                {
                    command = "Excess phase version",
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Excess Phase Version", cancellationToken);
        }

        /// <summary>
        /// Generate a minimum phase version of the measurement
        /// Maps to POST /measurements/:id/command "Generate minimum phase"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="includeCal">Include calibration data</param>
        /// <param name="appendLfTail">Append low frequency tail</param>
        /// <param name="appendHfTail">Append high frequency tail</param>
        /// <param name="lfTailStart">Low frequency tail start (Hz) - required if appendLfTail is true</param>
        /// <param name="lfTailSlope">Low frequency tail slope (dB/octave) - must be ≥ 0</param>
        /// <param name="hfTailStart">High frequency tail start (Hz) - required if appendHfTail is true</param>
        /// <param name="hfTailSlope">High frequency tail slope (dB/octave) - must be ≤ 0</param>
        /// <param name="frequencyWarping">Use frequency warping for HF tail</param>
        /// <param name="replicateData">Replicate data - required if appendLfTail or appendHfTail is false</param>
        public async Task GenerateMinimumPhase(
            string measurementId,
            bool includeCal = true,
            bool appendLfTail = false,
            bool appendHfTail = false,
            double lfTailStart = 27.0,
            double lfTailSlope = 12.0,
            double hfTailStart = 15000.0,
            double hfTailSlope = -18.0,
            bool frequencyWarping = false,
            bool replicateData = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["include cal"] = includeCal,
                    ["append lf tail"] = appendLfTail,
                    ["append hf tail"] = appendHfTail
                };

                if (appendLfTail)
                {
                    parameters["lf tail start"] = lfTailStart;
                    parameters["lf tail slope"] = lfTailSlope;
                }

                if (appendHfTail)
                {
                    parameters["hf tail start"] = hfTailStart;
                    parameters["hf tail slope"] = hfTailSlope;
                    parameters["frequency warping"] = frequencyWarping;
                }

                if (!appendLfTail || !appendHfTail)
                {
                    parameters["replicate data"] = replicateData;
                }

                var commandData = new
                {
                    command = "Generate minimum phase",
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Generate Minimum Phase", cancellationToken);
        }

        /// <summary>
        /// Trim the impulse response to the current window settings
        /// Maps to POST /measurements/:id/command "Trim IR to windows"
        /// </summary>
        public async Task TrimIRToWindows(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Trim IR to windows"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Trim IR to Windows", cancellationToken);
        }

        /// <summary>
        /// Add an SPL offset to the measurement
        /// Maps to POST /measurements/:id/command "Add SPL offset"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="offsetDb">The SPL offset value in dB</param>
        public async Task AddSPLOffset(
            string measurementId,
            double offsetDb,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Add SPL offset",
                    parameters = new
                    {
                        offset = offsetDb
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Add SPL Offset", cancellationToken);
        }

        /// <summary>
        /// Offset the time zero point of the measurement
        /// Maps to POST /measurements/:id/command "Offset t=0"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="offset">The time offset value</param>
        /// <param name="unit">The unit for the offset value: "seconds", "metres", "feet", or "samples"</param>
        public async Task OffsetTimeZero(
            string measurementId,
            double offset,
            string unit = "seconds",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Offset t=0",
                    parameters = new
                    {
                        offset = offset,
                        unit = unit
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Offset Time Zero", cancellationToken);
        }

        /// <summary>
        /// Estimate the impulse response delay
        /// Maps to POST /measurements/:id/command "Estimate IR delay"
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <returns>The estimated delay value in milliseconds</returns>
        public async Task<double> EstimateIRDelay(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Estimate IR delay"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
               if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Extract delay value from response
                if (responseData.TryGetProperty("delay", out var delayElement))
                {
                    return delayElement.GetDouble();
                }
                else if (responseData.TryGetProperty("result", out var resultElement) && 
                         resultElement.TryGetProperty("delay", out var nestedDelayElement))
                {
                    return nestedDelayElement.GetDouble();
                }
                else
                {
                    throw new REWApiException($"Unexpected response structure for Estimate IR delay command: {responseJson}", "/measurements/:id/command");
                }
            }, "Estimate IR Delay", cancellationToken);
        }

        /// <summary>
        /// Generate microphone in-box correction for a measurement
        /// Maps to POST /measurements/:id/command "Mic in box correction"
        /// </summary>
        public async Task GenerateMicInBoxCorrection(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Mic in box correction"  // Fixed: was "Generate Mic in box correction"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Mic In Box Correction", cancellationToken);
        }

        /// <summary>
        /// Merge calibration data into the impulse response
        /// Maps to POST /measurements/:id/command "Merge cal data to IR"
        /// </summary>
        public async Task MergeCalDataToIR(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Merge cal data to IR"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Merge Cal Data To IR", cancellationToken);
        }

        /// <summary>
        /// Generate equalised waterfall for a measurement
        /// Maps to POST /measurements/:id/command "Generate equalised waterfall"
        /// Note: This generates a waterfall with EQ applied
        /// Waterfall requires different parameters than spectrogram - see REW API docs
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="mode">Waterfall mode: "Fourier" or "Burst decay"</param>
        /// <param name="slices">Number of slices to generate</param>
        /// <param name="ppo">Frequency resolution (points per octave)</param>
        /// <param name="smoothing">Smoothing (e.g., "1/48")</param>
        /// <param name="leftWindowType">Left window type for Fourier mode (e.g., "Hann", "Tukey 0.25")</param>
        /// <param name="rightWindowType">Right window type for Fourier mode (e.g., "Hann", "Tukey 0.25")</param>
        /// <param name="windowWidthMs">Window width in milliseconds for Fourier mode</param>
        /// <param name="timeRangeMs">Time range in milliseconds for Fourier mode</param>
        /// <param name="riseTimeMs">Rise time in milliseconds for Fourier mode</param>
        /// <param name="useCsdMode">Use CSD (Cross-Spectral Density) mode for Fourier</param>
        /// <param name="bandwidth">Bandwidth for Burst decay mode (e.g., "1/3")</param>
        /// <param name="periods">Periods for Burst decay mode</param>
        public async Task GenerateEqualisedWaterfall(
            string measurementId,
            string mode = "Fourier",
            int slices = 101,
            int ppo = 48,
            string smoothing = "1/48",
            string leftWindowType = "Hann",
            string rightWindowType = "Tukey 0.25",
            double windowWidthMs = 300.0,
            double timeRangeMs = 500.0,
            double riseTimeMs = 150.0,
            bool useCsdMode = false,
            string bandwidth = "1/3",
            int periods = 30,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, string>
                {
                    ["mode"] = mode,
                    ["slices"] = slices.ToString(),
                    ["ppo"] = ppo.ToString(),
                    ["smoothing"] = smoothing
                };

                // Add mode-specific parameters
                if (mode == "Fourier")
                {
                    parameters["left window type"] = leftWindowType;
                    parameters["right window type"] = rightWindowType;
                    parameters["window width ms"] = windowWidthMs.ToString();
                    parameters["time range ms"] = timeRangeMs.ToString();
                    parameters["rise time ms"] = riseTimeMs.ToString();
                    parameters["use csd mode"] = useCsdMode.ToString().ToLower();
                }
                else if (mode == "Burst decay")
                {
                    parameters["bandwidth"] = bandwidth;
                    parameters["periods"] = periods.ToString();
                }

                var commandData = new
                {
                    command = "Generate equalised waterfall",
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Generate Equalised Waterfall", cancellationToken);
        }

        /// <summary>
        /// Generate equalised spectrogram for a measurement
        /// Maps to POST /measurements/:id/command "Generate equalised spectrogram"
        /// Note: This generates a spectrogram with EQ applied
        /// </summary>
        /// <param name="measurementId">The measurement ID</param>
        /// <param name="mode">Spectrogram mode: "Fourier", "Wavelet", "Airy CWT", "Morlet CWT", or "Burst decay"</param>
        /// <param name="slices">Number of slices to generate</param>
        /// <param name="ppo">Frequency resolution (points per octave)</param>
        /// <param name="amplitude">Amplitude scale: "Log (dB SPL)" or "Linear (% peak)"</param>
        /// <param name="windowType">Window type for Fourier mode (e.g., "Gaussian")</param>
        /// <param name="windowWidthMs">Window width in milliseconds for Fourier mode</param>
        /// <param name="beforeMs">Time before peak in milliseconds</param>
        /// <param name="afterMs">Time after peak in milliseconds</param>
        /// <param name="bandwidth">Bandwidth for wavelet modes (e.g., "1/3")</param>
        /// <param name="periods">Periods for Burst decay mode</param>
        public async Task GenerateEqualisedSpectrogram(
            string measurementId,
            string mode = "Fourier",
            int slices = 551,
            int ppo = 48,
            string amplitude = "Log (dB SPL)",
            string windowType = "Gaussian",
            double windowWidthMs = 300.0,
            double beforeMs = 50.0,
            double afterMs = 500.0,
            string bandwidth = "1/3",
            int periods = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["mode"] = mode,
                    ["slices"] = slices.ToString(),
                    ["ppo"] = ppo.ToString(),
                    ["amplitude"] = amplitude
                };

                // Add mode-specific parameters
                if (mode == "Fourier")
                {
                    parameters["window type"] = windowType;
                    parameters["window width ms"] = windowWidthMs.ToString();
                    parameters["before ms"] = beforeMs.ToString();
                    parameters["after ms"] = afterMs.ToString();
                }
                else if (mode == "Burst decay")
                {
                    parameters["bandwidth"] = bandwidth;
                    parameters["periods"] = periods.ToString();
                }
                else if (mode == "Wavelet" || mode == "Airy CWT" || mode == "Morlet CWT")
                {
                    parameters["bandwidth"] = bandwidth;
                    parameters["before ms"] = beforeMs.ToString();
                    parameters["after ms"] = afterMs.ToString();
                }

                var commandData = new
                {
                    command = "Generate equalised spectrogram",
                    parameters = parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/command", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
                }
            }, "Generate Equalised Spectrogram", cancellationToken);
        }

        #endregion

        #region General Measurement Commands (Global Operations)

        /// <summary>
        /// Save all measurements to a single file
        /// Maps to POST /measurements/command "Save all"
        /// </summary>
        /// <param name="filePath">Path to save the measurements file (supports .mdat format)</param>
        /// <param name="note">Optional note to save with the measurements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SaveAllMeasurements(
            string filePath,
            string? note = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = note != null 
                    ? new object[] { filePath, note }
                    : new object[] { filePath };

                var commandData = new
                {
                    command = "Save all",
                    parameters
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/command", content, cancellationToken);
             if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Save All Measurements", cancellationToken);
        }

        /// <summary>
        /// Load measurements from one or more files
        /// Maps to POST /measurements/command "Load"
        /// </summary>
        /// <param name="filePaths">Array of file paths to load (supports .mdat format)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task LoadMeasurements(
            string[] filePaths,
            CancellationToken cancellationToken = default)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("At least one file path must be provided", nameof(filePaths));

            foreach (var path in filePaths)
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException("File paths cannot be null or empty", nameof(filePaths));
            }

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Load",
                    parameters = filePaths
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/command", content, cancellationToken);
             if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Load Measurements", cancellationToken);
        }

        /// <summary>
        /// Generate a new measurement with a pure impulse (Dirac delta function)
        /// Maps to POST /measurements/command "Dirac"
        /// Creates a measurement containing a single impulse at the specified sample index
        /// </summary>
        /// <param name="sampleRate">Sample rate for the measurement (e.g., 48000 Hz)</param>
        /// <param name="totalSamples">Total number of samples for the response (e.g., 131072)</param>
        /// <param name="peakIndex">Zero-based index of the peak sample (e.g., 65536 for center)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task GenerateDiracMeasurement(
            int sampleRate,
            int totalSamples,
            int peakIndex,
            CancellationToken cancellationToken = default)
        {
            if (sampleRate <= 0)
                throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));
            if (totalSamples <= 0)
                throw new ArgumentException("Total samples must be positive", nameof(totalSamples));
            if (peakIndex < 0 || peakIndex >= totalSamples)
                throw new ArgumentException($"Peak index must be between 0 and {totalSamples - 1}", nameof(peakIndex));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Dirac",
                    parameters = new[] 
                    { 
                        sampleRate.ToString(), 
                        totalSamples.ToString(), 
                        peakIndex.ToString() 
                    }
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/command", content, cancellationToken);
              if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Generate Dirac Measurement", cancellationToken);
        }

        /// <summary>
        /// Sort all measurements alphabetically by name, then by date if names are the same
        /// Maps to POST /measurements/command "Sort alphabetically"
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SortMeasurementsAlphabetically(
            CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Sort alphabetically"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Sort Measurements Alphabetically", cancellationToken);
        }

        #endregion

        #region Batch Processing Commands (Process Multiple Measurements)

        /// <summary>
        /// Convert measurement IDs to the format required by the API.
        /// Handles both numeric indices (0-based, need +1) and UUID strings (pass through).
        /// REW API accepts string array - either numeric strings like "1", "2" or UUIDs.
        /// </summary>
        private string[] ConvertMeasurementIdsForApi(string[] measurementIds)
        {
            return measurementIds.Select(id =>
            {
                // If it's a numeric string (0-based index), convert to 1-based and return as string
                if (int.TryParse(id, out int index))
                {
                    return (index + 1).ToString(); // Convert to 1-based index string for REW API
                }
                // Otherwise it's a UUID - pass through as-is
                return id;
            }).ToArray();
        }

        /// <summary>
        /// Align SPL of multiple measurements to a target level or average
        /// Maps to POST /measurements/process-measurements with "Align SPL" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="targetDb">Target SPL in dB (e.g. "75.0") or "average" to align to average SPL</param>
        /// <param name="frequencyHz">Center frequency for alignment in Hz (e.g. 1000)</param>
        /// <param name="spanOctaves">Alignment span in octaves (e.g. 2)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task AlignSPL(
            string[] measurementIds,
            string targetDb,
            double frequencyHz,
            double spanOctaves,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));
            if (string.IsNullOrEmpty(targetDb))
                throw new ArgumentException("Target dB cannot be null or empty", nameof(targetDb));
            if (frequencyHz <= 0)
                throw new ArgumentException("Frequency must be positive", nameof(frequencyHz));
            if (spanOctaves <= 0)
                throw new ArgumentException("Span octaves must be positive", nameof(spanOctaves));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);

                var processData = new
                {
                    processName = "Align SPL",
                    measurementIndices = indices,
                    parameters = new
                    {
                        targetdB = targetDb,
                        frequencyHz = frequencyHz.ToString(),
                        spanOctaves = spanOctaves  // API expects number, not string
                    }
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
               if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Align SPL", cancellationToken);
        }

        /// <summary>
        /// Perform vector average of multiple measurements
        /// Maps to POST /measurements/process-measurements with "Vector average" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task VectorAverage(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Vector average",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Vector Average", cancellationToken);
        }

        /// <summary>
        /// Perform trace arithmetic on two measurements
        /// Maps to POST /measurements/process-measurements with "Arithmetic" process
        /// Common functions: "A + B", "A - B", "A * B", "A / B", "1 / A", etc.
        /// </summary>
        /// <param name="measurementIdA">First measurement ID (A)</param>
        /// <param name="measurementIdB">Second measurement ID (B)</param>
        /// <param name="function">Arithmetic function (e.g. "A + B", "A * B", "1 / A")</param>
        /// <param name="maxGain">Optional gain limit in dB for division/inversion (null = no limit)</param>
        /// <param name="lowerLimit">Optional lower frequency limit in Hz for band-limited operations</param>
        /// <param name="upperLimit">Optional upper frequency limit in Hz for band-limited operations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TraceArithmetic(
            string measurementIdA,
            string measurementIdB,
            string function,
            double? maxGain = null,
            double? lowerLimit = null,
            double? upperLimit = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementIdA))
                throw new ArgumentException("Measurement ID A cannot be null or empty", nameof(measurementIdA));
            if (string.IsNullOrEmpty(measurementIdB))
                throw new ArgumentException("Measurement ID B cannot be null or empty", nameof(measurementIdB));
            if (string.IsNullOrEmpty(function))
                throw new ArgumentException("Function cannot be null or empty", nameof(function));

            await _commandGate.ExecuteCommand(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    ["function"] = function
                };

                if (maxGain.HasValue)
                    parameters["maxGain"] = maxGain.Value.ToString();
                if (lowerLimit.HasValue)
                    parameters["lowerLimit"] = lowerLimit.Value.ToString();
                if (upperLimit.HasValue)
                    parameters["upperLimit"] = upperLimit.Value.ToString();

                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(new[] { measurementIdA, measurementIdB });
                
                var processData = new
                {
                    processName = "Arithmetic",
                    measurementIndices = indices,
                    parameters
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Trace Arithmetic", cancellationToken);
        }

        /// <summary>
        /// Apply smoothing to multiple measurements
        /// Maps to POST /measurements/process-measurements with "Smooth" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="smoothing">Smoothing value (e.g. "1/3", "1/6", "1/12")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SmoothBatch(
            string[] measurementIds,
            string smoothing = "1/3",
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));
            if (string.IsNullOrEmpty(smoothing))
                throw new ArgumentException("Smoothing cannot be null or empty", nameof(smoothing));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Smooth",
                    measurementIndices = indices,
                    parameters = new
                    {
                        smoothing
                    }
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Smooth Batch", cancellationToken);
        }

        /// <summary>
        /// Time align multiple measurements
        /// Maps to POST /measurements/process-measurements with "Time align" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task TimeAlign(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Time align",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Time Align", cancellationToken);
        }

        /// <summary>
        /// Align impulse response start of multiple measurements
        /// Maps to POST /measurements/process-measurements with "Align IR start" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task AlignIRStart(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Align IR start",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Align IR Start", cancellationToken);
        }

        /// <summary>
        /// Cross-correlation align multiple measurements
        /// Maps to POST /measurements/process-measurements with "Cross corr align" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task CrossCorrAlign(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Cross corr align",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Cross Correlation Align", cancellationToken);
        }

        /// <summary>
        /// RMS average multiple measurements
        /// Maps to POST /measurements/process-measurements with "RMS average" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task RMSAverage(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "RMS average",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "RMS Average", cancellationToken);
        }

        /// <summary>
        /// dB average multiple measurements
        /// Maps to POST /measurements/process-measurements with "dB average" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task DbAverage(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "dB average",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "dB Average", cancellationToken);
        }

        /// <summary>
        /// Magnitude plus phase average multiple measurements
        /// Maps to POST /measurements/process-measurements with "Magn plus phase average" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task MagnPlusPhaseAverage(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Magn plus phase average",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Magnitude Plus Phase Average", cancellationToken);
        }

        /// <summary>
        /// dB plus phase average multiple measurements
        /// Maps to POST /measurements/process-measurements with "dB plus phase average" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task DbPlusPhaseAverage(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "dB plus phase average",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "dB Plus Phase Average", cancellationToken);
        }

        /// <summary>
        /// Vector sum multiple measurements
        /// Maps to POST /measurements/process-measurements with "Vector sum" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task VectorSum(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Vector sum",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Vector Sum", cancellationToken);
        }

        /// <summary>
        /// Remove impulse response delays from multiple measurements
        /// Maps to POST /measurements/process-measurements with "Remove IR delays" process
        /// </summary>
        /// <param name="measurementIds">Array of measurement IDs (indices or UUIDs)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task RemoveIRDelays(
            string[] measurementIds,
            CancellationToken cancellationToken = default)
        {
            if (measurementIds == null || measurementIds.Length == 0)
                throw new ArgumentException("At least one measurement ID is required", nameof(measurementIds));

            await _commandGate.ExecuteCommand(async () =>
            {
                // Convert IDs: numeric (0-based) → 1-based int, UUIDs → string
                var indices = ConvertMeasurementIdsForApi(measurementIds);
                
                var processData = new
                {
                    processName = "Remove IR delays",
                    measurementIndices = indices
                };

                var json = JsonSerializer.Serialize(processData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/process-measurements", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Remove IR Delays", cancellationToken);
        }

        #endregion

        #region EQ Commands (Equalizer Operations)

        /// <summary>
        /// Calculate the target level for EQ matching
        /// Maps to POST /measurements/:id/eq/command with "Calculate target level"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task CalculateTargetLevel(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Calculate target level"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Calculate Target Level", cancellationToken);
        }

        /// <summary>
        /// Match the measurement to the target curve
        /// Maps to POST /measurements/:id/eq/command with "Match target"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task MatchTarget(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Match target"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Match Target", cancellationToken);
        }

        /// <summary>
        /// Optimize filter gains for the selected equalizer
        /// Maps to POST /measurements/:id/eq/command with "Optimise gains"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task OptimiseGains(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Optimise gains"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Optimise Gains", cancellationToken);
        }

        /// <summary>
        /// Optimize filter gains and Q values for the selected equalizer
        /// Maps to POST /measurements/:id/eq/command with "Optimise gains and Qs"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task OptimiseGainsAndQs(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Optimise gains and Qs"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Optimise Gains and Qs", cancellationToken);
        }

        /// <summary>
        /// Optimize filter gains, Q values and center frequencies for the selected equalizer
        /// Maps to POST /measurements/:id/eq/command with "Optimise gains, Qs and Fcs"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task OptimiseGainsQsAndFcs(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Optimise gains, Qs and Fcs"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Optimise Gains, Qs and Fcs", cancellationToken);
        }

        /// <summary>
        /// Generate a new measurement showing the predicted response with current EQ filters applied
        /// Maps to POST /measurements/:id/eq/command with "Generate predicted measurement"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task GeneratePredictedMeasurement(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Generate predicted measurement"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Generate Predicted Measurement", cancellationToken);
        }

        /// <summary>
        /// Generate a new measurement showing the filter response
        /// Maps to POST /measurements/:id/eq/command with "Generate filters measurement"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task GenerateFiltersMeasurement(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Generate filters measurement"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Generate Filters Measurement", cancellationToken);
        }

        /// <summary>
        /// Generate a new measurement showing the target curve
        /// Maps to POST /measurements/:id/eq/command with "Generate target measurement"
        /// </summary>
        /// <param name="measurementId">Measurement ID (index or UUID)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task GenerateTargetMeasurement(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            await _commandGate.ExecuteCommand(async () =>
            {
                var commandData = new
                {
                    command = "Generate target measurement"
                };

                var json = JsonSerializer.Serialize(commandData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/measurements/{measurementId}/eq/command", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Generate Target Measurement", cancellationToken);
        }

        #endregion

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
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            }, "Export Measurement", cancellationToken);
        }

        /// <summary>
        /// Get group delay data for a measurement
        /// Maps to GET /measurements/{id}/group-delay
        /// Returns FrequencyResponse object with group delay values in Magnitudes field (unit: seconds)
        /// </summary>
        public async Task<FrequencyResponseData> GetGroupDelay(
            string measurementId,
            string smoothing = "None",
            int? ppo = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var urlBuilder = new StringBuilder($"{_baseUrl}/measurements/{measurementId}/group-delay?smoothing={Uri.EscapeDataString(smoothing)}");
            if (ppo.HasValue)
                urlBuilder.Append($"&ppo={ppo.Value}");

            var response = await _httpClient.GetAsync(urlBuilder.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Get distortion analysis data for a measurement
        /// Maps to GET /measurements/{id}/distortion
        /// </summary>
        public async Task<DistortionData> GetDistortion(
            string measurementId,
            string unit = "%",
            int ppo = 12,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/distortion?unit={Uri.EscapeDataString(unit)}&ppo={ppo}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseDistortion(data, unit, ppo);
        }

        /// <summary>
        /// Get RT60 (reverberation time) analysis data for a measurement
        /// Maps to GET /measurements/{id}/rt60?octaveFrac={octaveFrac}
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="octaveFrac">Octave fraction: 1 for octave, 3 for one-third octave</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>RT60Result with frequencies and RT60/T20/T30/EDT values</returns>
        public async Task<RT60Result> GetRT60(
            string measurementId,
            int octaveFrac = 3,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            if (octaveFrac != 1 && octaveFrac != 3)
                throw new ArgumentException("Octave fraction must be 1 (octave) or 3 (one-third octave)", nameof(octaveFrac));

            var url = $"{_baseUrl}/measurements/{measurementId}/rt60?octaveFrac={octaveFrac}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseRT60(data, octaveFrac);
        }

        #region Phase 3a: Core EQ Settings Methods

        /// <summary>
        /// Get the equaliser assigned to a measurement
        /// Maps to GET /measurements/{id}/equaliser
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>EqualiserInfo with manufacturer and model</returns>
        public async Task<EqualiserInfo> GetMeasurementEqualiser(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/equaliser";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<EqualiserInfo>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            })!;
        }

        /// <summary>
        /// Set the equaliser for a measurement
        /// Maps to POST /measurements/{id}/equaliser
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="equaliserInfo">Equaliser manufacturer and model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementEqualiser(
            string measurementId,
            EqualiserInfo equaliserInfo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (equaliserInfo == null)
                throw new ArgumentNullException(nameof(equaliserInfo));

            var url = $"{_baseUrl}/measurements/{measurementId}/equaliser";
            var content = new StringContent(
                JsonSerializer.Serialize(equaliserInfo, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Get all filter settings for a measurement
        /// Maps to GET /measurements/{id}/filters
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of FilterSetting objects</returns>
        public async Task<FilterSetting[]> GetMeasurementFilters(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/filters";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            // Handle both response formats: {"value": [...], "Count": n} or just [...]
            if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("value", out var valueArray))
            {
                return JsonSerializer.Deserialize<FilterSetting[]>(valueArray.GetRawText(), new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                })!;
            }
            else if (data.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<FilterSetting[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                })!;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response format for filters endpoint: {data.ValueKind}");
            }
        }

        /// <summary>
        /// Update a single filter setting
        /// Maps to PUT /measurements/{id}/filters
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="filter">Complete filter setting to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementFilter(
            string measurementId,
            FilterSetting filter,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            var url = $"{_baseUrl}/measurements/{measurementId}/filters";
            var content = new StringContent(
                JsonSerializer.Serialize(filter, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Update multiple filter settings in a single call
        /// Maps to POST /measurements/{id}/filters
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="filters">Array of filter settings to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementFilters(
            string measurementId,
            FilterSetting[] filters,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (filters == null || filters.Length == 0)
                throw new ArgumentException("Filters array cannot be null or empty", nameof(filters));

            var url = $"{_baseUrl}/measurements/{measurementId}/filters";
            
            // REW API expects FilterList wrapper: { "filters": [...] }
            var filterList = new { filters = filters };
            var content = new StringContent(
                JsonSerializer.Serialize(filterList, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Get target settings for a measurement
        /// Maps to GET /measurements/{id}/target-settings
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>TargetSettings object</returns>
        public async Task<TargetSettings> GetMeasurementTargetSettings(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/target-settings";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TargetSettings>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            })!;
        }

        /// <summary>
        /// Update target settings for a measurement (partial update)
        /// Maps to PUT /measurements/{id}/target-settings
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="targetSettings">Target settings to update (partial fields allowed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementTargetSettings(
            string measurementId,
            TargetSettings targetSettings,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (targetSettings == null)
                throw new ArgumentNullException(nameof(targetSettings));

            var url = $"{_baseUrl}/measurements/{measurementId}/target-settings";
            var content = new StringContent(
                JsonSerializer.Serialize(targetSettings, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Get target level (SPL) for a measurement
        /// Maps to GET /measurements/{id}/target-level
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Target level in dB SPL</returns>
        public async Task<double> GetMeasurementTargetLevel(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/target-level";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<double>(json);
        }

        /// <summary>
        /// Set target level (SPL) for a measurement
        /// Maps to POST /measurements/{id}/target-level
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="targetLevel">Target level in dB SPL</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementTargetLevel(
            string measurementId,
            double targetLevel,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/target-level";
            var content = new StringContent(
                JsonSerializer.Serialize(targetLevel),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Phase 3b: EQ Commands

        /// <summary>
        /// Gets the list of available EQ commands that can be executed.
        /// Returns 8 commands: Calculate target level, Match target, Optimise gains,
        /// Optimise gains and Qs, Optimise gains, Qs and Fcs, Generate predicted measurement,
        /// Generate filters measurement, Generate target measurement.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of available EQ command strings</returns>
        public async Task<string[]> GetEQCommands(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measurements/eq/commands";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle response format: {"value": [...], "Count": 8} or just [...]
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueArray))
            {
                return JsonSerializer.Deserialize<string[]>(valueArray.GetRawText()) 
                    ?? Array.Empty<string>();
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<string[]>(json) 
                    ?? Array.Empty<string>();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response format: {root.ValueKind}");
            }
        }

        /// <summary>
        /// Executes an EQ command on a measurement (e.g., "Match target", "Optimise gains").
        /// Long-running operations return a progress ID that can be tracked via WebSocket subscription.
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="command">Command name (from GetEQCommands)</param>
        /// <param name="parameters">Optional command parameters (omit or pass null if no parameters needed)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command result with status message and optional operation ID</returns>
        public async Task<EQCommandResult> ExecuteEQCommand(
            string measurementId,
            string command,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var url = $"{_baseUrl}/measurements/{measurementId}/eq/command";
            
            // Build request body - only include parameters if provided
            object requestBody;
            if (parameters != null && parameters.Count > 0)
            {
                requestBody = new
                {
                    command,
                    parameters
                };
            }
            else
            {
                requestBody = new { command };
            }

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<EQCommandResult>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            }) ?? new EQCommandResult();
        }

        #endregion

        #region Phase 3c: Global EQ Defaults

        /// <summary>
        /// Gets the list of available equaliser profiles.
        /// Returns 50 built-in equaliser profiles from 33 manufacturers.
        /// </summary>
        /// <param name="manufacturer">Optional manufacturer name to filter results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of available equalisers</returns>
        public async Task<EqualiserInfo[]> GetAvailableEqualisers(
            string? manufacturer = null,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/equalisers";
            if (!string.IsNullOrEmpty(manufacturer))
            {
                url += $"?manufacturer={Uri.EscapeDataString(manufacturer)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle response format: {"value": [...], "Count": n} or just [...]
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueArray))
            {
                return JsonSerializer.Deserialize<EqualiserInfo[]>(valueArray.GetRawText(), new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }) ?? Array.Empty<EqualiserInfo>();
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<EqualiserInfo[]>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }) ?? Array.Empty<EqualiserInfo>();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response format: {root.ValueKind}");
            }
        }

        /// <summary>
        /// Gets the list of available equaliser manufacturers.
        /// Returns 33 manufacturer names.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of manufacturer names</returns>
        public async Task<string[]> GetManufacturers(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/manufacturers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle response format: {"value": [...], "Count": n} or just [...]
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueArray))
            {
                return JsonSerializer.Deserialize<string[]>(valueArray.GetRawText()) 
                    ?? Array.Empty<string>();
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<string[]>(json) 
                    ?? Array.Empty<string>();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response format: {root.ValueKind}");
            }
        }

        /// <summary>
        /// Gets the default equaliser profile used for new measurements.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Default equaliser information</returns>
        public async Task<EqualiserInfo> GetDefaultEqualiser(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/default-equaliser";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<EqualiserInfo>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            }) ?? new EqualiserInfo();
        }

        /// <summary>
        /// Sets the default equaliser profile for new measurements.
        /// </summary>
        /// <param name="equaliser">Equaliser information to set as default</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetDefaultEqualiser(
            EqualiserInfo equaliser,
            CancellationToken cancellationToken = default)
        {
            if (equaliser == null)
                throw new ArgumentNullException(nameof(equaliser));

            var url = $"{_baseUrl}/eq/default-equaliser";
            var content = new StringContent(
                JsonSerializer.Serialize(equaliser, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the default target curve settings for new measurements.
        /// Controls target shape (Full range, Subwoofer, LF, etc.), crossover settings, and slopes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Default target settings</returns>
        public async Task<TargetSettings> GetDefaultTargetSettings(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/default-target-settings";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TargetSettings>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            }) ?? new TargetSettings();
        }

        /// <summary>
        /// Updates the default target curve settings for new measurements.
        /// Can perform full replacement (POST) or partial update (PUT).
        /// </summary>
        /// <param name="targetSettings">Target settings to apply</param>
        /// <param name="partialUpdate">If true, performs PUT (partial update); if false, performs POST (full replacement)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateDefaultTargetSettings(
            TargetSettings targetSettings,
            bool partialUpdate = false,
            CancellationToken cancellationToken = default)
        {
            if (targetSettings == null)
                throw new ArgumentNullException(nameof(targetSettings));

            var url = $"{_baseUrl}/eq/default-target-settings";
            var content = new StringContent(
                JsonSerializer.Serialize(targetSettings, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                }),
                Encoding.UTF8,
                "application/json");

            var response = partialUpdate 
                ? await _httpClient.PutAsync(url, content, cancellationToken)
                : await _httpClient.PostAsync(url, content, cancellationToken);
            
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the default target level in dB SPL for new measurements.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Default target level in dB SPL</returns>
        public async Task<double> GetDefaultTargetLevel(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/default-target-level";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<double>(json);
        }

        /// <summary>
        /// Sets the default target level in dB SPL for new measurements.
        /// </summary>
        /// <param name="targetLevel">Target level in dB SPL (typical values: 75-85 dB)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetDefaultTargetLevel(
            double targetLevel,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/default-target-level";
            var content = new StringContent(
                JsonSerializer.Serialize(targetLevel),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Phase 3d - Room Curves (7 methods)

        /// <summary>
        /// Gets the default room curve settings for new measurements.
        /// Room curves add low frequency rise and high frequency fall to target curves.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Default room curve settings</returns>
        public async Task<RoomCurveSettings> GetDefaultRoomCurveSettings(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/default-room-curve-settings";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RoomCurveSettings>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize room curve settings");
        }

        /// <summary>
        /// Updates the default room curve settings for new measurements.
        /// </summary>
        /// <param name="roomCurveSettings">Room curve settings to apply</param>
        /// <param name="partialUpdate">If true, uses PUT for partial update; if false, uses POST for full replacement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateDefaultRoomCurveSettings(
            RoomCurveSettings roomCurveSettings,
            bool partialUpdate = false,
            CancellationToken cancellationToken = default)
        {
            if (roomCurveSettings == null)
                throw new ArgumentNullException(nameof(roomCurveSettings));

            var url = $"{_baseUrl}/eq/default-room-curve-settings";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var content = new StringContent(
                JsonSerializer.Serialize(roomCurveSettings, options),
                Encoding.UTF8,
                "application/json");

            var response = partialUpdate
                ? await _httpClient.PutAsync(url, content, cancellationToken)
                : await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the room curve settings for a specific measurement.
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Measurement's room curve settings</returns>
        public async Task<RoomCurveSettings> GetMeasurementRoomCurveSettings(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/room-curve-settings";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RoomCurveSettings>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize room curve settings");
        }

        /// <summary>
        /// Updates the room curve settings for a specific measurement.
        /// </summary>
        /// <param name="measurementId">Measurement UUID</param>
        /// <param name="roomCurveSettings">Room curve settings to apply</param>
        /// <param name="partialUpdate">If true, uses PUT for partial update; if false, uses POST for full replacement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMeasurementRoomCurveSettings(
            string measurementId,
            RoomCurveSettings roomCurveSettings,
            bool partialUpdate = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (roomCurveSettings == null)
                throw new ArgumentNullException(nameof(roomCurveSettings));

            var url = $"{_baseUrl}/measurements/{measurementId}/room-curve-settings";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var content = new StringContent(
                JsonSerializer.Serialize(roomCurveSettings, options),
                Encoding.UTF8,
                "application/json");

            var response = partialUpdate
                ? await _httpClient.PutAsync(url, content, cancellationToken)
                : await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the current house curve file path.
        /// House curves are user-defined target curves loaded from text files.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Full path to house curve file, or empty string if no house curve is set</returns>
        public async Task<string> GetHouseCurve(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/house-curve";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<string>(json) ?? string.Empty;
        }

        /// <summary>
        /// Sets the house curve file path for target curve generation.
        /// </summary>
        /// <param name="filePath">Full path to house curve text file (e.g., "C:\\curves\\my-curve.txt")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetHouseCurve(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            var url = $"{_baseUrl}/eq/house-curve";
            var content = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Clears the current house curve setting (resets to no house curve).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ClearHouseCurve(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/house-curve";
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Phase 3e - Match Target Settings (2 methods)

        /// <summary>
        /// Gets the match target settings that control the filter matching algorithm.
        /// These settings determine how REW's "Match target response" EQ command behaves.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current match target algorithm settings</returns>
        public async Task<MatchTargetSettings> GetMatchTargetSettings(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/match-target-settings";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<MatchTargetSettings>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize match target settings");
        }

        /// <summary>
        /// Updates the match target settings that control the filter matching algorithm.
        /// </summary>
        /// <param name="matchTargetSettings">Match target settings to apply</param>
        /// <param name="partialUpdate">If true, uses PUT for partial update; if false, uses POST for full replacement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateMatchTargetSettings(
            MatchTargetSettings matchTargetSettings,
            bool partialUpdate = false,
            CancellationToken cancellationToken = default)
        {
            if (matchTargetSettings == null)
                throw new ArgumentNullException(nameof(matchTargetSettings));

            var url = $"{_baseUrl}/eq/match-target-settings";
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var content = new StringContent(
                JsonSerializer.Serialize(matchTargetSettings, options),
                Encoding.UTF8,
                "application/json");

            var response = partialUpdate
                ? await _httpClient.PutAsync(url, content, cancellationToken)
                : await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the list of available target shapes for EQ configuration.
        /// Target shapes include options like "Full range", "Subwoofer only", "LF only", etc.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of target shape names</returns>
        public async Task<string[]> GetTargetShapes(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/target-shapes";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the list of available crossover types for target curve configuration.
        /// Crossover types define how the target curve transitions between frequency ranges.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of crossover type names</returns>
        public async Task<string[]> GetCrossoverTypes(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/crossover-types";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the list of available slopes (in dB per octave) for target curve configuration.
        /// Common values include 6, 12, 18, 24, 36, 48 dB/octave.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of slope values in dB/octave</returns>
        public async Task<int[]> GetSlopes(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/slopes";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
        }

        /// <summary>
        /// Gets the house curve logarithmic interpolation flag.
        /// When true, house curve data is interpolated logarithmically; when false, linearly.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if logarithmic interpolation is enabled, false otherwise</returns>
        public async Task<bool> GetHouseCurveLogInterpolation(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/house-curve-log-interpolation";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<bool>(json);
        }

        /// <summary>
        /// Sets the house curve logarithmic interpolation flag.
        /// </summary>
        /// <param name="useLogInterpolation">True to enable logarithmic interpolation, false for linear</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetHouseCurveLogInterpolation(
            bool useLogInterpolation,
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/house-curve-log-interpolation";
            var content = new StringContent(
                JsonSerializer.Serialize(useLogInterpolation),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Subscribes to EQ progress updates for monitoring EQ matching operations.
        /// The subscriber will receive notifications about EQ processing progress.
        /// </summary>
        /// <param name="subscriberUrl">URL where progress updates should be sent</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SubscribeToEQProgress(
            string subscriberUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/eq/subscribe";
            var subscriber = new { url = subscriberUrl };
            var content = new StringContent(
                JsonSerializer.Serialize(subscriber),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Unsubscribes from EQ progress updates.
        /// </summary>
        /// <param name="subscriberUrl">URL of the subscriber to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromEQProgress(
            string subscriberUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/eq/unsubscribe";
            var subscriber = new { url = subscriberUrl };
            var content = new StringContent(
                JsonSerializer.Serialize(subscriber),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the list of current EQ progress subscribers.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs</returns>
        public async Task<string[]> GetEQProgressSubscribers(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/eq/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle array of subscriber objects with "url" property
            if (root.ValueKind == JsonValueKind.Array)
            {
                var subscribers = new List<string>();
                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("url", out var urlProp))
                    {
                        var urlValue = urlProp.GetString();
                        if (!string.IsNullOrEmpty(urlValue))
                        {
                            subscribers.Add(urlValue);
                        }
                    }
                }
                return subscribers.ToArray();
            }

            return Array.Empty<string>();
        }

        #endregion

        #region Phase 3f: EQ Response Data (GZIP-compressed endpoints)

        /// <summary>
        /// Gets the target response curve for a measurement
        /// Maps to GET /measurements/{id}/target-response
        /// Returns GZIP-compressed frequency response data representing the target curve
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Target response curve data</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization or decompression fails</exception>
        public async Task<FrequencyResponseData> GetTargetResponse(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/target-response";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Gets the predicted frequency response with EQ filters applied
        /// Maps to GET /measurements/{id}/eq/frequency-response
        /// Returns GZIP-compressed frequency response data showing the measurement with EQ applied
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="unit">Unit for magnitude data (default: SPL)</param>
        /// <param name="smoothing">Smoothing to apply (default: None)</param>
        /// <param name="ppo">Points per octave for log spacing (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>EQ'd frequency response data</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization or decompression fails</exception>
        public async Task<FrequencyResponseData> GetEQFrequencyResponse(
            string measurementId,
            string unit = "SPL",
            string smoothing = "None",
            int? ppo = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var urlBuilder = new StringBuilder($"{_baseUrl}/measurements/{measurementId}/eq/frequency-response?unit={Uri.EscapeDataString(unit)}&smoothing={Uri.EscapeDataString(smoothing)}");
            if (ppo.HasValue)
                urlBuilder.Append($"&ppo={ppo.Value}");

            var response = await _httpClient.GetAsync(urlBuilder.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Gets the predicted group delay with EQ filters applied
        /// Maps to GET /measurements/{id}/eq/group-delay
        /// Returns GZIP-compressed group delay data showing the measurement with EQ applied
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="smoothing">Smoothing to apply (default: None)</param>
        /// <param name="ppo">Points per octave for log spacing (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>EQ'd group delay data</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization or decompression fails</exception>
        public async Task<FrequencyResponseData> GetEQGroupDelay(
            string measurementId,
            string smoothing = "None",
            int? ppo = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var urlBuilder = new StringBuilder($"{_baseUrl}/measurements/{measurementId}/eq/group-delay?smoothing={Uri.EscapeDataString(smoothing)}");
            if (ppo.HasValue)
                urlBuilder.Append($"&ppo={ppo.Value}");

            var response = await _httpClient.GetAsync(urlBuilder.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Gets the predicted impulse response with EQ filters applied
        /// Maps to GET /measurements/{id}/eq/impulse-response
        /// Returns GZIP-compressed impulse response data showing the measurement with EQ applied
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="unit">Unit for data (default: Percent)</param>
        /// <param name="windowed">Whether to return only windowed portion (default: false)</param>
        /// <param name="normalised">Whether to normalize data (default: true)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>EQ'd impulse response data</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization or decompression fails</exception>
        public async Task<ImpulseResponseData> GetEQImpulseResponse(
            string measurementId,
            string unit = "Percent",
            bool windowed = false,
            bool normalised = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var urlBuilder = new StringBuilder($"{_baseUrl}/measurements/{measurementId}/eq/impulse-response?unit={Uri.EscapeDataString(unit)}");
            if (windowed)
                urlBuilder.Append("&windowed=true");
            if (!normalised)
                urlBuilder.Append("&normalised=false");

            var response = await _httpClient.GetAsync(urlBuilder.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseImpulseResponse(data);
        }

        #endregion

        #region Phase 4: Additional Measurement Data Endpoints

        /// <summary>
        /// Gets the EQ'd (processed) frequency response for a measurement
        /// Maps to GET /measurements/{id}/eq/frequency-response
        /// Returns frequency response data after all EQ filters have been applied
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="unit">Unit for magnitude data (default: SPL)</param>
        /// <param name="smoothing">Smoothing to apply (default: None)</param>
        /// <param name="ppo">Points per octave for log spacing (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>EQ'd frequency response data</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization fails</exception>
        public async Task<FrequencyResponseData> GetProcessedFrequencyResponse(
            string measurementId,
            string unit = "SPL",
            string smoothing = "None",
            int? ppo = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var urlBuilder = new StringBuilder($"{_baseUrl}/measurements/{measurementId}/eq/frequency-response?unit={Uri.EscapeDataString(unit)}&smoothing={Uri.EscapeDataString(smoothing)}");
            if (ppo.HasValue)
                urlBuilder.Append($"&ppo={ppo.Value}");

            var response = await _httpClient.GetAsync(urlBuilder.ToString(), cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseFrequencyResponse(data);
        }

        /// <summary>
        /// Gets the impulse response of the measurement's filter set
        /// Maps to GET /measurements/{id}/filters-impulse-response
        /// Returns the impulse response that would result from applying the measurement's filters
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="sampleRate">Sample rate for the impulse response (Hz)</param>
        /// <param name="length">Length of the impulse response in samples (max: 4194304)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Filter impulse response data</returns>
        /// <exception cref="ArgumentException">If measurementId is null/empty or parameters invalid</exception>
        /// <exception cref="InvalidOperationException">If deserialization fails</exception>
        public async Task<ImpulseResponseData> GetFiltersImpulseResponse(
            string measurementId,
            int sampleRate = 48000,
            int length = 131072,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (sampleRate <= 0)
                throw new ArgumentException("Sample rate must be greater than 0", nameof(sampleRate));
            if (length <= 0 || length > 4194304)
                throw new ArgumentException("Length must be between 1 and 4194304 samples", nameof(length));

            var url = $"{_baseUrl}/measurements/{measurementId}/filters-impulse-response?samplerate={sampleRate}&length={length}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseImpulseResponse(data);
        }

        /// <summary>
        /// Gets waterfall plot data for a measurement
        /// Maps to GET /measurements/{id}/waterfall
        /// Returns GZIP-compressed waterfall data if available (requires prior generation)
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <summary>
        /// Gets the list of available commands for a measurement
        /// Maps to GET /measurements/{id}/commands
        /// Returns array of command names that can be issued to this measurement
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of available command names</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty</exception>
        /// <exception cref="InvalidOperationException">If deserialization fails</exception>
        public async Task<string[]> GetCommands(
            string measurementId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/commands";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var commands = JsonSerializer.Deserialize<string[]>(json);

            return commands ?? Array.Empty<string>();
        }

        /// <summary>
        /// Updates a measurement's metadata (title and/or notes)
        /// Maps to PUT /measurements/{id}
        /// Allows changing the measurement's name and description
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="title">New title for the measurement (optional)</param>
        /// <param name="notes">New notes for the measurement (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        /// <exception cref="ArgumentException">If measurementId is null or empty, or both title and notes are null</exception>
        /// <exception cref="InvalidOperationException">If update fails</exception>
        public async Task UpdateMeasurement(
            string measurementId,
            string? title = null,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (title == null && notes == null)
                throw new ArgumentException("At least one of title or notes must be provided", nameof(title));

            var updateData = new Dictionary<string, string>();
            if (title != null) updateData["title"] = title;
            if (notes != null) updateData["notes"] = notes;

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var url = $"{_baseUrl}/measurements/{measurementId}";
            var response = await _httpClient.PutAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the UUID of the currently selected measurement in REW
        /// Maps to GET /measurements/selected-uuid
        /// Returns the UUID of the selected measurement, or null if no measurement is selected
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UUID of selected measurement, or null if none selected</returns>
        public async Task<string?> GetSelectedMeasurementId(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measurements/selected-uuid";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            
            // API may return empty string or null if no measurement is selected
            if (data.ValueKind == JsonValueKind.String)
            {
                var uuid = data.GetString();
                return string.IsNullOrEmpty(uuid) ? null : uuid;
            }
            
            return null;
        }

        /// <summary>
        /// Selects a measurement in REW by its UUID
        /// Maps to POST /measurements/selected-uuid
        /// Sets the specified measurement as the currently selected measurement
        /// </summary>
        /// <param name="measurementId">UUID of the measurement to select</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentException">Thrown when measurementId is null or empty</exception>
        public async Task SetSelectedMeasurementId(string measurementId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/selected-uuid";
            var content = new StringContent($"\"{measurementId}\"", System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Gets the impulse response window settings for a measurement
        /// Maps to GET /measurements/{id}/ir-windows
        /// Returns the current IR window configuration including window types, widths, and FDW settings
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IRWindows object with current settings</returns>
        /// <exception cref="ArgumentException">Thrown when measurementId is null or empty</exception>
        public async Task<IRWindows> GetIRWindows(string measurementId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));

            var url = $"{_baseUrl}/measurements/{measurementId}/ir-windows";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var windows = JsonSerializer.Deserialize<IRWindows>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return windows ?? new IRWindows();
        }

        /// <summary>
        /// Sets the impulse response window settings for a measurement (replaces all settings)
        /// Maps to POST /measurements/{id}/ir-windows
        /// Completely replaces the current IR window configuration
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="windows">IRWindows object with new settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentException">Thrown when measurementId is null/empty or windows is null</exception>
        public async Task SetIRWindows(string measurementId, IRWindows windows, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (windows == null)
                throw new ArgumentNullException(nameof(windows));
            
            // Normalize FDW cycles: if 0 (invalid), set to default 15
            // API requires FDW cycles to be 1-35 even when FDW is not enabled
            if (windows.FdwWidthCycles == 0.0)
            {
                windows.FdwWidthCycles = 15.0;
            }
            
            if (!windows.IsValid())
                throw new ArgumentException("Invalid IR window settings", nameof(windows));

            var json = JsonSerializer.Serialize(windows, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var url = $"{_baseUrl}/measurements/{measurementId}/ir-windows";
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Updates the impulse response window settings for a measurement (partial update)
        /// Maps to PUT /measurements/{id}/ir-windows
        /// Updates only the provided settings, leaving others unchanged
        /// </summary>
        /// <param name="measurementId">UUID of the measurement</param>
        /// <param name="windows">IRWindows object with settings to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ArgumentException">Thrown when measurementId is null/empty or windows is null</exception>
        public async Task UpdateIRWindows(string measurementId, IRWindows windows, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(measurementId))
                throw new ArgumentException("Measurement ID cannot be null or empty", nameof(measurementId));
            if (windows == null)
                throw new ArgumentNullException(nameof(windows));
            
            // Normalize FDW cycles: if 0 (invalid), set to default 15
            // API requires FDW cycles to be 1-35 even when FDW is not enabled
            if (windows.FdwWidthCycles == 0.0)
            {
                windows.FdwWidthCycles = 15.0;
            }
            
            if (!windows.IsValid())
                throw new ArgumentException("Invalid IR window settings", nameof(windows));

            var json = JsonSerializer.Serialize(windows, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var url = $"{_baseUrl}/measurements/{measurementId}/ir-windows";
            var response = await _httpClient.PutAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        // Internal helper methods

        private async Task<string> PollJobCompletion(string jobId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/jobs/{jobId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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

        private async Task<T> PollJobResult<T>(string processId, CancellationToken cancellationToken)
        {
            // According to REW API docs, process results are polled via /measurements/process-result?processId={id}
            // The ProcessResult structure has: processName, message, results (map of result names to values)
            // For Generate Waterfall/Spectrogram, the data is in results["0"]
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{_baseUrl}/measurements/process-result?processId={processId}", cancellationToken);
                    
                    // 404 means result not ready yet, continue polling
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var processResult = JsonSerializer.Deserialize<JsonElement>(json);

                    // Check if we have results
                    if (processResult.TryGetProperty("results", out var results))
                    {
                        // Return the entire results object as a JsonElement for custom parsing
                        return JsonSerializer.Deserialize<T>(results.GetRawText())!;
                    }

                    // Check for error message
                    if (processResult.TryGetProperty("message", out var message))
                    {
                        var msg = message.GetString();
                        if (!string.IsNullOrEmpty(msg) && (msg.Contains("failed") || msg.Contains("error")))
                        {
                            throw new REWJobException(processId, "Process", msg);
                        }
                    }

                    // Result not ready, continue polling
                    await Task.Delay(1000, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // 404 means result not ready yet
                    await Task.Delay(1000, cancellationToken);
                }
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
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

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
            // REW API uses "magnitude" and "phase", not "magnitudeData" and "phaseData"
            var magnitudeData = data.TryGetProperty("magnitude", out var magData) ? magData.GetString() : null;
            var phaseData = data.TryGetProperty("phase", out var phData) ? phData.GetString() : null;

            var result = new FrequencyResponseData
            {
                Smoothing = data.TryGetProperty("smoothing", out var smooth) ? smooth.GetString() ?? string.Empty : string.Empty,
                // REW uses "startFreq" and "freqStep", not "startFrequency" and "frequencyStep"
                StartFrequency = data.TryGetProperty("startFreq", out var startF) ? startF.GetDouble() : 0,
                PointsPerOctave = data.TryGetProperty("pointsPerOctave", out var ppo) ? ppo.GetInt32() : null,
                FrequencyStep = data.TryGetProperty("freqStep", out var step) ? step.GetDouble() : null,
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

            // Parse sample rate - REW returns it as double, but we store as int
            // Try both "sampleRate" (camelCase) and "samplerate" (lowercase) for compatibility
            int sampleRate = 0;
            if (data.TryGetProperty("sampleRate", out var rate) || data.TryGetProperty("samplerate", out rate))
            {
                if (rate.ValueKind == JsonValueKind.Number)
                {
                    sampleRate = rate.TryGetInt32(out var intVal) ? intVal : (int)rate.GetDouble();
                }
            }

            return new ImpulseResponseData
            {
                StartTime = data.TryGetProperty("startTime", out var startT) ? startT.GetDouble() : 0,
                TimingReference = data.TryGetProperty("timingReference", out var timing) ? timing.GetString() ?? string.Empty : string.Empty,
                SampleInterval = data.TryGetProperty("sampleInterval", out var interval) ? interval.GetDouble() : 0,
                SampleRate = sampleRate,
                Data = !string.IsNullOrEmpty(responseData) ? _arrayCodec.DecodeFloatArray(responseData) : Array.Empty<float>()
            };
        }

        private WaterfallResult ParseWaterfallResult(JsonElement data, WaterfallOptions options)
        {
            // REW API returns property names with capital letters
            var frequenciesData = data.TryGetProperty("Frequencies", out var freqs) ? freqs.GetString() : null;
            var timesData = data.TryGetProperty("Times", out var times) ? times.GetString() : null;

            // Try double first, fallback to float if necessary
            double[] frequencies;
            double[] timeArray;
            
            try
            {
                frequencies = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeDoubleArray(frequenciesData) : Array.Empty<double>();
            }
            catch (ArgumentException)
            {
                // Fallback to float decoding and convert to double
                var floatFreqs = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeFloatArray(frequenciesData) : Array.Empty<float>();
                frequencies = floatFreqs.Select(f => (double)f).ToArray();
            }
            
            try
            {
                timeArray = !string.IsNullOrEmpty(timesData) ? _arrayCodec.DecodeDoubleArray(timesData) : Array.Empty<double>();
            }
            catch (ArgumentException)
            {
                // Fallback to float decoding and convert to double
                var floatTimes = !string.IsNullOrEmpty(timesData) ? _arrayCodec.DecodeFloatArray(timesData) : Array.Empty<float>();
                timeArray = floatTimes.Select(f => (double)f).ToArray();
            }
            
            // REW API returns SPL data as numbered properties "0", "1", "2", etc.
            // Each property contains Base64-encoded float array for that time slice
            var splArray = new float[timeArray.Length, frequencies.Length];
            for (int t = 0; t < timeArray.Length; t++)
            {
                var key = t.ToString();
                if (data.TryGetProperty(key, out var splSlice))
                {
                    var sliceData = splSlice.GetString();
                    if (!string.IsNullOrEmpty(sliceData))
                    {
                        var decodedSlice = _arrayCodec.DecodeFloatArray(sliceData);
                        for (int f = 0; f < Math.Min(frequencies.Length, decodedSlice.Length); f++)
                        {
                            splArray[t, f] = decodedSlice[f];
                        }
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

        private SpectrogramResult ParseSpectrogramResult(JsonElement data, SpectrogramOptions options)
        {
            // REW API returns property names with capital letters
            var frequenciesData = data.TryGetProperty("Frequencies", out var freqs) ? freqs.GetString() : null;
            var timesData = data.TryGetProperty("Times", out var times) ? times.GetString() : null;

            // Try double first, fallback to float if necessary
            double[] frequencies;
            double[] timeArray;
            
            try
            {
                frequencies = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeDoubleArray(frequenciesData) : Array.Empty<double>();
            }
            catch (ArgumentException)
            {
                // Fallback to float decoding and convert to double
                var floatFreqs = !string.IsNullOrEmpty(frequenciesData) ? _arrayCodec.DecodeFloatArray(frequenciesData) : Array.Empty<float>();
                frequencies = floatFreqs.Select(f => (double)f).ToArray();
            }
            
            try
            {
                timeArray = !string.IsNullOrEmpty(timesData) ? _arrayCodec.DecodeDoubleArray(timesData) : Array.Empty<double>();
            }
            catch (ArgumentException)
            {
                // Fallback to float decoding and convert to double
                var floatTimes = !string.IsNullOrEmpty(timesData) ? _arrayCodec.DecodeFloatArray(timesData) : Array.Empty<float>();
                timeArray = floatTimes.Select(f => (double)f).ToArray();
            }
            
            // REW API returns SPL data as numbered properties "0", "1", "2", etc.
            // Each property contains Base64-encoded float array for that time slice
            var splArray = new float[timeArray.Length, frequencies.Length];
            for (int t = 0; t < timeArray.Length; t++)
            {
                var key = t.ToString();
                if (data.TryGetProperty(key, out var splSlice))
                {
                    var sliceData = splSlice.GetString();
                    if (!string.IsNullOrEmpty(sliceData))
                    {
                        var decodedSlice = _arrayCodec.DecodeFloatArray(sliceData);
                        for (int f = 0; f < Math.Min(frequencies.Length, decodedSlice.Length); f++)
                        {
                            splArray[t, f] = decodedSlice[f];
                        }
                    }
                }
            }

            // Extract FFTSize and WindowType from JSON if available
            var fftSize = 0;
            var windowType = string.Empty;
            
            if (data.TryGetProperty("FFTSize", out var fftElement))
            {
                fftSize = fftElement.GetInt32();
            }
            else if (data.TryGetProperty("fftSize", out var fftElement2))
            {
                fftSize = fftElement2.GetInt32();
            }
            
            if (data.TryGetProperty("WindowType", out var windowElement))
            {
                windowType = windowElement.GetString() ?? string.Empty;
            }
            else if (data.TryGetProperty("windowType", out var windowElement2))
            {
                windowType = windowElement2.GetString() ?? string.Empty;
            }
            else
            {
                // Fallback to requested WindowType enum value as string
                windowType = options.WindowType.ToString();
            }

            return new SpectrogramResult
            {
                Frequencies = frequencies,
                Times = timeArray,
                SPLData = splArray,
                FFTSize = fftSize,
                WindowType = windowType
            };
        }

        private DistortionData ParseDistortion(JsonElement data, string unit, int ppo)
        {
            // REW API returns: { "type": "...", "columnHeaders": ["col1", "col2"], "data": [[...], [...]] }
            var type = data.TryGetProperty("type", out var typeElem) ? typeElem.GetString() ?? "" : "";
            
            var headers = Array.Empty<string>();
            if (data.TryGetProperty("columnHeaders", out var headersElem) && headersElem.ValueKind == JsonValueKind.Array)
            {
                headers = headersElem.EnumerateArray()
                    .Select(h => h.GetString() ?? "")
                    .ToArray();
            }

            var dataArray = Array.Empty<double[]>();
            if (data.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
            {
                dataArray = dataElem.EnumerateArray()
                    .Select(row => row.ValueKind == JsonValueKind.Array
                        ? row.EnumerateArray().Select(v => v.GetDouble()).ToArray()
                        : Array.Empty<double>())
                    .ToArray();
            }

            return new DistortionData
            {
                Type = type,
                Headers = headers,
                Data = dataArray,
                Unit = unit,
                PPO = ppo
            };
        }

        private RT60Result ParseRT60(JsonElement data, int octaveFrac)
        {
            // REW API returns RT60 as a map of frequency -> { EDT, T20, T30, Topt, ... }
            // The API returns a JSON object where keys are frequency values (as strings)
            // and values are objects with ISO 3382 acoustical parameters
            // Note: Property names are UPPERCASE (EDT, T20, T30)
            // RT60 is typically derived from T30 (most reliable) or Topt (optimal)
            
            var freqList = new List<double>();
            var rt60List = new List<double>(); // Will use T30 as RT60
            var t20List = new List<double>();
            var t30List = new List<double>();
            var edtList = new List<double>();

            // Enumerate the properties (frequency keys), ordered numerically
            foreach (var property in data.EnumerateObject().OrderBy(p => 
                double.TryParse(p.Name, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out var freq) ? freq : double.MaxValue))
            {
                if (double.TryParse(property.Name, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var frequency))
                {
                    freqList.Add(frequency);
                    
                    var values = property.Value;
                    // API uses uppercase property names
                    var edt = values.TryGetProperty("EDT", out var edtVal) ? edtVal.GetDouble() : 0.0;
                    var t20 = values.TryGetProperty("T20", out var t20Val) ? t20Val.GetDouble() : 0.0;
                    var t30 = values.TryGetProperty("T30", out var t30Val) ? t30Val.GetDouble() : 0.0;
                    
                    edtList.Add(edt);
                    t20List.Add(t20);
                    t30List.Add(t30);
                    rt60List.Add(t30); // RT60 is typically T30 value (most reliable estimate)
                }
            }

            return new RT60Result
            {
                Frequencies = freqList.ToArray(),
                RT60Values = rt60List.ToArray(), // T30 values
                T20Values = t20List.ToArray(),
                T30Values = t30List.ToArray(),
                EDTValues = edtList.ToArray(),
                Method = RT60Method.T30, // Using T30 as RT60
                OctaveFraction = octaveFrac
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

        private float[,] Decode2DFloatArray(JsonElement dataElement, int rows, int cols)
        {
            // Waterfall/Spectrogram data is an array of Base64-encoded float arrays
            // Each array represents one time slice (row)
            // Convert jagged array to 2D array
            if (dataElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("2D data element must be an array");

            var result = new float[rows, cols];
            int rowIndex = 0;

            foreach (var slice in dataElement.EnumerateArray())
            {
                if (rowIndex >= rows) break;

                if (slice.ValueKind == JsonValueKind.String)
                {
                    var encodedData = slice.GetString();
                    if (!string.IsNullOrEmpty(encodedData))
                    {
                        var rowData = _arrayCodec.DecodeFloatArray(encodedData);
                        for (int colIndex = 0; colIndex < Math.Min(rowData.Length, cols); colIndex++)
                        {
                            result[rowIndex, colIndex] = rowData[colIndex];
                        }
                    }
                }
                rowIndex++;
            }

            return result;
        }

        #region Subscription Methods

        /// <summary>
        /// Subscribe to measurement change notifications
        /// Maps to POST /measurements/subscribe
        /// Notifications are sent when measurements are added, removed, or modified
        /// </summary>
        /// <param name="subscriberUrl">URL where notifications should be sent</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SubscribeToMeasurementChanges(
            string subscriberUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/measurements/subscribe";
            var subscriber = new { url = subscriberUrl };
            var content = new StringContent(
                JsonSerializer.Serialize(subscriber),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            
            _logger?.LogInformation("Subscribed to measurement changes: {Url}", subscriberUrl);
        }

        /// <summary>
        /// Unsubscribe from measurement change notifications
        /// Maps to POST /measurements/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL of the subscriber to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromMeasurementChanges(
            string subscriberUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/measurements/unsubscribe";
            var subscriber = new { url = subscriberUrl };
            var content = new StringContent(
                JsonSerializer.Serialize(subscriber),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
            
            _logger?.LogInformation("Unsubscribed from measurement changes: {Url}", subscriberUrl);
        }

        /// <summary>
        /// Get list of all current measurement subscribers
        /// Maps to GET /measurements/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs</returns>
        public async Task<string[]> GetMeasurementSubscribers(
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measurements/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var subscribers = JsonSerializer.Deserialize<JsonElement>(json);

            var urls = new List<string>();
            if (subscribers.ValueKind == JsonValueKind.Array)
            {
                foreach (var subscriber in subscribers.EnumerateArray())
                {
                    if (subscriber.TryGetProperty("url", out var urlElement))
                    {
                        var subscriberUrl = urlElement.GetString();
                        if (!string.IsNullOrEmpty(subscriberUrl))
                        {
                            urls.Add(subscriberUrl);
                        }
                    }
                }
            }

            return urls.ToArray();
        }

        #endregion
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