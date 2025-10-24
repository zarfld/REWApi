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
        /// POST /input-levels/command
        /// REW API expects: {"command": "Start"}
        /// </summary>
        public async Task<bool> StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/input-levels/command";
                
                // REW API expects command object: {"command": "Start"}
                var commandObject = new { command = "Start" };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(commandObject),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
                response.EnsureSuccessStatusCode();
                return true;
            }, "Start Input Levels Monitoring", cancellationToken);
        }

        /// <summary>
        /// Stop monitoring input levels
        /// POST /input-levels/command
        /// REW API expects: {"command": "Stop"}
        /// </summary>
        public async Task<bool> StopMonitoringAsync(CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/input-levels/command";
                
                // REW API expects command object: {"command": "Stop"}
                var commandObject = new { command = "Stop" };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(commandObject),
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
        /// POST /input-levels/subscribe
        /// REW API expects: {"url": "...", "parameters": {"unit": "..."}}
        /// </summary>
        /// <param name="subscriberUrl">URL to receive notifications</param>
        /// <param name="unit">Optional unit for level values (e.g., "dBFS", "dBV", "SPL"). If omitted, defaults to dBFS.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<bool> SubscribeAsync(string subscriberUrl, string? unit = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/input-levels/subscribe";
            
            // Build subscription request matching REW API spec
            var subscriptionData = new Dictionary<string, object>
            {
                ["url"] = subscriberUrl
            };
            
            // Add optional parameters if specified
            if (!string.IsNullOrWhiteSpace(unit))
            {
                subscriptionData["parameters"] = new Dictionary<string, string>
                {
                    ["unit"] = unit
                };
            }
            
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(subscriptionData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Unsubscribe from input level change notifications
        /// POST /input-levels/unsubscribe
        /// REW API expects: {"url": "..."}
        /// </summary>
        /// <param name="subscriberUrl">URL to stop receiving notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<bool> UnsubscribeAsync(string subscriberUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var url = $"{_baseUrl}/input-levels/unsubscribe";
            
            // REW API expects {"url": "..."}
            var subscriptionData = new Dictionary<string, string>
            {
                ["url"] = subscriberUrl
            };
            
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(subscriptionData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }

        /// <summary>
        /// Get list of current subscribers
        /// GET /input-levels/subscribers
        /// REW API returns: [{"url": "..."}, ...]
        /// </summary>
        public async Task<List<string>> GetSubscribersAsync(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/input-levels/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content, JsonOptions) ?? new List<Dictionary<string, string>>();
            
            // Extract URLs from subscriber objects (REW API uses "url" property)
            var result = new List<string>();
            foreach (var subscriber in subscribers)
            {
                if (subscriber.ContainsKey("url"))
                    result.Add(subscriber["url"]);
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
