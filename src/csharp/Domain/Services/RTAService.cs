using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// REW Real-Time Analyzer (RTA) Service
    /// Maps to /rta/* endpoints in REW API
    /// Provides full control of REW's real-time analyzer for spectrum analysis
    /// 
    /// Key Features:
    /// - Real-time spectrum and fractional octave analysis
    /// - Harmonic distortion measurement
    /// - Subscription support for live data streaming
    /// - Configurable averaging and display options
    /// 
    /// Reference: https://www.roomeqwizard.com/help/help_en-GB/html/api.html#rta
    /// </summary>
    public class RTAService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RTAService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Status and Control

        /// <summary>
        /// Get current RTA status
        /// GET /rta/status
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current RTA status including enabled and running state</returns>
        public async Task<RTAStatus> GetStatus(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/status", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTAStatus>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize RTA status");
        }

        /// <summary>
        /// Get list of available RTA commands
        /// GET /rta/commands
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available commands (Start, Stop, Restart, etc.)</returns>
        public async Task<List<string>> GetCommands(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/commands", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Send command to RTA
        /// POST /rta/command
        /// </summary>
        /// <param name="command">Command to send (Start, Stop, Restart, etc.)</param>
        /// <param name="parameters">Optional parameters object for the command</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SendCommand(string command, object? parameters = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            object commandData;
            if (parameters == null)
            {
                commandData = new { command };
            }
            else
            {
                commandData = new { command, parameters };
            }
                
            var json = JsonSerializer.Serialize(commandData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/command", httpContent, cancellationToken);
            
            // If not successful, read the response body to get the actual error message
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Start the RTA
        /// Convenience method that sends "Start" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Start(CancellationToken cancellationToken = default)
        {
            await SendCommand("Start", null, cancellationToken);
        }

        /// <summary>
        /// Stop the RTA
        /// Convenience method that sends "Stop" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Stop(CancellationToken cancellationToken = default)
        {
            await SendCommand("Stop", null, cancellationToken);
        }

        /// <summary>
        /// Restart the RTA (stops and starts, clearing averages)
        /// Convenience method that sends "Restart" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Restart(CancellationToken cancellationToken = default)
        {
            await SendCommand("Restart", null, cancellationToken);
        }

        /// <summary>
        /// Reset the RTA (clears all data and averages)
        /// Convenience method that sends "Reset" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Reset(CancellationToken cancellationToken = default)
        {
            await SendCommand("Reset", null, cancellationToken);
        }

        /// <summary>
        /// Set SPL offset for the RTA
        /// Convenience method that sends "Set SPL offset" command with value
        /// </summary>
        /// <param name="offsetDb">SPL offset in dB</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetSPLOffset(double offsetDb, CancellationToken cancellationToken = default)
        {
            await SendCommand("Set SPL offset", new { offset = offsetDb }, cancellationToken);
        }

        /// <summary>
        /// Import calibration file for RTA input
        /// Convenience method that sends "Import cal" command with file path
        /// </summary>
        /// <param name="filePath">Path to calibration file (.txt format)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ImportCalibration(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            await SendCommand("Import cal", new { filePath }, cancellationToken);
        }

        /// <summary>
        /// Import reference calibration file for RTA
        /// Convenience method that sends "Import reference" command with file path
        /// </summary>
        /// <param name="filePath">Path to reference calibration file (.txt format)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ImportReference(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            await SendCommand("Import reference", new { filePath }, cancellationToken);
        }

        /// <summary>
        /// Save RTA graph as image
        /// Convenience method that sends "Save graph image" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Image capture settings can be configured via GetImageCaptureSettings/UpdateImageCaptureSettings
        /// The image will be saved to the location specified in those settings
        /// </remarks>
        public async Task SaveGraphImage(CancellationToken cancellationToken = default)
        {
            await SendCommand("Save graph image", null, cancellationToken);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get current RTA configuration
        /// GET /rta/configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current RTA configuration (mode, FFT length, averaging, etc.)</returns>
        public async Task<RTAConfiguration> GetConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/configuration", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTAConfiguration>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize RTA configuration");
        }

        /// <summary>
        /// Update RTA configuration (full replacement)
        /// PUT /rta/configuration
        /// </summary>
        /// <param name="configuration">Complete RTA configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetConfiguration(RTAConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var json = JsonSerializer.Serialize(configuration);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/rta/configuration", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Update partial RTA configuration
        /// POST /rta/configuration
        /// </summary>
        /// <param name="configuration">Partial RTA configuration (only specified fields updated)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateConfiguration(RTAConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var json = JsonSerializer.Serialize(configuration);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/configuration", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get current RTA appearance configuration
        /// GET /rta/appearance-configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current RTA appearance settings</returns>
        public async Task<RTAAppearanceConfiguration> GetAppearanceConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/appearance-configuration", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTAAppearanceConfiguration>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize RTA appearance configuration");
        }

        /// <summary>
        /// Update RTA appearance configuration
        /// POST /rta/appearance-configuration
        /// </summary>
        /// <param name="configuration">RTA appearance configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateAppearanceConfiguration(RTAAppearanceConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var json = JsonSerializer.Serialize(configuration);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/appearance-configuration", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get current RTA distortion configuration
        /// GET /rta/distortion-configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current RTA distortion measurement settings</returns>
        public async Task<RTADistortionConfiguration> GetDistortionConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/distortion-configuration", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTADistortionConfiguration>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize RTA distortion configuration");
        }

        /// <summary>
        /// Update RTA distortion configuration
        /// POST /rta/distortion-configuration
        /// </summary>
        /// <param name="configuration">RTA distortion configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateDistortionConfiguration(RTADistortionConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var json = JsonSerializer.Serialize(configuration);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/distortion-configuration", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Graph Display Settings

        /// <summary>
        /// Get current Y-axis unit
        /// GET /rta/y-axis
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current Y-axis unit (e.g., "dBFS", "dBu", "SPL")</returns>
        public async Task<string> GetYAxisUnit(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/y-axis", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            // API returns quoted string, need to deserialize
            return JsonSerializer.Deserialize<string>(content) ?? "dBFS";
        }

        /// <summary>
        /// Set Y-axis unit
        /// POST /rta/y-axis
        /// </summary>
        /// <param name="unit">Unit for Y-axis (dBFS, dBu, dBV, SPL, etc.)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetYAxisUnit(string unit, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("Unit cannot be null or empty", nameof(unit));

            var json = JsonSerializer.Serialize(unit);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/y-axis", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get available Y-axis units
        /// GET /rta/y-axis/units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available units</returns>
        public async Task<List<string>> GetAvailableYAxisUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/y-axis/units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get current graph limits
        /// GET /rta/graph-limits
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current graph Y-axis limits (min and max)</returns>
        public async Task<RTAGraphLimits> GetGraphLimits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/graph-limits", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTAGraphLimits>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize graph limits");
        }

        /// <summary>
        /// Set graph limits
        /// POST /rta/graph-limits
        /// </summary>
        /// <param name="limits">Graph limits (min and max Y values)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetGraphLimits(RTAGraphLimits limits, CancellationToken cancellationToken = default)
        {
            if (limits == null)
                throw new ArgumentNullException(nameof(limits));

            var json = JsonSerializer.Serialize(limits);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/graph-limits", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get current cursor X position (frequency)
        /// GET /rta/cursor-x
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cursor frequency in Hz</returns>
        public async Task<double> GetCursorX(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/cursor-x", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content);
        }

        /// <summary>
        /// Set cursor X position (frequency)
        /// POST /rta/cursor-x
        /// </summary>
        /// <param name="frequencyHz">Frequency in Hz</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetCursorX(double frequencyHz, CancellationToken cancellationToken = default)
        {
            if (frequencyHz <= 0)
                throw new ArgumentException("Frequency must be positive", nameof(frequencyHz));

            var json = JsonSerializer.Serialize(frequencyHz);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/cursor-x", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get image capture settings
        /// GET /rta/image-capture-settings
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current image capture settings</returns>
        public async Task<RTAImageCaptureSettings> GetImageCaptureSettings(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/image-capture-settings", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RTAImageCaptureSettings>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize image capture settings");
        }

        /// <summary>
        /// Update image capture settings
        /// POST /rta/image-capture-settings
        /// </summary>
        /// <param name="settings">Image capture settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateImageCaptureSettings(RTAImageCaptureSettings settings, CancellationToken cancellationToken = default)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var json = JsonSerializer.Serialize(settings);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/image-capture-settings", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Real-Time Data Access

        /// <summary>
        /// Get current RTA input levels
        /// GET /rta/levels?unit={unit}
        /// </summary>
        /// <param name="unit">Unit for level values (dBFS, dBu, dBV, SPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of RTA levels (one per input, or two for stereo)</returns>
        public async Task<List<RTALevel>> GetLevels(string unit = "dBFS", CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/rta/levels";
            if (!string.IsNullOrWhiteSpace(unit))
                url += $"?unit={Uri.EscapeDataString(unit)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RTALevel>>(content) ?? new List<RTALevel>();
        }

        /// <summary>
        /// Get available units for level readings
        /// GET /rta/levels/units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available units</returns>
        public async Task<List<string>> GetAvailableLevelUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/levels/units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get current RTA captured data (RMS average)
        /// GET /rta/captured-data?unit={unit}
        /// </summary>
        /// <param name="unit">Unit for magnitude data (dBFS, dBu, dBV, SPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Frequency response with Base64-encoded magnitude array</returns>
        /// <remarks>
        /// The response contains:
        /// - timestampNanos: Nanosecond timestamp
        /// - sampleCount: Running sample count since RTA started
        /// - smoothing: Current smoothing setting
        /// - startFreq: Start frequency in Hz
        /// - ppo (fractional octave) or freqStep (spectrum): Frequency spacing
        /// - magnitudes: Base64-encoded float array of magnitude values
        /// 
        /// Use helper methods to decode the Base64 array to float values.
        /// Array encoding is big-endian 32-bit floats.
        /// </returns>
        public async Task<object> GetCapturedData(string unit = "dBFS", CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/rta/captured-data";
            if (!string.IsNullOrWhiteSpace(unit))
                url += $"?unit={Uri.EscapeDataString(unit)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            // Return raw JSON for now - client needs to decode Base64 array
            return JsonSerializer.Deserialize<object>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize captured data");
        }

        /// <summary>
        /// Get current RTA captured peak data
        /// GET /rta/captured-peak-data?unit={unit}
        /// </summary>
        /// <param name="unit">Unit for magnitude data (dBFS, dBu, dBV, SPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Frequency response with Base64-encoded magnitude array</returns>
        public async Task<object> GetCapturedPeakData(string unit = "dBFS", CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/rta/captured-peak-data";
            if (!string.IsNullOrWhiteSpace(unit))
                url += $"?unit={Uri.EscapeDataString(unit)}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize captured peak data");
        }

        /// <summary>
        /// Get available units for captured data
        /// GET /rta/captured-data/units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available units</returns>
        public async Task<List<string>> GetAvailableCapturedDataUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/captured-data/units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get current RTA distortion results
        /// GET /rta/distortion?unit={levelUnit}&distortion={distortionUnit}
        /// </summary>
        /// <param name="levelUnit">Unit for level values (dBFS, dBu, dBV, SPL)</param>
        /// <param name="distortionUnit">Unit for distortion values (dB or percent)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of distortion results (one per input, or two for stereo)</returns>
        public async Task<List<RTADistortion>> GetDistortion(
            string levelUnit = "dBFS", 
            string distortionUnit = "dB", 
            CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/rta/distortion";
            var queryParams = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(levelUnit))
                queryParams.Add($"unit={Uri.EscapeDataString(levelUnit)}");
            
            if (!string.IsNullOrWhiteSpace(distortionUnit))
                queryParams.Add($"distortion={Uri.EscapeDataString(distortionUnit)}");
            
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RTADistortion>>(content) ?? new List<RTADistortion>();
        }

        /// <summary>
        /// Get available units for distortion level values
        /// GET /rta/distortion/units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available units</returns>
        public async Task<List<string>> GetAvailableDistortionUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/distortion/units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get available units for relative distortion values
        /// GET /rta/distortion/relative-units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available units (typically "dB" and "percent")</returns>
        public async Task<List<string>> GetAvailableRelativeDistortionUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/distortion/relative-units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Subscription Management - Status

        /// <summary>
        /// Subscribe to RTA status updates
        /// POST /rta/status/subscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to receive status update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Receives notifications when RTA status changes (enabled/disabled, running/stopped)
        /// </remarks>
        public async Task SubscribeToRTAStatus(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/status/subscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unsubscribe from RTA status updates
        /// POST /rta/status/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to remove from status update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromRTAStatus(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/status/unsubscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get list of current RTA status subscribers
        /// GET /rta/status/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs currently subscribed to RTA status updates</returns>
        public async Task<string[]> GetRTAStatusSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/status/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
            
            // Extract URL from each Subscriber object
            return subscribers
                .Select(s => {
                    var element = (JsonElement)s;
                    return element.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToArray()!;
        }

        #endregion

        #region Subscription Management - Levels

        /// <summary>
        /// Subscribe to RTA input level updates
        /// POST /rta/levels/subscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to receive level update notifications</param>
        /// <param name="unit">Unit for level values (dBFS, dBu, dBV, SPL). Optional - uses parameters field in Subscriber object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Receives notifications when RTA input levels change
        /// Use unit parameter to specify desired level unit
        /// </remarks>
        public async Task SubscribeToRTALevels(string subscriberUrl, string? unit = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            object subscriberData;
            if (!string.IsNullOrWhiteSpace(unit))
            {
                subscriberData = new 
                { 
                    url = subscriberUrl,
                    parameters = new Dictionary<string, string> { { "unit", unit } }
                };
            }
            else
            {
                subscriberData = new { url = subscriberUrl };
            }

            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/levels/subscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unsubscribe from RTA input level updates
        /// POST /rta/levels/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to remove from level update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromRTALevels(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/levels/unsubscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get list of current RTA level subscribers
        /// GET /rta/levels/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs currently subscribed to RTA level updates</returns>
        public async Task<string[]> GetRTALevelsSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/levels/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
            
            return subscribers
                .Select(s => {
                    var element = (JsonElement)s;
                    return element.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToArray()!;
        }

        #endregion

        #region Subscription Management - Captured Data

        /// <summary>
        /// Subscribe to RTA captured data updates (RMS average)
        /// POST /rta/captured-data/subscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to receive captured data update notifications</param>
        /// <param name="unit">Unit for magnitude data (dBFS, dBu, dBV, SPL). Optional - uses parameters field in Subscriber object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Receives notifications when RTA captured data updates
        /// Data includes Base64-encoded float array of magnitude values
        /// Use unit parameter to specify desired magnitude unit
        /// </remarks>
        public async Task SubscribeToRTACapturedData(string subscriberUrl, string? unit = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            object subscriberData;
            if (!string.IsNullOrWhiteSpace(unit))
            {
                subscriberData = new 
                { 
                    url = subscriberUrl,
                    parameters = new Dictionary<string, string> { { "unit", unit } }
                };
            }
            else
            {
                subscriberData = new { url = subscriberUrl };
            }

            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/captured-data/subscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unsubscribe from RTA captured data updates
        /// POST /rta/captured-data/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to remove from captured data update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromRTACapturedData(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/captured-data/unsubscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get list of current RTA captured data subscribers
        /// GET /rta/captured-data/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs currently subscribed to RTA captured data updates</returns>
        public async Task<string[]> GetRTACapturedDataSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/captured-data/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
            
            return subscribers
                .Select(s => {
                    var element = (JsonElement)s;
                    return element.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToArray()!;
        }

        #endregion

        #region Subscription Management - Captured Peak Data

        /// <summary>
        /// Subscribe to RTA captured peak data updates
        /// POST /rta/captured-peak-data/subscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to receive captured peak data update notifications</param>
        /// <param name="unit">Unit for magnitude data (dBFS, dBu, dBV, SPL). Optional - uses parameters field in Subscriber object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Receives notifications when RTA captured peak data updates
        /// Data includes Base64-encoded float array of peak magnitude values
        /// Use unit parameter to specify desired magnitude unit
        /// </remarks>
        public async Task SubscribeToRTACapturedPeakData(string subscriberUrl, string? unit = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            object subscriberData;
            if (!string.IsNullOrWhiteSpace(unit))
            {
                subscriberData = new 
                { 
                    url = subscriberUrl,
                    parameters = new Dictionary<string, string> { { "unit", unit } }
                };
            }
            else
            {
                subscriberData = new { url = subscriberUrl };
            }

            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/captured-peak-data/subscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unsubscribe from RTA captured peak data updates
        /// POST /rta/captured-peak-data/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to remove from captured peak data update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromRTACapturedPeakData(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/captured-peak-data/unsubscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get list of current RTA captured peak data subscribers
        /// GET /rta/captured-peak-data/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs currently subscribed to RTA captured peak data updates</returns>
        public async Task<string[]> GetRTACapturedPeakDataSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/captured-peak-data/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
            
            return subscribers
                .Select(s => {
                    var element = (JsonElement)s;
                    return element.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToArray()!;
        }

        #endregion

        #region Subscription Management - Distortion

        /// <summary>
        /// Subscribe to RTA distortion updates
        /// POST /rta/distortion/subscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to receive distortion update notifications</param>
        /// <param name="levelUnit">Unit for level values (dBFS, dBu, dBV, SPL). Optional - uses parameters field in Subscriber object</param>
        /// <param name="distortionUnit">Unit for relative distortion values (dB or percent). Optional - uses parameters field in Subscriber object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// Receives notifications when RTA distortion measurements update
        /// Use levelUnit to specify desired level unit
        /// Use distortionUnit to specify desired distortion unit (dB or percent)
        /// </remarks>
        public async Task SubscribeToRTADistortion(
            string subscriberUrl, 
            string? levelUnit = null, 
            string? distortionUnit = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            object subscriberData;
            var hasLevelUnit = !string.IsNullOrWhiteSpace(levelUnit);
            var hasDistortionUnit = !string.IsNullOrWhiteSpace(distortionUnit);

            if (hasLevelUnit || hasDistortionUnit)
            {
                var parameters = new Dictionary<string, string>();
                if (hasLevelUnit) parameters["unit"] = levelUnit!;
                if (hasDistortionUnit) parameters["distortion"] = distortionUnit!;
                
                subscriberData = new 
                { 
                    url = subscriberUrl,
                    parameters = parameters
                };
            }
            else
            {
                subscriberData = new { url = subscriberUrl };
            }

            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/distortion/subscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Unsubscribe from RTA distortion updates
        /// POST /rta/distortion/unsubscribe
        /// </summary>
        /// <param name="subscriberUrl">URL to remove from distortion update notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UnsubscribeFromRTADistortion(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var subscriberData = new { url = subscriberUrl };
            var json = JsonSerializer.Serialize(subscriberData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/distortion/unsubscribe", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Get list of current RTA distortion subscribers
        /// GET /rta/distortion/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of subscriber URLs currently subscribed to RTA distortion updates</returns>
        public async Task<string[]> GetRTADistortionSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/rta/distortion/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<object>>(content) ?? new List<object>();
            
            return subscribers
                .Select(s => {
                    var element = (JsonElement)s;
                    return element.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToArray()!;
        }

        #endregion
    }
}
