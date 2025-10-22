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
    /// Service for monitoring audio input levels in REW
    /// ISO/IEC/IEEE 12207:2017 - Software Implementation
    /// 
    /// Provides access to REW Input Levels monitoring:
    /// - Start/Stop monitoring
    /// - Get current input levels
    /// - Subscribe to level updates
    /// - Query available units
    /// </summary>
    public class InputLevelsService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<InputLevelsService>? _logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        internal InputLevelsService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            ILogger<InputLevelsService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
        }

        #region Commands

        /// <summary>
        /// Get available input levels commands
        /// </summary>
        public async Task<List<string>> GetCommandsAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/input-levels/commands";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        /// <summary>
        /// Start monitoring input levels
        /// </summary>
        public async Task<bool> StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/input-levels/command";
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize("Start"),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
                response.EnsureSuccessStatusCode();
                return true;
            }, "Start Input Levels Monitoring", cancellationToken);
        }

        /// <summary>
        /// Stop monitoring input levels
        /// </summary>
        public async Task<bool> StopMonitoringAsync(CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/input-levels/command";
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize("Stop"),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
                response.EnsureSuccessStatusCode();
                return true;
            }, "Stop Input Levels Monitoring", cancellationToken);
        }

        #endregion

        #region Level Reading

        /// <summary>
        /// Get the last recorded input levels
        /// Returns a dictionary with channel names as keys and level values
        /// </summary>
        public async Task<Dictionary<string, double>> GetLastLevelsAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/input-levels/last-levels";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, double>>(content, JsonOptions) ?? new Dictionary<string, double>();
        }

        /// <summary>
        /// Get available units for input level measurements
        /// </summary>
        public async Task<List<string>> GetUnitsAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/input-levels/units";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content, JsonOptions) ?? new List<string>();
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Subscribe to input level change notifications
        /// </summary>
        /// <param name="callbackUrl">URL to receive notifications</param>
        public async Task<bool> SubscribeAsync(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/input-levels/subscribe";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { callbackUrl }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Unsubscribe from input level change notifications
        /// </summary>
        /// <param name="callbackUrl">URL to stop receiving notifications</param>
        public async Task<bool> UnsubscribeAsync(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/input-levels/unsubscribe";
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(new { callbackUrl }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Get list of current subscribers
        /// </summary>
        public async Task<List<string>> GetSubscribersAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/input-levels/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content, JsonOptions) ?? new List<Dictionary<string, string>>();
            
            // Extract callback URLs from subscriber objects
            var result = new List<string>();
            foreach (var subscriber in subscribers)
            {
                if (subscriber.ContainsKey("callbackUrl"))
                    result.Add(subscriber["callbackUrl"]);
            }
            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if input level monitoring is currently active
        /// </summary>
        public async Task<bool> IsMonitoringAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var levels = await GetLastLevelsAsync(cancellationToken);
                // If we get levels data, monitoring is active
                return levels.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
