using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// REW SPL Meter service providing sound pressure level monitoring
    /// Maps to /spl-meter/* endpoints in REW API
    /// Supports up to 4 SPL meters (Pro upgrade) or 1 without
    /// ISO/IEC/IEEE 12207:2017 - Implementation Process
    /// </summary>
    public class SPLMeterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SPLMeterService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Command Operations

        /// <summary>
        /// Get list of available SPL meter commands
        /// GET /spl-meter/commands
        /// </summary>
        /// <returns>List of command names (Start, Stop, etc.)</returns>
        public async Task<List<string>> GetCommands()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/commands");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content);
        }

        /// <summary>
        /// Execute command on specific SPL meter (Start, Stop)
        /// POST /spl-meter/{meterNumber}/command
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        /// <param name="command">Command to execute (Start, Stop)</param>
        public async Task ExecuteCommand(int meterNumber, string command)
        {
            ValidateMeterNumber(meterNumber);
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var json = JsonSerializer.Serialize(new { command });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/spl-meter/{meterNumber}/command", content);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Start SPL meter monitoring
        /// Convenience method for ExecuteCommand(meterNumber, "Start")
        /// </summary>
        public async Task Start(int meterNumber = 1)
        {
            await ExecuteCommand(meterNumber, "Start");
        }

        /// <summary>
        /// Stop SPL meter monitoring
        /// Convenience method for ExecuteCommand(meterNumber, "Stop")
        /// </summary>
        public async Task Stop(int meterNumber = 1)
        {
            await ExecuteCommand(meterNumber, "Stop");
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Get SPL meter configuration
        /// GET /spl-meter/{meterNumber}/configuration
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        public async Task<SPLMeterConfiguration> GetConfiguration(int meterNumber = 1)
        {
            ValidateMeterNumber(meterNumber);
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/{meterNumber}/configuration");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SPLMeterConfiguration>(content);
        }

        /// <summary>
        /// Update SPL meter configuration
        /// POST /spl-meter/{meterNumber}/configuration
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        /// <param name="configuration">Configuration with mode, weighting, filter settings</param>
        public async Task UpdateConfiguration(int meterNumber, SPLMeterConfiguration configuration)
        {
            ValidateMeterNumber(meterNumber);
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            if (!configuration.IsValid())
                throw new ArgumentException("Invalid SPL meter configuration", nameof(configuration));

            var json = JsonSerializer.Serialize(configuration);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/spl-meter/{meterNumber}/configuration", content);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Available Options

        /// <summary>
        /// Get list of available SPL meter display modes
        /// GET /spl-meter/modes
        /// </summary>
        /// <returns>List of mode names (SPL, etc.)</returns>
        public async Task<List<string>> GetModes()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/modes");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content);
        }

        /// <summary>
        /// Get list of available weighting options
        /// GET /spl-meter/weightings
        /// </summary>
        /// <returns>List of weighting names (A, C, Z)</returns>
        public async Task<List<string>> GetWeightings()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/weightings");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content);
        }

        /// <summary>
        /// Get list of available filter options
        /// GET /spl-meter/filters
        /// </summary>
        /// <returns>List of filter names (Fast, Slow, Impulse)</returns>
        public async Task<List<string>> GetFilters()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/filters");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content);
        }

        #endregion

        #region Data Retrieval

        /// <summary>
        /// Get last SPL meter levels update
        /// GET /spl-meter/{meterNumber}/levels
        /// Returns SPLValues with SPL, Leq, SEL figures
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        public async Task<SPLValues> GetLevels(int meterNumber = 1)
        {
            ValidateMeterNumber(meterNumber);
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/{meterNumber}/levels");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SPLValues>(content);
        }

        #endregion

        #region Subscription Methods

        /// <summary>
        /// Subscribe to SPL meter level change notifications
        /// POST /spl-meter/{id}/subscribe
        /// Subscriber receives updates when meter levels change
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        /// <param name="subscriberUrl">Callback URL for notifications</param>
        public async Task SubscribeToSPLMeter(int meterNumber, string subscriberUrl)
        {
            ValidateMeterNumber(meterNumber);
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var json = JsonSerializer.Serialize(new { url = subscriberUrl });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/spl-meter/{meterNumber}/subscribe", content);
            response.EnsureSuccessStatusCode();
            
            Console.WriteLine($"Subscribed to SPL meter {meterNumber}: {subscriberUrl}");
        }

        /// <summary>
        /// Unsubscribe from SPL meter level change notifications
        /// POST /spl-meter/{id}/unsubscribe
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        /// <param name="subscriberUrl">Callback URL to remove</param>
        public async Task UnsubscribeFromSPLMeter(int meterNumber, string subscriberUrl)
        {
            ValidateMeterNumber(meterNumber);
            if (string.IsNullOrWhiteSpace(subscriberUrl))
                throw new ArgumentException("Subscriber URL cannot be null or empty", nameof(subscriberUrl));

            var json = JsonSerializer.Serialize(new { url = subscriberUrl });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/spl-meter/{meterNumber}/unsubscribe", content);
            response.EnsureSuccessStatusCode();
            
            Console.WriteLine($"Unsubscribed from SPL meter {meterNumber}: {subscriberUrl}");
        }

        /// <summary>
        /// Get list of current SPL meter subscribers
        /// GET /spl-meter/{id}/subscribers
        /// </summary>
        /// <param name="meterNumber">Meter ID (1-4)</param>
        /// <returns>Array of subscriber URLs</returns>
        public async Task<string[]> GetSPLMeterSubscribers(int meterNumber)
        {
            ValidateMeterNumber(meterNumber);
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/spl-meter/{meterNumber}/subscribers");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var subscribers = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content) 
                ?? new List<Dictionary<string, string>>();
            
            // Extract url property from each Subscriber object
            return subscribers
                .Where(s => s.ContainsKey("url"))
                .Select(s => s["url"])
                .ToArray();
        }

        #endregion

        #region Validation

        private void ValidateMeterNumber(int meterNumber)
        {
            if (meterNumber < 1 || meterNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(meterNumber), 
                    "Meter number must be between 1 and 4 (Pro upgrade supports up to 4 meters, standard version supports 1)");
        }

        #endregion
    }
}
