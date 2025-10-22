using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using REWApi.Domain;
using REWApi.Domain.Services;
using REWApi.Infrastructure;

[assembly: InternalsVisibleTo("REWApi.Tests.CSharp")]

namespace REWApi
{
    /// <summary>
    /// Options for launching REW with API support
    /// </summary>
    public class REWLaunchOptions
    {
        public bool NoGUI { get; set; } = true;
        public bool EnableAPI { get; set; } = true;
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 4735;
        public string? REWExecutablePath { get; set; }
        public string[] AdditionalArgs { get; set; } = Array.Empty<string>();
        public TimeSpan LaunchTimeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// Main REW API client facade
    /// Provides unified access to all REW services with proper lifecycle management
    /// </summary>
    public class RewClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly CommandGate _commandGate;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly string _baseUrl;
        private readonly ILogger<RewClient>? _logger;
        private readonly bool _ownHttpClient;
        private readonly Process? _rewProcess;
        private volatile bool _disposed;

        // Domain service properties
        public ApplicationService Application { get; }
        public AudioService Audio { get; }
        public MeasurementsService Measurements { get; }
        public SPLMeterService SPLMeter { get; }
        public GeneratorService Generator { get; }

        // Internal properties for testing
        internal HttpClient HttpClient => _httpClient;
        internal string BaseUrl => _baseUrl;

        private RewClient(
            HttpClient httpClient,
            string baseUrl,
            ILogger<RewClient>? logger = null,
            bool ownHttpClient = true,
            Process? rewProcess = null)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl;
            _logger = logger;
            _ownHttpClient = ownHttpClient;
            _rewProcess = rewProcess;

            // Create command gate with clear function
            _commandGate = new CommandGate(
                async () =>
                {
                    var response = await _httpClient.PostAsync(
                        $"{_baseUrl}/application/clear-command-in-progress",
                        new StringContent("", System.Text.Encoding.UTF8, "application/json"));
                    response.EnsureSuccessStatusCode();
                },
                null);

            // Create subscription manager
            var callbackBaseUrl = "http://localhost:8080"; // Would be configurable in production
            _subscriptionManager = new SubscriptionManager(_httpClient, _baseUrl, callbackBaseUrl, null);

            // Initialize domain services
            Application = new ApplicationService(_commandGate, _httpClient, _baseUrl, _subscriptionManager);
            Audio = new AudioService(_commandGate, _httpClient, _baseUrl);
            Measurements = new MeasurementsService(_commandGate, _httpClient, _baseUrl);
            SPLMeter = new SPLMeterService(_httpClient, _baseUrl);
            Generator = new GeneratorService(_httpClient, _baseUrl);
        }

        /// <summary>
        /// Connect to running REW instance
        /// Tests connection and retrieves version info
        /// </summary>
        /// <param name="baseUrl">REW API base URL (default: http://localhost:4735)</param>
        /// <param name="httpClient">Optional HttpClient instance</param>
        /// <param name="logger">Optional logger</param>
        /// <param name="timeout">Connection timeout</param>
        /// <returns>Connected RewClient instance</returns>
        /// <exception cref="REWConnectionException">Thrown if connection fails</exception>
        public static async Task<RewClient> ConnectAsync(
            string baseUrl = "http://localhost:4735",
            HttpClient? httpClient = null,
            ILogger<RewClient>? logger = null,
            TimeSpan timeout = default)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            // Normalize base URL
            baseUrl = baseUrl.TrimEnd('/');
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
                baseUrl = "http://" + baseUrl;

            bool ownHttpClient = httpClient == null;
            httpClient ??= CreateDefaultHttpClient();

            try
            {
                logger?.LogInformation("Connecting to REW API at {BaseUrl}", baseUrl);

                // Test connection with timeout using /version endpoint (which exists)
                using var cts = new CancellationTokenSource(timeout);
                var testResponse = await httpClient.GetAsync($"{baseUrl}/version", cts.Token);
                
                if (!testResponse.IsSuccessStatusCode)
                {
                    throw new REWConnectionException(baseUrl, 
                        new HttpRequestException($"REW API returned {testResponse.StatusCode}: {testResponse.ReasonPhrase}"));
                }

                // Create client instance
                var client = new RewClient(httpClient, baseUrl, logger, ownHttpClient);

                // Get version info to verify API compatibility
                var version = await client.Application.GetVersion(cts.Token);
                logger?.LogInformation("Connected to REW {Version} (API: {APIVersion})", 
                    version.Version, version.APIVersion ?? "Unknown");

                return client;
            }
            catch (Exception ex) when (!(ex is REWConnectionException))
            {
                // Clean up HttpClient if we created it
                if (ownHttpClient)
                    httpClient?.Dispose();

                throw new REWConnectionException(baseUrl, ex);
            }
        }

        /// <summary>
        /// Ensure REW is running and connect
        /// Launches REW with API flags if needed
        /// </summary>
        /// <param name="options">Launch options</param>
        /// <param name="logger">Optional logger</param>
        /// <returns>Connected RewClient instance</returns>
        public static async Task<RewClient> EnsureRunningAndConnect(
            REWLaunchOptions? options = null,
            ILogger<RewClient>? logger = null)
        {
            options ??= new REWLaunchOptions();
            var baseUrl = $"http://{options.Host}:{options.Port}";

            try
            {
                // Try to connect to existing instance first
                logger?.LogDebug("Attempting to connect to existing REW instance at {BaseUrl}", baseUrl);
                return await ConnectAsync(baseUrl, logger: logger, timeout: TimeSpan.FromSeconds(5));
            }
            catch (REWConnectionException)
            {
                logger?.LogInformation("No existing REW instance found, launching new instance");
            }

            // Launch REW with API support
            var rewProcess = await LaunchREW(options, logger);
            
            try
            {
                // Wait for REW to start and become available
                var httpClient = CreateDefaultHttpClient();
                var startTime = DateTime.UtcNow;
                
                while (DateTime.UtcNow - startTime < options.LaunchTimeout)
                {
                    try
                    {
                        var client = await ConnectAsync(baseUrl, httpClient, logger, TimeSpan.FromSeconds(2));
                        
                        // Success - return client with process reference for cleanup
                        return new RewClient(httpClient, baseUrl, logger, true, rewProcess);
                    }
                    catch (REWConnectionException)
                    {
                        // REW not ready yet, wait and retry
                        await Task.Delay(1000);
                        
                        // Check if process is still running
                        if (rewProcess.HasExited)
                        {
                            throw new REWConnectionException(baseUrl, 
                                new InvalidOperationException($"REW process exited with code {rewProcess.ExitCode}"));
                        }
                    }
                }

                throw new REWConnectionException(baseUrl, 
                    new TimeoutException($"REW did not become available within {options.LaunchTimeout}"));
            }
            catch
            {
                // Clean up process if connection failed
                try
                {
                    if (!rewProcess.HasExited)
                        rewProcess.Kill();
                    rewProcess.Dispose();
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to clean up REW process after connection failure");
                }
                throw;
            }
        }

        /// <summary>
        /// Test connection to REW API
        /// </summary>
        public async Task<bool> TestConnection(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return false;

            return await Application.TestConnection(cancellationToken);
        }

        /// <summary>
        /// Get REW version and API capabilities
        /// </summary>
        public async Task<REWVersion> GetVersion(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return await Application.GetVersion(cancellationToken);
        }

        /// <summary>
        /// Clean shutdown - stops subscriptions and closes REW if launched by us
        /// </summary>
        public async Task Shutdown(bool shutdownREW = false, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            try
            {
                _logger?.LogInformation("Shutting down REW client");

                // Unsubscribe from all notifications
                await _subscriptionManager.UnsubscribeAll();

                // Shutdown REW if requested or if we launched it
                if (shutdownREW || _rewProcess != null)
                {
                    try
                    {
                        await Application.Shutdown(cancellationToken);
                        _logger?.LogInformation("REW application shutdown requested");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to shutdown REW application gracefully");
                    }
                }

                // Wait for REW process to exit if we launched it
                if (_rewProcess != null && !_rewProcess.HasExited)
                {
                    var shutdownTimeout = TimeSpan.FromSeconds(10);
                    if (!_rewProcess.WaitForExit((int)shutdownTimeout.TotalMilliseconds))
                    {
                        _logger?.LogWarning("REW process did not exit within {Timeout}, forcing termination", shutdownTimeout);
                        _rewProcess.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during REW client shutdown");
            }
        }

        /// <summary>
        /// Test connection to REW API
        /// Attempts to connect and retrieve version info
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection successful, false otherwise</returns>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get application status as a connection test
                var status = await Application.GetStatus(cancellationToken);
                return status != null && !string.IsNullOrEmpty(status.Version);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to connect to REW API at {BaseUrl}", _baseUrl);
                return false;
            }
        }

        /// <summary>
        /// Get current command gate status
        /// Useful for diagnostics and monitoring
        /// </summary>
        public Infrastructure.CommandStatus GetCommandStatus()
        {
            ThrowIfDisposed();
            return _commandGate.GetCurrentStatus();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // Best effort shutdown
                Shutdown(shutdownREW: _rewProcess != null).Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during disposal shutdown");
            }

            try
            {
                _commandGate?.Dispose();
                _subscriptionManager?.Dispose();
                
                if (_ownHttpClient)
                    _httpClient?.Dispose();
                
                _rewProcess?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing REW client resources");
            }
        }

        // Private helper methods

        private static HttpClient CreateDefaultHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for measurements
            httpClient.DefaultRequestHeaders.Add("User-Agent", "REWApi.NET/1.0");
            return httpClient;
        }

        private static Task<Process> LaunchREW(REWLaunchOptions options, ILogger? logger)
        {
            var rewPath = options.REWExecutablePath ?? FindREWExecutable();
            if (string.IsNullOrEmpty(rewPath) || !File.Exists(rewPath))
            {
                throw new FileNotFoundException(
                    "REW executable not found. Please specify the path in REWLaunchOptions.REWExecutablePath");
            }

            var args = new List<string>();
            
            if (options.NoGUI)
                args.Add("--nogui");
            
            if (options.EnableAPI)
            {
                args.Add("--api");
                args.Add($"--apihost={options.Host}");
                args.Add($"--apiport={options.Port}");
            }
            
            args.AddRange(options.AdditionalArgs);

            var startInfo = new ProcessStartInfo
            {
                FileName = rewPath,
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                CreateNoWindow = options.NoGUI,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            logger?.LogInformation("Launching REW: {FileName} {Arguments}", rewPath, startInfo.Arguments);

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start REW process");
            }

            // Set up output logging
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger?.LogDebug("REW stdout: {Output}", e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger?.LogDebug("REW stderr: {Output}", e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return Task.FromResult(process);
        }

        private static string? FindREWExecutable()
        {
            // Common REW installation paths
            var possiblePaths = new[]
            {
                @"C:\Program Files\Room EQ Wizard\rew.exe",
                @"C:\Program Files (x86)\Room EQ Wizard\rew.exe",
                @"C:\REW\rew.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Room EQ Wizard", "rew.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Room EQ Wizard", "rew.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Try PATH environment variable
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (var directory in pathEnv.Split(Path.PathSeparator))
                {
                    var rewPath = Path.Combine(directory, "rew.exe");
                    if (File.Exists(rewPath))
                        return rewPath;
                }
            }

            return null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RewClient));
        }
    }
}