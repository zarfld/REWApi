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
    /// REW Audio system configuration and management
    /// Maps to /audio/* endpoints with proper state management
    /// </summary>
    public class AudioService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<AudioService>? _logger;
        private AudioState? _cachedState;
        private DateTime _lastStateRefresh = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30);

        internal AudioService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            ILogger<AudioService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
        }

        /// <summary>
        /// Configure Java audio with automatic fallbacks
        /// Handles sample rate negotiation and error recovery
        /// </summary>
        public async Task ConfigureJava(
            string inputDevice, 
            string outputDevice, 
            int sampleRate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(inputDevice))
                throw new ArgumentException("Input device cannot be null or empty", nameof(inputDevice));
            if (string.IsNullOrEmpty(outputDevice))
                throw new ArgumentException("Output device cannot be null or empty", nameof(outputDevice));
            if (sampleRate <= 0)
                throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));

            await _commandGate.ExecuteCommand(async () =>
            {
                try
                {
                    // First, set driver to Java
                    await SetDriverInternal(AudioDriverType.Java, cancellationToken);

                    // Get available devices to validate inputs
                    var devices = await GetJavaDevices(cancellationToken);
                    
                    if (!devices.InputDevices.Contains(inputDevice))
                    {
                        throw new REWAudioException(
                            $"Input device '{inputDevice}' not found",
                            devices.InputDevices.ToArray(),
                            await GetSupportedSampleRatesInternal(cancellationToken));
                    }

                    if (!devices.OutputDevices.Contains(outputDevice))
                    {
                        throw new REWAudioException(
                            $"Output device '{outputDevice}' not found",
                            devices.OutputDevices.ToArray(),
                            await GetSupportedSampleRatesInternal(cancellationToken));
                    }

                    // Configure Java audio settings
                    var config = new JavaAudioConfig
                    {
                        InputDevice = inputDevice,
                        OutputDevice = outputDevice,
                        Input = devices.Inputs.FirstOrDefault(),
                        Output = devices.Outputs.FirstOrDefault()
                    };

                    await UpdateJavaConfigInternal(config, cancellationToken);

                    // Set sample rate
                    var supportedRates = await GetSupportedSampleRatesInternal(cancellationToken);
                    if (!supportedRates.Contains(sampleRate))
                    {
                        var fallbackRate = supportedRates.OrderBy(r => Math.Abs(r - sampleRate)).First();
                        _logger?.LogWarning("Sample rate {RequestedRate} not supported, using {FallbackRate}", 
                            sampleRate, fallbackRate);
                        sampleRate = fallbackRate;
                    }

                    await SetSampleRateInternal(sampleRate, cancellationToken);

                    // Configure audio system
                    await ConfigureInternal(cancellationToken);

                    // Invalidate cached state
                    _cachedState = null;

                    _logger?.LogInformation("Successfully configured Java audio: {Input} -> {Output} @ {SampleRate}Hz",
                        inputDevice, outputDevice, sampleRate);
                }
                catch (REWAudioException)
                {
                    throw; // Re-throw our specific exceptions
                }
                catch (Exception ex)
                {
                    var devices = await GetJavaDevices(cancellationToken);
                    var supportedRates = await GetSupportedSampleRatesInternal(cancellationToken);
                    
                    throw new REWAudioException(
                        $"Failed to configure Java audio: {ex.Message}",
                        devices.InputDevices.ToArray(),
                        supportedRates);
                }
            }, "Configure Java Audio", cancellationToken);
        }

        /// <summary>
        /// Configure ASIO audio with device validation
        /// Includes automatic driver reload if needed
        /// </summary>
        public async Task ConfigureASIO(
            string device, 
            string input, 
            string output, 
            int sampleRate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(device))
                throw new ArgumentException("ASIO device cannot be null or empty", nameof(device));
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("ASIO input cannot be null or empty", nameof(input));
            if (string.IsNullOrEmpty(output))
                throw new ArgumentException("ASIO output cannot be null or empty", nameof(output));

            await _commandGate.ExecuteCommand(async () =>
            {
                try
                {
                    // Set driver to ASIO
                    await SetDriverInternal(AudioDriverType.ASIO, cancellationToken);

                    // Reload ASIO driver to refresh device list
                    await ReloadASIODriverInternal(cancellationToken);

                    // Configure ASIO settings
                    var config = new ASIOAudioConfig
                    {
                        Device = device,
                        Input = input,
                        Output = output
                    };

                    await UpdateASIOConfigInternal(config, cancellationToken);

                    // Set sample rate
                    await SetSampleRateInternal(sampleRate, cancellationToken);

                    // Configure audio system
                    await ConfigureInternal(cancellationToken);

                    // Invalidate cached state
                    _cachedState = null;

                    _logger?.LogInformation("Successfully configured ASIO audio: {Device} {Input} -> {Output} @ {SampleRate}Hz",
                        device, input, output, sampleRate);
                }
                catch (Exception ex)
                {
                    throw new REWAudioException(
                        $"Failed to configure ASIO audio: {ex.Message}",
                        new[] { device },
                        await GetSupportedSampleRatesInternal(cancellationToken));
                }
            }, "Configure ASIO Audio", cancellationToken);
        }

        /// <summary>
        /// Get available sample rates for current driver
        /// Maps to GET /audio/sample-rates
        /// </summary>
        public async Task<int[]> GetSupportedSampleRates(CancellationToken cancellationToken = default)
        {
            return await GetSupportedSampleRatesInternal(cancellationToken);
        }

        /// <summary>
        /// Switch audio driver type with state invalidation
        /// Maps to PUT /audio/driver
        /// </summary>
        public async Task SetDriver(AudioDriverType driver, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                await SetDriverInternal(driver, cancellationToken);
                _cachedState = null; // Invalidate cache
            }, $"Set Driver to {driver}", cancellationToken);
        }

        /// <summary>
        /// Get Java audio configuration
        /// Maps to GET /audio/java
        /// </summary>
        public async Task<JavaAudioConfig> GetJavaConfig(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return ParseJavaAudioConfig(data);
        }

        /// <summary>
        /// Update Java audio settings
        /// Maps to PUT /audio/java
        /// </summary>
        public async Task UpdateJavaConfig(JavaAudioConfig config, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                await UpdateJavaConfigInternal(config, cancellationToken);
                _cachedState = null;
            }, "Update Java Audio Config", cancellationToken);
        }

        /// <summary>
        /// Get available Java input/output devices
        /// Maps to GET /audio/java/devices
        /// </summary>
        public async Task<JavaDevices> GetJavaDevices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/devices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new JavaDevices
            {
                InputDevices = ParseStringArray(data, "inputDevices"),
                Inputs = ParseStringArray(data, "inputs"),
                OutputDevices = ParseStringArray(data, "outputDevices"),
                Outputs = ParseStringArray(data, "outputs"),
                OutputChannels = ParseStringArray(data, "outputChannels"),
                RefOutputChannels = ParseStringArray(data, "refOutputChannels"),
                NumInputChannels = data.TryGetProperty("numInputChannels", out var num) ? num.GetInt32() : 0
            };
        }

        #region Java Audio Device Configuration

        /// <summary>
        /// Get current Java input device
        /// Maps to GET /audio/java/input-device
        /// </summary>
        public async Task<string> GetJavaInputDevice(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/input-device", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("device", out var device) ? device.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java input device
        /// Maps to POST /audio/java/input-device
        /// </summary>
        public async Task<bool> SetJavaInputDevice(string device, CancellationToken cancellationToken = default)
        {
            var requestData = new { device };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/input-device", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available Java input device choices
        /// Maps to GET /audio/java/input-devices
        /// </summary>
        public async Task<List<string>> GetJavaInputDevices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/input-devices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Get number of hardware channels the input device has
        /// Maps to GET /audio/java/num-input-device-channels
        /// </summary>
        public async Task<int> GetJavaNumInputDeviceChannels(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/num-input-device-channels", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int>(json);
        }

        /// <summary>
        /// Get current Java output device
        /// Maps to GET /audio/java/output-device
        /// </summary>
        public async Task<string> GetJavaOutputDevice(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/output-device", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("device", out var device) ? device.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java output device
        /// Maps to POST /audio/java/output-device
        /// </summary>
        public async Task<bool> SetJavaOutputDevice(string device, CancellationToken cancellationToken = default)
        {
            var requestData = new { device };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/output-device", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available Java output device choices
        /// Maps to GET /audio/java/output-devices
        /// </summary>
        public async Task<List<string>> GetJavaOutputDevices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/output-devices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Get number of hardware channels the output device has
        /// Maps to GET /audio/java/num-output-device-channels
        /// </summary>
        public async Task<int> GetJavaNumOutputDeviceChannels(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/num-output-device-channels", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int>(json);
        }

        /// <summary>
        /// Get current Java input
        /// Maps to GET /audio/java/input
        /// </summary>
        public async Task<string> GetJavaInput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/input", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("input", out var input) ? input.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java input
        /// Maps to POST /audio/java/input
        /// </summary>
        public async Task<bool> SetJavaInput(string input, CancellationToken cancellationToken = default)
        {
            var requestData = new { input };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/input", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available Java input choices
        /// Maps to GET /audio/java/inputs
        /// </summary>
        public async Task<List<string>> GetJavaInputs(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/inputs", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Get current Java output
        /// Maps to GET /audio/java/output
        /// </summary>
        public async Task<string> GetJavaOutput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/output", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("output", out var output) ? output.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java output
        /// Maps to POST /audio/java/output
        /// </summary>
        public async Task<bool> SetJavaOutput(string output, CancellationToken cancellationToken = default)
        {
            var requestData = new { output };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/output", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available Java output choices
        /// Maps to GET /audio/java/outputs
        /// </summary>
        public async Task<List<string>> GetJavaOutputs(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/outputs", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        #endregion

        #region Java Audio Channel Configuration

        /// <summary>
        /// Get current Java input channel
        /// Maps to GET /audio/java/input-channel
        /// </summary>
        public async Task<int> GetJavaInputChannel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/input-channel", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("channel", out var channel) ? channel.GetInt32() : 0;
        }

        /// <summary>
        /// Set Java input channel
        /// Maps to POST /audio/java/input-channel
        /// </summary>
        public async Task<bool> SetJavaInputChannel(int channel, CancellationToken cancellationToken = default)
        {
            var requestData = new { channel };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/input-channel", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get number of Java input channels
        /// Maps to GET /audio/java/num-input-channels
        /// </summary>
        public async Task<int> GetJavaNumInputChannels(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/num-input-channels", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int>(json);
        }

        /// <summary>
        /// Get current Java reference input channel
        /// Maps to GET /audio/java/ref-input-channel
        /// </summary>
        public async Task<int> GetJavaRefInputChannel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/ref-input-channel", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("channel", out var channel) ? channel.GetInt32() : 0;
        }

        /// <summary>
        /// Set Java reference input channel
        /// Maps to POST /audio/java/ref-input-channel
        /// </summary>
        public async Task<bool> SetJavaRefInputChannel(int channel, CancellationToken cancellationToken = default)
        {
            var requestData = new { channel };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/ref-input-channel", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get last Java input channel of the multi-input set
        /// Maps to GET /audio/java/last-input-channel
        /// </summary>
        public async Task<int> GetJavaLastInputChannel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/last-input-channel", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("channel", out var channel) ? channel.GetInt32() : 0;
        }

        /// <summary>
        /// Set last Java input channel of the multi-input set
        /// Maps to POST /audio/java/last-input-channel
        /// </summary>
        public async Task<bool> SetJavaLastInputChannel(int channel, CancellationToken cancellationToken = default)
        {
            var requestData = new { channel };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/last-input-channel", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get current Java output channel
        /// Maps to GET /audio/java/output-channel
        /// </summary>
        public async Task<string> GetJavaOutputChannel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/output-channel", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("channel", out var channel) ? channel.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java output channel
        /// Maps to POST /audio/java/output-channel
        /// </summary>
        public async Task<bool> SetJavaOutputChannel(string channel, CancellationToken cancellationToken = default)
        {
            var requestData = new { channel };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/output-channel", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get current Java reference output channel
        /// Maps to GET /audio/java/ref-output-channel
        /// </summary>
        public async Task<string> GetJavaRefOutputChannel(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/ref-output-channel", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("channel", out var channel) ? channel.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set Java reference output channel
        /// Maps to POST /audio/java/ref-output-channel
        /// </summary>
        public async Task<bool> SetJavaRefOutputChannel(string channel, CancellationToken cancellationToken = default)
        {
            var requestData = new { channel };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/ref-output-channel", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get Java output channel mapping
        /// Maps to GET /audio/java/output-channel-mapping
        /// </summary>
        public async Task<List<string>> GetJavaOutputChannelMapping(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/java/output-channel-mapping", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("mapping", out var mapping) 
                ? JsonSerializer.Deserialize<List<string>>(mapping.GetRawText()) ?? new List<string>()
                : new List<string>();
        }

        /// <summary>
        /// Set Java output channel mapping
        /// Maps to POST /audio/java/output-channel-mapping
        /// </summary>
        public async Task<bool> SetJavaOutputChannelMapping(List<string> mapping, CancellationToken cancellationToken = default)
        {
            var requestData = new { mapping };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/java/output-channel-mapping", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region ASIO Audio Device Configuration

        /// <summary>
        /// Get available ASIO commands
        /// Maps to GET /audio/asio/commands
        /// </summary>
        public async Task<List<string>> GetASIOCommands(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/commands", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Send an ASIO command
        /// Maps to POST /audio/asio/command
        /// </summary>
        public async Task<bool> SendASIOCommand(string command, CancellationToken cancellationToken = default)
        {
            var request = new { command };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/command", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get current ASIO device
        /// Maps to GET /audio/asio/device
        /// </summary>
        public async Task<string> GetASIODevice(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/device", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("device", out var device) ? device.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO device
        /// Maps to POST /audio/asio/device
        /// </summary>
        public async Task<bool> SetASIODevice(string device, CancellationToken cancellationToken = default)
        {
            var request = new { device };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/device", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available ASIO devices
        /// Maps to GET /audio/asio/devices
        /// </summary>
        public async Task<List<string>> GetASIODevices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/devices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Get current ASIO input
        /// Maps to GET /audio/asio/input
        /// </summary>
        public async Task<string> GetASIOInput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/input", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("input", out var input) ? input.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO input
        /// Maps to POST /audio/asio/input
        /// </summary>
        public async Task<bool> SetASIOInput(string input, CancellationToken cancellationToken = default)
        {
            var request = new { input };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/input", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available ASIO inputs
        /// Maps to GET /audio/asio/inputs
        /// </summary>
        public async Task<List<string>> GetASIOInputs(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/inputs", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        /// <summary>
        /// Get current ASIO output
        /// Maps to GET /audio/asio/output
        /// </summary>
        public async Task<string> GetASIOOutput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/output", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("output", out var output) ? output.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO output
        /// Maps to POST /audio/asio/output
        /// </summary>
        public async Task<bool> SetASIOOutput(string output, CancellationToken cancellationToken = default)
        {
            var request = new { output };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/output", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available ASIO outputs
        /// Maps to GET /audio/asio/outputs
        /// </summary>
        public async Task<List<string>> GetASIOOutputs(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/outputs", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }

        #endregion

        #region ASIO Audio Advanced Configuration

        /// <summary>
        /// Get ASIO reference input
        /// Maps to GET /audio/asio/ref-input
        /// </summary>
        public async Task<string> GetASIORefInput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/ref-input", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("input", out var input) ? input.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO reference input
        /// Maps to POST /audio/asio/ref-input
        /// </summary>
        public async Task<bool> SetASIORefInput(string input, CancellationToken cancellationToken = default)
        {
            var request = new { input };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/ref-input", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get ASIO last input for multi-input set
        /// Maps to GET /audio/asio/last-input
        /// </summary>
        public async Task<string> GetASIOLastInput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/last-input", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("input", out var input) ? input.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO last input for multi-input set
        /// Maps to POST /audio/asio/last-input
        /// </summary>
        public async Task<bool> SetASIOLastInput(string input, CancellationToken cancellationToken = default)
        {
            var request = new { input };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/last-input", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get ASIO reference output
        /// Maps to GET /audio/asio/ref-output
        /// </summary>
        public async Task<string> GetASIORefOutput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/ref-output", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("output", out var output) ? output.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO reference output
        /// Maps to POST /audio/asio/ref-output
        /// </summary>
        public async Task<bool> SetASIORefOutput(string output, CancellationToken cancellationToken = default)
        {
            var request = new { output };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/ref-output", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get ASIO secondary output
        /// Maps to GET /audio/asio/secondary-output
        /// </summary>
        public async Task<string> GetASIOSecondaryOutput(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/secondary-output", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("output", out var output) ? output.GetString() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Set ASIO secondary output
        /// Maps to POST /audio/asio/secondary-output
        /// </summary>
        public async Task<bool> SetASIOSecondaryOutput(string output, CancellationToken cancellationToken = default)
        {
            var request = new { output };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/secondary-output", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get ASIO secondary output enable status
        /// Maps to GET /audio/asio/secondary-output-enable
        /// </summary>
        public async Task<bool> GetASIOSecondaryOutputEnable(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio/secondary-output-enable", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.TryGetProperty("enable", out var enable) && enable.GetBoolean();
        }

        /// <summary>
        /// Set ASIO secondary output enable status
        /// Maps to POST /audio/asio/secondary-output-enable
        /// </summary>
        public async Task<bool> SetASIOSecondaryOutputEnable(bool enable, CancellationToken cancellationToken = default)
        {
            var request = new { enable };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/asio/secondary-output-enable", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Audio Calibration Configuration

        /// <summary>
        /// Get input calibration configuration
        /// Maps to GET /audio/input-cal
        /// </summary>
        public async Task<JsonElement> GetInputCalibrationConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/input-cal", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        /// <summary>
        /// Set input calibration configuration settings
        /// Maps to PUT /audio/input-cal
        /// </summary>
        public async Task<bool> SetInputCalibrationConfiguration(object configuration, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(configuration), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/input-cal", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get output calibration configuration
        /// Maps to GET /audio/output-cal
        /// </summary>
        public async Task<JsonElement> GetOutputCalibrationConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/output-cal", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        /// <summary>
        /// Set output calibration configuration settings
        /// Maps to PUT /audio/output-cal
        /// </summary>
        public async Task<bool> SetOutputCalibrationConfiguration(object configuration, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(configuration), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/output-cal", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get audio input options
        /// Maps to GET /audio/input-options
        /// </summary>
        public async Task<JsonElement> GetAudioInputOptions(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/input-options", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        /// <summary>
        /// Set audio input options (complete replacement)
        /// Maps to POST /audio/input-options
        /// </summary>
        public async Task<bool> SetAudioInputOptions(object options, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(options), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/input-options", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update audio input options (partial update)
        /// Maps to PUT /audio/input-options
        /// </summary>
        public async Task<bool> UpdateAudioInputOptions(object options, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(options), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/input-options", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get audio output options
        /// Maps to GET /audio/output-options
        /// </summary>
        public async Task<JsonElement> GetAudioOutputOptions(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/output-options", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        /// <summary>
        /// Set audio output options (complete replacement)
        /// Maps to POST /audio/output-options
        /// </summary>
        public async Task<bool> SetAudioOutputOptions(object options, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(options), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/audio/output-options", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update audio output options (partial update)
        /// Maps to PUT /audio/output-options
        /// </summary>
        public async Task<bool> UpdateAudioOutputOptions(object options, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(JsonSerializer.Serialize(options), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/output-options", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        /// <summary>
        /// Get ASIO audio configuration
        /// Maps to GET /audio/asio
        /// </summary>
        public async Task<ASIOAudioConfig> GetASIOConfig(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/asio", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new ASIOAudioConfig
            {
                Device = data.TryGetProperty("device", out var device) ? device.GetString() : null,
                Input = data.TryGetProperty("input", out var input) ? input.GetString() : null,
                Output = data.TryGetProperty("output", out var output) ? output.GetString() : null,
                RefOutput = data.TryGetProperty("refOutput", out var refOut) ? refOut.GetString() : null,
                RefInput = data.TryGetProperty("refInput", out var refIn) ? refIn.GetString() : null,
                LastInput = data.TryGetProperty("lastInput", out var lastIn) ? lastIn.GetString() : null,
                SecondaryOutputEnabled = data.TryGetProperty("secondaryOutputEnabled", out var secEnabled) && secEnabled.GetBoolean(),
                SecondaryOutput = data.TryGetProperty("secondaryOutput", out var secOut) ? secOut.GetString() : null
            };
        }

        /// <summary>
        /// Update ASIO audio settings
        /// Maps to PUT /audio/asio
        /// </summary>
        public async Task UpdateASIOConfig(ASIOAudioConfig config, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                await UpdateASIOConfigInternal(config, cancellationToken);
                _cachedState = null;
            }, "Update ASIO Audio Config", cancellationToken);
        }

        /// <summary>
        /// Reload ASIO driver (useful after hardware changes)
        /// Maps to POST /audio/asio/reload-driver
        /// </summary>
        public async Task ReloadASIODriver(CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                await ReloadASIODriverInternal(cancellationToken);
                _cachedState = null;
            }, "Reload ASIO Driver", cancellationToken);
        }

        /// <summary>
        /// Update input calibration settings
        /// Maps to PUT /audio/input-calibration
        /// </summary>
        public async Task UpdateInputCalibration(CalibrationConfig config, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                var requestData = new
                {
                    enabled = config.Enabled,
                    calibrationFile = config.CalibrationFile,
                    offsetdB = config.OffsetdB,
                    delayMs = config.DelayMs
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/audio/input-calibration", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _cachedState = null;
            }, "Update Input Calibration", cancellationToken);
        }

        /// <summary>
        /// Update output calibration settings
        /// Maps to PUT /audio/output-calibration
        /// </summary>
        public async Task UpdateOutputCalibration(CalibrationConfig config, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                var requestData = new
                {
                    enabled = config.Enabled,
                    calibrationFile = config.CalibrationFile,
                    offsetdB = config.OffsetdB,
                    delayMs = config.DelayMs
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/audio/output-calibration", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                _cachedState = null;
            }, "Update Output Calibration", cancellationToken);
        }

        /// <summary>
        /// Refresh cached audio state from REW
        /// Call after external changes or errors
        /// </summary>
        public async Task RefreshState(CancellationToken cancellationToken = default)
        {
            _cachedState = null;
            await GetState(forceRefresh: true, cancellationToken);
        }

        /// <summary>
        /// Get current audio state (cached with refresh option)
        /// Maps to GET /audio
        /// </summary>
        public async Task<AudioState> GetState(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            if (!forceRefresh && _cachedState != null && 
                DateTime.UtcNow - _lastStateRefresh < _cacheTimeout)
            {
                return _cachedState;
            }

            var response = await _httpClient.GetAsync($"{_baseUrl}/audio", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            _cachedState = new AudioState
            {
                AudioEnabled = data.TryGetProperty("audioEnabled", out var enabled) && enabled.GetBoolean(),
                AudioEndpointsReady = data.TryGetProperty("audioEndpointsReady", out var ready) && ready.GetBoolean(),
                Driver = Enum.TryParse<AudioDriverType>(
                    data.TryGetProperty("driver", out var driver) ? driver.GetString() : "Java",
                    true, out var driverType) ? driverType : AudioDriverType.Java,
                SampleRate = data.TryGetProperty("sampleRate", out var rate) ? rate.GetInt32() : 0,
                SupportedSampleRates = data.TryGetProperty("supportedSampleRates", out var rates)
                    ? JsonSerializer.Deserialize<int[]>(rates.GetRawText()) ?? Array.Empty<int>()
                    : Array.Empty<int>(),
                Treat32BitAs24Bit = data.TryGetProperty("treat32BitAs24Bit", out var treat) && treat.GetBoolean(),
                Java = data.TryGetProperty("java", out var javaElement) ? ParseJavaAudioConfig(javaElement) : null,
                ASIO = data.TryGetProperty("asio", out var asioElement) ? ParseASIOAudioConfig(asioElement) : null
            };

            _lastStateRefresh = DateTime.UtcNow;
            return _cachedState;
        }

        // Internal helper methods

        private async Task<int[]> GetSupportedSampleRatesInternal(CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/audio/sample-rates", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
        }

        private async Task SetDriverInternal(AudioDriverType driver, CancellationToken cancellationToken)
        {
            var requestData = new { driver = driver.ToString().ToLowerInvariant() };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/driver", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task SetSampleRateInternal(int sampleRate, CancellationToken cancellationToken)
        {
            var requestData = new { sampleRate, unit = "Hz" };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/sample-rate", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task ConfigureInternal(CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/audio/configure",
                new StringContent("", Encoding.UTF8, "application/json"),
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpdateJavaConfigInternal(JavaAudioConfig config, CancellationToken cancellationToken)
        {
            var requestData = new
            {
                inputDevice = config.InputDevice,
                input = config.Input,
                outputDevice = config.OutputDevice,
                output = config.Output,
                inputChannel = config.InputChannel,
                refInputChannel = config.RefInputChannel,
                lastInputChannel = config.LastInputChannel,
                outputChannel = config.OutputChannel,
                refOutputChannel = config.RefOutputChannel,
                outputChannelMapping = config.OutputChannelMapping,
                stereoOnly = config.StereoOnly
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/java", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpdateASIOConfigInternal(ASIOAudioConfig config, CancellationToken cancellationToken)
        {
            var requestData = new
            {
                device = config.Device,
                input = config.Input,
                output = config.Output,
                refOutput = config.RefOutput,
                refInput = config.RefInput,
                lastInput = config.LastInput,
                secondaryOutputEnabled = config.SecondaryOutputEnabled,
                secondaryOutput = config.SecondaryOutput
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/audio/asio", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task ReloadASIODriverInternal(CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/audio/asio/reload-driver",
                new StringContent("", Encoding.UTF8, "application/json"),
                cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private static JavaAudioConfig ParseJavaAudioConfig(JsonElement data)
        {
            return new JavaAudioConfig
            {
                InputDevice = data.TryGetProperty("inputDevice", out var inputDev) ? inputDev.GetString() : null,
                Input = data.TryGetProperty("input", out var input) ? input.GetString() : null,
                OutputDevice = data.TryGetProperty("outputDevice", out var outputDev) ? outputDev.GetString() : null,
                Output = data.TryGetProperty("output", out var output) ? output.GetString() : null,
                InputChannel = data.TryGetProperty("inputChannel", out var inputCh) ? inputCh.GetInt32() : 0,
                RefInputChannel = data.TryGetProperty("refInputChannel", out var refInputCh) ? refInputCh.GetInt32() : 0,
                LastInputChannel = data.TryGetProperty("lastInputChannel", out var lastInputCh) ? lastInputCh.GetInt32() : 0,
                OutputChannel = data.TryGetProperty("outputChannel", out var outputCh) ? outputCh.GetString() : null,
                RefOutputChannel = data.TryGetProperty("refOutputChannel", out var refOutputCh) ? refOutputCh.GetString() : null,
                OutputChannelMapping = ParseStringArray(data, "outputChannelMapping"),
                StereoOnly = data.TryGetProperty("stereoOnly", out var stereo) && stereo.GetBoolean()
            };
        }

        private static ASIOAudioConfig ParseASIOAudioConfig(JsonElement data)
        {
            return new ASIOAudioConfig
            {
                Device = data.TryGetProperty("device", out var device) ? device.GetString() : null,
                Input = data.TryGetProperty("input", out var input) ? input.GetString() : null,
                Output = data.TryGetProperty("output", out var output) ? output.GetString() : null,
                RefOutput = data.TryGetProperty("refOutput", out var refOut) ? refOut.GetString() : null,
                RefInput = data.TryGetProperty("refInput", out var refIn) ? refIn.GetString() : null,
                LastInput = data.TryGetProperty("lastInput", out var lastIn) ? lastIn.GetString() : null,
                SecondaryOutputEnabled = data.TryGetProperty("secondaryOutputEnabled", out var secEnabled) && secEnabled.GetBoolean(),
                SecondaryOutput = data.TryGetProperty("secondaryOutput", out var secOut) ? secOut.GetString() : null
            };
        }

        private static List<string> ParseStringArray(JsonElement data, string propertyName)
        {
            if (data.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                return arrayElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => s != null)
                    .ToList();
            }
            return new List<string>();
        }
    }
}