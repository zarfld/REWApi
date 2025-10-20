using System;
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
    /// REW Application control and system-level operations
    /// Maps to /application/* endpoints in REW API
    /// </summary>
    public class ApplicationService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly ILogger<ApplicationService>? _logger;

        internal ApplicationService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            SubscriptionManager subscriptionManager,
            ILogger<ApplicationService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _logger = logger;
        }

        /// <summary>
        /// Shutdown REW application cleanly
        /// Maps to POST /application/shutdown
        /// </summary>
        public async Task Shutdown(CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/application/shutdown",
                    new StringContent("", Encoding.UTF8, "application/json"),
                    cancellationToken);

                response.EnsureSuccessStatusCode();
                
                _logger?.LogInformation("REW application shutdown initiated");
            }, "Shutdown Application", cancellationToken);
        }

        /// <summary>
        /// Enable/disable blocking mode for scripts
        /// Maps to PUT /application/blocking-mode
        /// Persisted across REW sessions
        /// </summary>
        public async Task EnableBlocking(bool enable = true, CancellationToken cancellationToken = default)
        {
            await _commandGate.ExecuteCommand(async () =>
            {
                var requestData = new { blockingMode = enable };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_baseUrl}/application/blocking-mode",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();
                
                _logger?.LogInformation("REW blocking mode {Action}", enable ? "enabled" : "disabled");
            }, $"{(enable ? "Enable" : "Disable")} Blocking Mode", cancellationToken);
        }

        /// <summary>
        /// Get current application status
        /// Maps to GET /version and various /application/* endpoints
        /// </summary>
        public async Task<ApplicationStatus> GetStatus(CancellationToken cancellationToken = default)
        {
            // Get version information
            var versionResponse = await _httpClient.GetAsync($"{_baseUrl}/version", cancellationToken);
            versionResponse.EnsureSuccessStatusCode();
            var versionJson = await versionResponse.Content.ReadAsStringAsync(cancellationToken);
            var versionData = JsonSerializer.Deserialize<JsonElement>(versionJson);
            var version = versionData.GetProperty("message").GetString() ?? string.Empty;

            // Get blocking mode
            var blockingResponse = await _httpClient.GetAsync($"{_baseUrl}/application/blocking", cancellationToken);
            blockingResponse.EnsureSuccessStatusCode();
            var blockingJson = await blockingResponse.Content.ReadAsStringAsync(cancellationToken);
            var blockingMode = JsonSerializer.Deserialize<bool>(blockingJson);

            // Get logging status
            var loggingResponse = await _httpClient.GetAsync($"{_baseUrl}/application/logging", cancellationToken);
            loggingResponse.EnsureSuccessStatusCode();
            var loggingJson = await loggingResponse.Content.ReadAsStringAsync(cancellationToken);
            var loggingEnabled = JsonSerializer.Deserialize<bool>(loggingJson);

            return new ApplicationStatus
            {
                Version = version,
                BlockingMode = blockingMode,
                CommandInProgress = false, // TODO: Need to find correct endpoint for this
                CurrentCommand = null,     // TODO: Need to find correct endpoint for this
                LoggingEnabled = loggingEnabled,
                LogFilePath = null         // TODO: Need to find correct endpoint for this
            };
        }

        /// <summary>
        /// Clear any command currently in progress (emergency stop)
        /// Maps to POST /application/clear-command-in-progress
        /// </summary>
        public async Task ClearCommandInProgress(CancellationToken cancellationToken = default)
        {
            // This is used internally by CommandGate, but also available as public API
            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/application/clear-command-in-progress",
                new StringContent("", Encoding.UTF8, "application/json"),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Cleared REW command in progress");
        }

        /// <summary>
        /// Subscribe to application errors and warnings
        /// Maps to /application/errors and /application/warnings
        /// Returns IDisposable for cleanup
        /// </summary>
        public IDisposable SubscribeToErrors(Action<ApplicationError> onError)
        {
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            return _subscriptionManager.Subscribe<string>(
                "/application/errors",
                jsonData =>
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(jsonData);
                        var error = new ApplicationError
                        {
                            Timestamp = data.TryGetProperty("timestamp", out var ts) && ts.TryGetDateTime(out var timestamp)
                                ? timestamp
                                : DateTime.UtcNow,
                            Message = data.GetProperty("message").GetString() ?? string.Empty,
                            Details = data.TryGetProperty("details", out var details) ? details.GetString() : null,
                            Severity = Enum.TryParse<ErrorSeverity>(
                                data.TryGetProperty("severity", out var sev) ? sev.GetString() : "Error",
                                true,
                                out var severity) ? severity : ErrorSeverity.Error
                        };

                        onError(error);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error parsing application error notification");
                    }
                });
        }

        /// <summary>
        /// Subscribe to application warnings
        /// </summary>
        public IDisposable SubscribeToWarnings(Action<ApplicationWarning> onWarning)
        {
            if (onWarning == null)
                throw new ArgumentNullException(nameof(onWarning));

            return _subscriptionManager.Subscribe<string>(
                "/application/warnings",
                jsonData =>
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(jsonData);
                        var warning = new ApplicationWarning
                        {
                            Timestamp = data.TryGetProperty("timestamp", out var ts) && ts.TryGetDateTime(out var timestamp)
                                ? timestamp
                                : DateTime.UtcNow,
                            Message = data.GetProperty("message").GetString() ?? string.Empty,
                            Details = data.TryGetProperty("details", out var details) ? details.GetString() : null
                        };

                        onWarning(warning);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error parsing application warning notification");
                    }
                });
        }

        /// <summary>
        /// Toggle debug logging and get log file path
        /// Maps to PUT /application/logging
        /// </summary>
        public async Task<string> EnableLogging(bool enable = true, CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var requestData = new { enabled = enable };
                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_baseUrl}/application/logging",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(responseJson);

                var logFilePath = data.TryGetProperty("logFilePath", out var path) 
                    ? path.GetString() ?? string.Empty 
                    : string.Empty;

                _logger?.LogInformation("REW logging {Action}, log file: {LogFile}", 
                    enable ? "enabled" : "disabled", logFilePath);

                return logFilePath;
            }, $"{(enable ? "Enable" : "Disable")} Logging", cancellationToken);
        }

        /// <summary>
        /// Get REW version and API capabilities
        /// Maps to GET /version
        /// </summary>
        public async Task<REWVersion> GetVersion(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/version", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            // Parse the message field: "5.40 Beta 102 API 0.9.0"
            var message = data.GetProperty("message").GetString() ?? string.Empty;
            var parts = message.Split(' ');
            
            string version = "";
            string apiVersion = "";
            
            if (parts.Length >= 1)
            {
                version = parts[0]; // "5.40"
            }
            
            // Look for "API" keyword followed by version
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("API", StringComparison.OrdinalIgnoreCase) && i + 1 < parts.Length)
                {
                    apiVersion = parts[i + 1]; // "0.9.0"
                    break;
                }
            }

            return new REWVersion
            {
                Version = version,
                BuildDate = null, // Not available in this endpoint
                APIVersion = apiVersion,
                OpenAPIHash = null, // Not available in this endpoint
                IsProVersion = message.Contains("Pro", StringComparison.OrdinalIgnoreCase),
                SupportedFeatures = Array.Empty<string>() // Not available in this endpoint
            };
        }

        /// <summary>
        /// Test if REW application is responsive
        /// Simple ping to verify the API is working
        /// </summary>
        public async Task<bool> TestConnection(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/version", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "REW connection test failed");
                return false;
            }
        }
    }

    /// <summary>
    /// REW version and capability information
    /// </summary>
    public class REWVersion
    {
        public string Version { get; set; } = string.Empty;
        public string? BuildDate { get; set; }
        public string? APIVersion { get; set; }
        public string? OpenAPIHash { get; set; }
        public bool IsProVersion { get; set; }
        public string[] SupportedFeatures { get; set; } = Array.Empty<string>();
    }
}