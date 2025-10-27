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
    /// REW Signal Generator Service
    /// Maps to /generator/* endpoints in REW API
    /// Provides full control of REW's signal generator for test signals
    /// </summary>
    public class GeneratorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public GeneratorService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Status and Control

        /// <summary>
        /// Get current generator status
        /// GET /generator/status
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current generator status including enabled state, playing state, signal, and level</returns>
        public async Task<GeneratorStatus> GetStatus(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/status", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeneratorStatus>(content) 
                ?? throw new InvalidOperationException("Failed to deserialize generator status");
        }

        /// <summary>
        /// Get list of available generator commands
        /// GET /generator/commands
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available commands (typically "Start" and "Stop")</returns>
        public async Task<List<string>> GetCommands(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/commands", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Send command to generator (Start or Stop)
        /// POST /generator/commands
        /// </summary>
        /// <param name="command">Command to send ("Start" or "Stop")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task SendCommand(string command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var json = JsonSerializer.Serialize(new { command });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/command", httpContent, cancellationToken);
            
            // If not successful, read the response body to get the actual error message
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Start the generator
        /// Convenience method that sends "Play" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Start(CancellationToken cancellationToken = default)
        {
            await SendCommand("Play", cancellationToken);
        }

        /// <summary>
        /// Stop the generator
        /// Convenience method that sends "Stop" command
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task Stop(CancellationToken cancellationToken = default)
        {
            await SendCommand("Stop", cancellationToken);
        }

        #endregion

        #region Signal Selection and Configuration

        /// <summary>
        /// Get currently selected signal
        /// GET /generator/signal
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current signal name</returns>
        public async Task<string> GetSignal(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/signal", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<GeneratorSignalResponse>(content);
            return wrapper?.Signal ?? "";
        }

        /// <summary>
        /// Set current signal
        /// POST /generator/signal
        /// </summary>
        /// <param name="signalName">Signal name to select</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetSignal(string signalName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                throw new ArgumentException("Signal name cannot be null or empty", nameof(signalName));

            var wrapper = new GeneratorSignalResponse { Signal = signalName };
            var json = JsonSerializer.Serialize(wrapper);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/signal", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Get list of available signals
        /// GET /generator/signals
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available signal types</returns>
        public async Task<List<string>> GetAvailableSignals(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/signals", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Get configuration for current signal
        /// GET /generator/signal/configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current signal configuration</returns>
        public async Task<GeneratorConfiguration> GetConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/signal/configuration", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeneratorConfiguration>(content) 
                ?? new GeneratorConfiguration();
        }

        /// <summary>
        /// Update configuration for current signal
        /// POST or PUT /generator/signal/configuration
        /// DEPRECATED: Use signal-specific methods instead (UpdateSineConfiguration, UpdateRandomNoiseConfiguration, etc.)
        /// </summary>
        /// <param name="configuration">New configuration settings (can be partial - only non-null properties sent)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <remarks>
        /// The API accepts partial configurations per REW documentation.
        /// Different signals require different configuration properties - consult /generator/signals for schemas.
        /// This method serializes only non-null properties to support partial updates.
        /// </remarks>
        [Obsolete("Use signal-specific configuration methods instead: UpdateSineConfiguration, UpdateRandomNoiseConfiguration, etc.")]
        public async Task UpdateConfiguration(GeneratorConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Serialize with options to ignore null values for partial configuration support
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(configuration, options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/signal/configuration", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #region Signal-Specific Configuration Methods

        /// <summary>
        /// Updates sine wave generator configuration.
        /// </summary>
        public async Task UpdateSineConfiguration(SineConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates random noise generator configuration (pinknoise, whitenoise).
        /// </summary>
        public async Task UpdateRandomNoiseConfiguration(RandomNoiseConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates periodic noise generator configuration (pinkpn, whitepn).
        /// </summary>
        public async Task UpdatePeriodicNoiseConfiguration(PeriodicNoiseConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates square wave generator configuration.
        /// </summary>
        public async Task UpdateSquareConfiguration(SquareConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates sawtooth wave generator configuration.
        /// </summary>
        public async Task UpdateSawtoothConfiguration(SawtoothConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates tone burst generator configuration.
        /// </summary>
        public async Task UpdateToneBurstConfiguration(ToneBurstConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates CEA burst generator configuration.
        /// </summary>
        public async Task UpdateCEABurstConfiguration(CEABurstConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates two-tone generator configuration (dualtone).
        /// </summary>
        public async Task UpdateTwoToneConfiguration(TwoToneConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates three-tone generator configuration (tripletone).
        /// </summary>
        public async Task UpdateThreeToneConfiguration(ThreeToneConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates quadtone (4-tone) generator configuration.
        /// </summary>
        public async Task UpdateQuadtoneConfiguration(QuadtoneConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates multitone generator configuration.
        /// </summary>
        public async Task UpdateMultitoneConfiguration(MultitoneConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates linear sweep generator configuration.
        /// </summary>
        public async Task UpdateLinearSweepConfiguration(LinearSweepConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates log sweep generator configuration.
        /// </summary>
        public async Task UpdateLogSweepConfiguration(LogSweepConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Updates measurement sweep generator configuration.
        /// </summary>
        public async Task UpdateMeasSweepConfiguration(MeasSweepConfiguration configuration, CancellationToken cancellationToken = default)
        {
            await UpdateSignalConfiguration(configuration, cancellationToken);
        }

        /// <summary>
        /// Generic helper method to update signal-specific configuration.
        /// </summary>
        private async Task UpdateSignalConfiguration<T>(T configuration, CancellationToken cancellationToken) where T : class
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(configuration, options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/signal/configuration", httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        /// <summary>
        /// Get commands available for current signal
        /// GET /generator/signal/commands
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of signal-specific commands (may be empty)</returns>
        public async Task<List<string>> GetSignalCommands(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/signal/commands", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Send command to current signal
        /// POST /generator/signal/command
        /// </summary>
        /// <param name="command">Command to send to signal</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SendSignalCommand(string command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var json = JsonSerializer.Serialize(new { command });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/signal/command", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Level and Frequency Control

        /// <summary>
        /// Get current generator output level
        /// GET /generator/level
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current output level (default unit: dBFS)</returns>
        public async Task<double> GetLevel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/level", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<GeneratorLevelResponse>(content);
            return wrapper?.Value ?? 0.0;
        }

        /// <summary>
        /// Set generator output level
        /// POST /generator/level
        /// </summary>
        /// <param name="level">Output level value</param>
        /// <param name="unit">Unit for level (default: dBFS). Supported: dBFS, dBu, dBV, V</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetLevel(double level, string unit = "dBFS", CancellationToken cancellationToken = default)
        {
            var wrapper = new GeneratorLevelResponse 
            { 
                Value = level,
                Unit = unit
            };

            var json = JsonSerializer.Serialize(wrapper);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/level", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        /// <summary>
        /// Get list of supported level units
        /// GET /generator/level/units
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of supported units (dBFS, dBu, dBV, V, etc.)</returns>
        public async Task<List<string>> GetLevelUnits(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/level/units", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Get current frequency (for signals that support it)
        /// GET /generator/frequency
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current frequency in Hz</returns>
        public async Task<double> GetFrequency(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/frequency", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<GeneratorFrequencyResponse>(content);
            return wrapper?.Value ?? 0.0;
        }

        /// <summary>
        /// Set generator frequency (for signals that support it)
        /// POST /generator/frequency
        /// </summary>
        /// <param name="frequency">Frequency value</param>
        /// <param name="unit">Unit for frequency (default: Hz). Supported: Hz, kHz</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetFrequency(double frequency, string unit = "Hz", CancellationToken cancellationToken = default)
        {
            var wrapper = new GeneratorFrequencyResponse { Value = frequency, Unit = unit };
            var json = JsonSerializer.Serialize(wrapper);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/frequency", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Output Control

        /// <summary>
        /// Get whether second output is inverted
        /// GET /generator/invert-second-output
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if second output is inverted</returns>
        public async Task<bool> GetInvertSecondOutput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/invert-second-output", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content);
        }

        /// <summary>
        /// Set whether to invert second output
        /// POST /generator/invert-second-output
        /// </summary>
        /// <param name="invert">True to invert second output</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task SetInvertSecondOutput(bool invert, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(invert);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/invert-second-output", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Protection

        /// <summary>
        /// Get generator protection settings
        /// GET /generator/protection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current protection settings</returns>
        public async Task<GeneratorProtectionSettings> GetProtection(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/generator/protection", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GeneratorProtectionSettings>(content) 
                ?? GeneratorProtectionSettings.CreateSafeDefaults();
        }

        /// <summary>
        /// Update generator protection settings
        /// POST /generator/protection
        /// </summary>
        /// <param name="protection">New protection settings</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task UpdateProtection(GeneratorProtectionSettings protection, CancellationToken cancellationToken = default)
        {
            if (protection == null)
                throw new ArgumentNullException(nameof(protection));

            var json = JsonSerializer.Serialize(protection);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/generator/protection", httpContent, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Response: {errorBody}");
            }
        }

        #endregion

        #region Validation Helpers

        private void ValidateFrequency(double frequency)
        {
            if (frequency <= 0 || frequency > 24000)
                throw new ArgumentOutOfRangeException(nameof(frequency), 
                    "Frequency must be between 0 and 24000 Hz");
        }

        private void ValidateLevel(double level)
        {
            if (level < -120 || level > 0)
                throw new ArgumentOutOfRangeException(nameof(level), 
                    "Level must be between -120 and 0 dBFS");
        }

        #endregion
    }
}
