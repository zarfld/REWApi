using System;
using System.Collections.Generic;
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
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SendCommand(string command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var json = JsonSerializer.Serialize(new { command });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/rta/command", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Start the RTA
        /// Convenience method that sends "Start" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Start(CancellationToken cancellationToken = default)
        {
            await SendCommand("Start", cancellationToken);
        }

        /// <summary>
        /// Stop the RTA
        /// Convenience method that sends "Stop" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Stop(CancellationToken cancellationToken = default)
        {
            await SendCommand("Stop", cancellationToken);
        }

        /// <summary>
        /// Restart the RTA (stops and starts, clearing averages)
        /// Convenience method that sends "Restart" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Restart(CancellationToken cancellationToken = default)
        {
            await SendCommand("Restart", cancellationToken);
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

        #region Subscription Management (For Future Implementation)

        // Note: Subscription support requires HTTP server implementation to receive callbacks
        // Subscriptions available for:
        // - POST /rta/status/subscribe
        // - POST /rta/levels/subscribe (with unit parameter)
        // - POST /rta/captured-data/subscribe (with unit parameter)
        // - POST /rta/captured-peak-data/subscribe (with unit parameter)
        // - POST /rta/distortion/subscribe (with unit and distortion parameters)
        
        // Each subscription requires a Subscriber object with:
        // - url: Callback URL for updates
        // - parameters: Optional dict with unit/distortion parameters

        // Unsubscribe endpoints:
        // - POST /rta/status/unsubscribe
        // - POST /rta/levels/unsubscribe
        // - POST /rta/captured-data/unsubscribe
        // - POST /rta/captured-peak-data/unsubscribe
        // - POST /rta/distortion/unsubscribe

        #endregion
    }
}
