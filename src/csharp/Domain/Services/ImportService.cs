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
    /// Service for importing various measurement data into REW
    /// Handles sweep recordings, frequency responses, impulse responses, and RTA files
    /// ISO/IEC/IEEE 12207:2017 - Implementation Process
    /// </summary>
    public class ImportService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<ImportService>? _logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ImportService(CommandGate commandGate, HttpClient httpClient, string baseUrl, ILogger<ImportService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
        }

        #region Sweep Recordings Import

        /// <summary>
        /// Gets the current sweep stimulus file path
        /// GET /import/sweep-recordings/stimulus
        /// </summary>
        public async Task<string> GetSweepStimulusAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/sweep-recordings/stimulus";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the sweep stimulus file path for import
        /// POST /import/sweep-recordings/stimulus
        /// </summary>
        public async Task<bool> SetSweepStimulusAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/sweep-recordings/stimulus";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the current sweep response file path
        /// GET /import/sweep-recordings/response
        /// </summary>
        public async Task<string> GetSweepResponseAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/sweep-recordings/response";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the sweep response file path for import
        /// POST /import/sweep-recordings/response
        /// </summary>
        public async Task<bool> SetSweepResponseAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/sweep-recordings/response";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Frequency Response Import

        /// <summary>
        /// Gets the current frequency response file path
        /// GET /import/frequency-response
        /// </summary>
        public async Task<string> GetFrequencyResponseFileAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/frequency-response";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the frequency response file path for import
        /// POST /import/frequency-response
        /// </summary>
        public async Task<bool> SetFrequencyResponseFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/frequency-response";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the current frequency response data file path
        /// GET /import/frequency-response-data
        /// </summary>
        public async Task<string> GetFrequencyResponseDataFileAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/frequency-response-data";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the frequency response data file path for import
        /// POST /import/frequency-response-data
        /// </summary>
        public async Task<bool> SetFrequencyResponseDataFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/frequency-response-data";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region Impulse Response Import

        /// <summary>
        /// Gets the current impulse response file path
        /// GET /import/impulse-response
        /// </summary>
        public async Task<string> GetImpulseResponseFileAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/impulse-response";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the impulse response file path for import
        /// POST /import/impulse-response
        /// </summary>
        public async Task<bool> SetImpulseResponseFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/impulse-response";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the current impulse response data file path
        /// GET /import/impulse-response-data
        /// </summary>
        public async Task<string> GetImpulseResponseDataFileAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/impulse-response-data";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the impulse response data file path for import
        /// POST /import/impulse-response-data
        /// </summary>
        public async Task<bool> SetImpulseResponseDataFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/impulse-response-data";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        #endregion

        #region RTA File Import

        /// <summary>
        /// Gets the current RTA file path for import
        /// GET /import/rta-file
        /// </summary>
        public async Task<string> GetRTAFileAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/rta-file";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<string>(content, JsonOptions) ?? string.Empty;
        }

        /// <summary>
        /// Sets the RTA file path for import
        /// POST /import/rta-file
        /// </summary>
        public async Task<bool> SetRTAFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var url = $"{_baseUrl}/import/rta-file";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(filePath),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets the RTA file import progress (0-100)
        /// GET /import/rta-file/progress
        /// </summary>
        public async Task<int> GetRTAFileProgressAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/rta-file/progress";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int>(content, JsonOptions);
        }

        /// <summary>
        /// Gets available RTA file save options
        /// GET /import/rta-file/save-options
        /// Returns options like: "Current", "Peak", "Both"
        /// </summary>
        public async Task<List<string>> GetRTAFileSaveOptionsAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/rta-file/save-options";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribes to import events
        /// POST /import/subscribe
        /// </summary>
        public async Task<bool> SubscribeAsync(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/import/subscribe";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { callbackUrl }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Unsubscribes from import events
        /// POST /import/unsubscribe
        /// </summary>
        public async Task<bool> UnsubscribeAsync(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/import/unsubscribe";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { callbackUrl }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Gets list of current import event subscribers
        /// GET /import/subscribers
        /// </summary>
        public async Task<List<string>> GetSubscribersAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/import/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        #endregion
    }
}
