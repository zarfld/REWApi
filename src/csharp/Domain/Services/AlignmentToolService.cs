using System;
using System.Collections.Generic;
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
    /// Service for REW Alignment Tool operations
    /// ISO/IEC/IEEE 12207:2017 - Software Implementation
    /// 
    /// Provides access to speaker/subwoofer alignment functionality:
    /// - Alignment commands (level phase, align phase, filter IRs, etc.)
    /// - Alignment modes (Phase/Impulse)
    /// - Measurement configuration (A/B channels, gain, delay, inversion)
    /// - Timing parameters (frequency, delays, time delay removal)
    /// - Results retrieval (aligned frequency response, filtered impulse responses)
    /// </summary>
    public class AlignmentToolService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<AlignmentToolService>? _logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of the AlignmentToolService
        /// </summary>
        public AlignmentToolService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            ILogger<AlignmentToolService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
        }

        #region Commands

        /// <summary>
        /// Gets the list of available alignment commands
        /// GET /alignment-tool/commands
        /// </summary>
        public async Task<List<string>> GetCommandsAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/commands";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        /// <summary>
        /// Sends an alignment command to execute
        /// POST /alignment-tool/command
        /// Commands: "Level phase", "Align phase", "Filter IRs", "Align IRs", etc.
        /// </summary>
        public async Task<bool> SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var url = $"{_baseUrl}/alignment-tool/command";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(command),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Modes

        /// <summary>
        /// Gets the list of available alignment modes
        /// GET /alignment-tool/modes
        /// </summary>
        public async Task<List<string>> GetModesAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/modes";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        /// <summary>
        /// Gets the current alignment mode
        /// GET /alignment-tool/mode
        /// Returns: "Phase" or "Impulse"
        /// </summary>
        public async Task<string> GetModeAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/mode";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the alignment mode
        /// POST /alignment-tool/mode
        /// </summary>
        public async Task<bool> SetModeAsync(string mode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(mode))
                throw new ArgumentException("Mode cannot be null or empty", nameof(mode));

            var url = $"{_baseUrl}/alignment-tool/mode";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(mode),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Timing Parameters

        /// <summary>
        /// Gets the alignment frequency
        /// GET /alignment-tool/frequency
        /// </summary>
        public async Task<double> GetFrequencyAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/frequency";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Gets the maximum negative delay
        /// GET /alignment-tool/max-negative-delay
        /// </summary>
        public async Task<double> GetMaxNegativeDelayAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/max-negative-delay";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the maximum negative delay
        /// POST /alignment-tool/max-negative-delay
        /// </summary>
        public async Task<bool> SetMaxNegativeDelayAsync(double delay, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/max-negative-delay";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(delay),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the maximum positive delay
        /// GET /alignment-tool/max-positive-delay
        /// </summary>
        public async Task<double> GetMaxPositiveDelayAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/max-positive-delay";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the maximum positive delay
        /// POST /alignment-tool/max-positive-delay
        /// </summary>
        public async Task<bool> SetMaxPositiveDelayAsync(double delay, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/max-positive-delay";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(delay),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets whether to remove time delay
        /// GET /alignment-tool/remove-time-delay
        /// </summary>
        public async Task<bool> GetRemoveTimeDelayAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/remove-time-delay";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content, JsonOptions);
        }

        /// <summary>
        /// Sets whether to remove time delay
        /// POST /alignment-tool/remove-time-delay
        /// </summary>
        public async Task<bool> SetRemoveTimeDelayAsync(bool removeDelay, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/remove-time-delay";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(removeDelay),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Channel A Configuration

        /// <summary>
        /// Gets the measurement index for channel A
        /// GET /alignment-tool/index-a
        /// </summary>
        public async Task<int> GetIndexAAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/index-a";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the measurement index for channel A
        /// POST /alignment-tool/index-a
        /// </summary>
        public async Task<bool> SetIndexAAsync(int index, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/index-a";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(index),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the measurement UUID for channel A
        /// GET /alignment-tool/uuid-a
        /// </summary>
        public async Task<string> GetUuidAAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/uuid-a";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the measurement UUID for channel A
        /// POST /alignment-tool/uuid-a
        /// </summary>
        public async Task<bool> SetUuidAAsync(string uuid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentException("UUID cannot be null or empty", nameof(uuid));

            var url = $"{_baseUrl}/alignment-tool/uuid-a";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(uuid),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the gain for channel A
        /// GET /alignment-tool/gain-a
        /// </summary>
        public async Task<double> GetGainAAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/gain-a";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the gain for channel A
        /// POST /alignment-tool/gain-a
        /// </summary>
        public async Task<bool> SetGainAAsync(double gain, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/gain-a";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(gain),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets whether channel A is inverted
        /// GET /alignment-tool/invert-a
        /// </summary>
        public async Task<bool> GetInvertAAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/invert-a";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content, JsonOptions);
        }

        /// <summary>
        /// Sets whether channel A is inverted
        /// POST /alignment-tool/invert-a
        /// </summary>
        public async Task<bool> SetInvertAAsync(bool invert, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/invert-a";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(invert),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Channel B Configuration

        /// <summary>
        /// Gets the measurement index for channel B
        /// GET /alignment-tool/index-b
        /// </summary>
        public async Task<int> GetIndexBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/index-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the measurement index for channel B
        /// POST /alignment-tool/index-b
        /// </summary>
        public async Task<bool> SetIndexBAsync(int index, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/index-b";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(index),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the measurement UUID for channel B
        /// GET /alignment-tool/uuid-b
        /// </summary>
        public async Task<string> GetUuidBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/uuid-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the measurement UUID for channel B
        /// POST /alignment-tool/uuid-b
        /// </summary>
        public async Task<bool> SetUuidBAsync(string uuid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentException("UUID cannot be null or empty", nameof(uuid));

            var url = $"{_baseUrl}/alignment-tool/uuid-b";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(uuid),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the gain for channel B
        /// GET /alignment-tool/gain-b
        /// </summary>
        public async Task<double> GetGainBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/gain-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the gain for channel B
        /// POST /alignment-tool/gain-b
        /// </summary>
        public async Task<bool> SetGainBAsync(double gain, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/gain-b";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(gain),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the delay for channel B
        /// GET /alignment-tool/delay-b
        /// </summary>
        public async Task<double> GetDelayBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/delay-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(content, JsonOptions);
        }

        /// <summary>
        /// Sets the delay for channel B
        /// POST /alignment-tool/delay-b
        /// </summary>
        public async Task<bool> SetDelayBAsync(double delay, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/delay-b";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(delay),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets whether channel B is inverted
        /// GET /alignment-tool/invert-b
        /// </summary>
        public async Task<bool> GetInvertBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/invert-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(content, JsonOptions);
        }

        /// <summary>
        /// Sets whether channel B is inverted
        /// POST /alignment-tool/invert-b
        /// </summary>
        public async Task<bool> SetInvertBAsync(bool invert, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/invert-b";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(invert),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Results

        /// <summary>
        /// Gets the alignment result
        /// GET /alignment-tool/result
        /// </summary>
        public async Task<dynamic> GetResultAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/result";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(content, JsonOptions)!;
        }

        /// <summary>
        /// Gets the aligned frequency response
        /// GET /alignment-tool/aligned-frequency-response
        /// </summary>
        public async Task<string> GetAlignedFrequencyResponseAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/aligned-frequency-response";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Gets the filtered impulse response for channel A
        /// GET /alignment-tool/filtered-impulse-response-a
        /// </summary>
        public async Task<string> GetFilteredImpulseResponseAAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/filtered-impulse-response-a";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Gets the filtered impulse response for channel B
        /// GET /alignment-tool/filtered-impulse-response-b
        /// </summary>
        public async Task<string> GetFilteredImpulseResponseBAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/alignment-tool/filtered-impulse-response-b";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        #endregion
    }
}
