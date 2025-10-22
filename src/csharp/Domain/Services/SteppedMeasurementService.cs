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
    /// REW Stepped Measurement Service
    /// Maps to /stepped-measurement/* endpoints in REW API
    /// Provides control for stepped sine and frequency sweep measurements
    /// </summary>
    public class SteppedMeasurementService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SteppedMeasurementService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Commands

        /// <summary>
        /// Get list of available stepped measurement commands
        /// GET /stepped-measurement/commands
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available commands</returns>
        public async Task<List<string>> GetCommands(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/commands", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Send command to stepped measurement
        /// POST /stepped-measurement/command
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if command was sent successfully</returns>
        public async Task<bool> SendCommand(string command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be null or empty", nameof(command));

            var json = JsonSerializer.Serialize(new { command });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/command", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Measurement Type

        /// <summary>
        /// Get list of available stepped measurement types
        /// GET /stepped-measurement/types
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of measurement types</returns>
        public async Task<List<string>> GetTypes(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/types", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Get current stepped measurement type
        /// GET /stepped-measurement/type
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current measurement type</returns>
        public async Task<string> GetType(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/type", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            // API returns plain text, not JSON
            var content = await response.Content.ReadAsStringAsync();
            return content.Trim('"').Trim();
        }

        /// <summary>
        /// Set stepped measurement type
        /// POST /stepped-measurement/type
        /// </summary>
        /// <param name="type">Measurement type to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if type was set successfully</returns>
        public async Task<bool> SetType(string type, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Type cannot be null or empty", nameof(type));

            var json = JsonSerializer.Serialize(new { type });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/type", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Frequency Span

        /// <summary>
        /// Get frequency span configuration
        /// GET /stepped-measurement/frequency-span
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Frequency span configuration</returns>
        public async Task<SteppedFreqSpan> GetFrequencySpan(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/frequency-span", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SteppedFreqSpan>(content) 
                ?? new SteppedFreqSpan();
        }

        /// <summary>
        /// Set frequency span configuration
        /// POST /stepped-measurement/frequency-span
        /// </summary>
        /// <param name="freqSpan">Frequency span configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was set successfully</returns>
        public async Task<bool> SetFrequencySpan(SteppedFreqSpan freqSpan, CancellationToken cancellationToken = default)
        {
            if (freqSpan == null)
                throw new ArgumentNullException(nameof(freqSpan));

            var json = JsonSerializer.Serialize(freqSpan);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/frequency-span", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update frequency span configuration (partial update)
        /// PUT /stepped-measurement/frequency-span
        /// </summary>
        /// <param name="freqSpan">Frequency span configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was updated successfully</returns>
        public async Task<bool> UpdateFrequencySpan(SteppedFreqSpan freqSpan, CancellationToken cancellationToken = default)
        {
            if (freqSpan == null)
                throw new ArgumentNullException(nameof(freqSpan));

            var json = JsonSerializer.Serialize(freqSpan);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/stepped-measurement/frequency-span", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available PPO (points per octave) values
        /// GET /stepped-measurement/ppo-values
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available PPO values</returns>
        public async Task<List<int>> GetPpoValues(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/ppo-values", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<int>>(content) 
                ?? new List<int>();
        }

        #endregion

        #region Level Span

        /// <summary>
        /// Get level span configuration
        /// GET /stepped-measurement/level-span
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Level span configuration</returns>
        public async Task<SteppedLevelSpan> GetLevelSpan(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/level-span", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SteppedLevelSpan>(content) 
                ?? new SteppedLevelSpan();
        }

        /// <summary>
        /// Set level span configuration
        /// POST /stepped-measurement/level-span
        /// </summary>
        /// <param name="levelSpan">Level span configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was set successfully</returns>
        public async Task<bool> SetLevelSpan(SteppedLevelSpan levelSpan, CancellationToken cancellationToken = default)
        {
            if (levelSpan == null)
                throw new ArgumentNullException(nameof(levelSpan));

            var json = JsonSerializer.Serialize(levelSpan);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/level-span", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update level span configuration (partial update)
        /// PUT /stepped-measurement/level-span
        /// </summary>
        /// <param name="levelSpan">Level span configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was updated successfully</returns>
        public async Task<bool> UpdateLevelSpan(SteppedLevelSpan levelSpan, CancellationToken cancellationToken = default)
        {
            if (levelSpan == null)
                throw new ArgumentNullException(nameof(levelSpan));

            var json = JsonSerializer.Serialize(levelSpan);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/stepped-measurement/level-span", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region FFT Configuration

        /// <summary>
        /// Get FFT configuration
        /// GET /stepped-measurement/fft-configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>FFT configuration</returns>
        public async Task<SteppedFFTConfiguration> GetFftConfiguration(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/fft-configuration", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SteppedFFTConfiguration>(content) 
                ?? new SteppedFFTConfiguration();
        }

        /// <summary>
        /// Set FFT configuration
        /// POST /stepped-measurement/fft-configuration
        /// </summary>
        /// <param name="fftConfig">FFT configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was set successfully</returns>
        public async Task<bool> SetFftConfiguration(SteppedFFTConfiguration fftConfig, CancellationToken cancellationToken = default)
        {
            if (fftConfig == null)
                throw new ArgumentNullException(nameof(fftConfig));

            var json = JsonSerializer.Serialize(fftConfig);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/fft-configuration", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update FFT configuration (partial update)
        /// PUT /stepped-measurement/fft-configuration
        /// </summary>
        /// <param name="fftConfig">FFT configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was updated successfully</returns>
        public async Task<bool> UpdateFftConfiguration(SteppedFFTConfiguration fftConfig, CancellationToken cancellationToken = default)
        {
            if (fftConfig == null)
                throw new ArgumentNullException(nameof(fftConfig));

            var json = JsonSerializer.Serialize(fftConfig);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/stepped-measurement/fft-configuration", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available FFT lengths
        /// GET /stepped-measurement/fft-configuration/fft-lengths
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available FFT lengths</returns>
        public async Task<List<string>> GetFftLengths(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/fft-configuration/fft-lengths", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Get available averaging choices
        /// GET /stepped-measurement/fft-configuration/averaging-choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available averaging options</returns>
        public async Task<List<int>> GetAveragingChoices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/fft-configuration/averaging-choices", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<int>>(content) 
                ?? new List<int>();
        }

        /// <summary>
        /// Get available overlap choices
        /// GET /stepped-measurement/fft-configuration/overlap-choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available overlap percentages</returns>
        public async Task<List<string>> GetOverlapChoices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/fft-configuration/overlap-choices", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        /// <summary>
        /// Get available window function choices
        /// GET /stepped-measurement/fft-configuration/window-choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available window functions</returns>
        public async Task<List<string>> GetWindowChoices(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/fft-configuration/window-choices", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        #endregion

        #region Options

        /// <summary>
        /// Get stepped measurement options
        /// GET /stepped-measurement/options
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stepped measurement options</returns>
        public async Task<SteppedOptions> GetOptions(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/options", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SteppedOptions>(content) 
                ?? new SteppedOptions();
        }

        /// <summary>
        /// Set stepped measurement options
        /// POST /stepped-measurement/options
        /// </summary>
        /// <param name="options">Stepped measurement options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if options were set successfully</returns>
        public async Task<bool> SetOptions(SteppedOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var json = JsonSerializer.Serialize(options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/options", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update stepped measurement options (partial update)
        /// PUT /stepped-measurement/options
        /// </summary>
        /// <param name="options">Stepped measurement options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if options were updated successfully</returns>
        public async Task<bool> UpdateOptions(SteppedOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var json = JsonSerializer.Serialize(options);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{_baseUrl}/stepped-measurement/options", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region IMD Stimuli

        /// <summary>
        /// Get available IMD (Intermodulation Distortion) stimuli
        /// GET /stepped-measurement/imd-stimuli
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available IMD stimuli</returns>
        public async Task<List<string>> GetImdStimuli(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/imd-stimuli", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        #endregion

        #region Progress Monitoring

        /// <summary>
        /// Get current stepped measurement progress
        /// GET /stepped-measurement/progress
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current measurement progress</returns>
        public async Task<SteppedProgress> GetProgress(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/progress", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SteppedProgress>(content) 
                ?? new SteppedProgress();
        }

        /// <summary>
        /// Subscribe to progress updates
        /// POST /stepped-measurement/progress/subscribe
        /// </summary>
        /// <param name="callbackUrl">URL for progress update callbacks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription was successful</returns>
        public async Task<bool> SubscribeToProgress(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var json = JsonSerializer.Serialize(new { url = callbackUrl });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/progress/subscribe", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Unsubscribe from progress updates
        /// POST /stepped-measurement/progress/unsubscribe
        /// </summary>
        /// <param name="callbackUrl">URL to unsubscribe</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if unsubscription was successful</returns>
        public async Task<bool> UnsubscribeFromProgress(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var json = JsonSerializer.Serialize(new { url = callbackUrl });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/progress/unsubscribe", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get list of progress subscribers
        /// GET /stepped-measurement/progress/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of subscribed callback URLs</returns>
        public async Task<List<string>> GetProgressSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/progress/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        #endregion

        #region Results Subscription

        /// <summary>
        /// Subscribe to results updates
        /// POST /stepped-measurement/results/subscribe
        /// </summary>
        /// <param name="callbackUrl">URL for results callbacks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription was successful</returns>
        public async Task<bool> SubscribeToResults(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var json = JsonSerializer.Serialize(new { url = callbackUrl });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/results/subscribe", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Unsubscribe from results updates
        /// POST /stepped-measurement/results/unsubscribe
        /// </summary>
        /// <param name="callbackUrl">URL to unsubscribe</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if unsubscription was successful</returns>
        public async Task<bool> UnsubscribeFromResults(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var json = JsonSerializer.Serialize(new { url = callbackUrl });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/stepped-measurement/results/unsubscribe", httpContent, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get list of results subscribers
        /// GET /stepped-measurement/results/subscribers
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of subscribed callback URLs</returns>
        public async Task<List<string>> GetResultsSubscribers(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stepped-measurement/results/subscribers", cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) 
                ?? new List<string>();
        }

        #endregion
    }
}
