using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace REWApi.Tests
{
    /// <summary>
    /// Base class for service tests providing consistent test baseline using official REW sample data.
    /// Implements test isolation by loading Sampledata.mdat before each test.
    /// 
    /// This solves the test pollution problem where tests share REW state causing cascading failures.
    /// Reference: docs/test-baseline-reset-strategy.md
    /// </summary>
    public abstract class BaseServiceTests : IAsyncLifetime
    {
        // Test timing constants based on empirical testing
        protected const int CommandDelay = 100;         // After REW commands
        protected const int MeasurementLoad = 3000;     // After loading measurements (8.3 MB file)
        protected const int StateChange = 2000;         // After major state changes
        protected const int SimulatorCalc = 3000;       // After room simulator updates

        // Sample data configuration
        private const string SampleDataPath = "D:/Repos/REWApi/tests/resources/Sampledata.mdat";
        private const int ExpectedMinimumMeasurements = 7;  // REW de-duplicates some measurements

        // REW startup configuration
        private static readonly string DefaultREWPath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                         "Programs", "REW", "roomeqwizard.exe");
        private const int REWStartupTimeout = 120000;  // 120 seconds (2 minutes) for REW to start
        private const int REWInitialWait = 10000;  // Wait 10 seconds after launching before first health check
        private const int REWHealthCheckInterval = 2000;  // Check every 2 seconds
        private const int REWPostHealthyWait = 5000;  // Wait 5 seconds after API becomes healthy
        
    private static Process? _rewProcess;
    private static bool _rewStartedByTests = false;
    private static readonly object _rewLock = new object();
    private static readonly SemaphoreSlim _baselineLoadSemaphore = new SemaphoreSlim(1, 1);        protected RewClient? _client;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:4735";

        protected BaseServiceTests()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Initialize client connection, starting REW if needed
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            await EnsureREWIsRunningAsync();
            _client = await RewClient.ConnectAsync();
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            _client?.Dispose();
            _httpClient?.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Ensure REW is running with API enabled. Starts REW if not already running.
        /// </summary>
        private async Task EnsureREWIsRunningAsync()
        {
            // Check if REW API is already responsive
            if (await IsREWApiHealthyAsync())
            {
                return;
            }

            // Try to start REW
            bool needsStartup = false;
            lock (_rewLock)
            {
                // Double-check in case another thread started it
                if (_rewProcess != null && !_rewProcess.HasExited)
                {
                    needsStartup = false;
                }
                else
                {
                    needsStartup = true;
                }
            }

            if (!needsStartup)
            {
                // Another thread already started REW, just wait for it
                await WaitForREWHealthyAsync();
                return;
            }

            // Start REW process
            lock (_rewLock)
            {
                // Final check before starting
                if (_rewProcess != null && !_rewProcess.HasExited)
                {
                    return;
                }

                // Check if REW executable exists
                if (!System.IO.File.Exists(DefaultREWPath))
                {
                    throw new InvalidOperationException(
                        $"REW executable not found at {DefaultREWPath}. " +
                        "Please install REW or update the path in BaseServiceTests.");
                }

                // Start REW with API enabled
                var startInfo = new ProcessStartInfo
                {
                    FileName = DefaultREWPath,
                    Arguments = "-api",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized
                };

                _rewProcess = Process.Start(startInfo);
                _rewStartedByTests = true;

                if (_rewProcess == null)
                {
                    throw new InvalidOperationException("Failed to start REW process.");
                }
            }

            // Give REW time to launch before checking health (outside lock)
            Console.WriteLine($"REW started, waiting {REWInitialWait}ms for initialization...");
            await Task.Delay(REWInitialWait);

            // Wait for REW API to become healthy
            await WaitForREWHealthyAsync();
        }

        /// <summary>
        /// Wait for REW API to become healthy with progress reporting
        /// </summary>
        private async Task WaitForREWHealthyAsync()
        {
            var startTime = DateTime.UtcNow;
            var checksPerformed = 0;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(REWStartupTimeout))
            {
                checksPerformed++;
                if (await IsREWApiHealthyAsync())
                {
                    Console.WriteLine($"REW API healthy after {(DateTime.UtcNow - startTime).TotalSeconds:F1}s ({checksPerformed} health checks)");
                    // Give REW more time to fully initialize after API becomes available
                    Console.WriteLine($"Waiting additional {REWPostHealthyWait}ms for full initialization...");
                    await Task.Delay(REWPostHealthyWait);
                    return;
                }

                if (checksPerformed % 10 == 0)
                {
                    Console.WriteLine($"Still waiting for REW API... ({(DateTime.UtcNow - startTime).TotalSeconds:F1}s elapsed)");
                }

                await Task.Delay(REWHealthCheckInterval);
            }

            throw new TimeoutException(
                $"REW API did not become healthy within {REWStartupTimeout}ms. " +
                "Check if REW started correctly and API is enabled.");
        }

        /// <summary>
        /// Check if REW API is responsive
        /// </summary>
        private async Task<bool> IsREWApiHealthyAsync()
        {
            try
            {
                using var healthClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                // Use /measurements endpoint as health check - it always exists
                var response = await healthClient.GetAsync($"{_baseUrl}/measurements");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Static cleanup method to shut down REW if started by tests.
        /// Call this in test assembly cleanup or at end of test run.
        /// </summary>
        public static void ShutdownREWIfStartedByTests()
        {
            lock (_rewLock)
            {
                if (_rewStartedByTests && _rewProcess != null && !_rewProcess.HasExited)
                {
                    try
                    {
                        _rewProcess.Kill();
                        _rewProcess.WaitForExit(5000);
                    }
                    catch
                    {
                        // Best effort cleanup
                    }
                    finally
                    {
                        _rewProcess?.Dispose();
                        _rewProcess = null;
                        _rewStartedByTests = false;
                    }
                }
            }
        }

        /// <summary>
        /// Load baseline sample data before each test.
        /// Call this in test SetUp/constructor if you want baseline for every test.
        /// This method is thread-safe and ensures only one test loads baseline at a time.
        /// </summary>
        protected async Task LoadBaselineAsync()
        {
            await _baselineLoadSemaphore.WaitAsync();
            try
            {
                // Clear all existing measurements
                await _httpClient.DeleteAsync($"{_baseUrl}/measurements");
                // Give REW time to process the delete before loading new data
                await Task.Delay(StateChange);

                // Load official sample data
                var loadCommand = new
                {
                    command = "Load",
                    parameters = new[] { SampleDataPath }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_baseUrl}/measurements/command",
                    loadCommand
                );

                response.EnsureSuccessStatusCode();
                
                // Wait for large file to load (8.3 MB)
                await Task.Delay(MeasurementLoad);

                // Verify baseline loaded correctly
                var measurements = await GetMeasurementsAsync();
                if (measurements.Count < ExpectedMinimumMeasurements)
                {
                    throw new InvalidOperationException(
                        $"Expected at least {ExpectedMinimumMeasurements} measurements from sample data, got {measurements.Count}"
                    );
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to load baseline sample data from {SampleDataPath}. " +
                    $"Ensure REW is running with API enabled and Sampledata.mdat exists. " +
                    $"Error: {ex.Message}",
                    ex
                );
            }
            finally
            {
                _baselineLoadSemaphore.Release();
            }
        }

        /// <summary>
        /// Get measurements from REW API, converting from object format to list.
        /// REW returns measurements as object with numeric keys (1, 2, 3...), not array.
        /// </summary>
        protected async Task<List<MeasurementSummary>> GetMeasurementsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measurements");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            
            // REW returns measurements as object with numeric keys, not array
            // Example: { "1": {...}, "2": {...}, "3": {...} }
            var measurementsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            if (measurementsDict == null || measurementsDict.Count == 0)
            {
                return new List<MeasurementSummary>();
            }

            // Convert dictionary values to list of MeasurementSummary
            var measurements = new List<MeasurementSummary>();
            foreach (var kvp in measurementsDict)
            {
                var measurement = JsonSerializer.Deserialize<MeasurementSummary>(
                    kvp.Value.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                
                if (measurement != null)
                {
                    measurements.Add(measurement);
                }
            }

            return measurements;
        }

        /// <summary>
        /// Clear all measurements from REW
        /// </summary>
        protected async Task ClearAllMeasurementsAsync()
        {
            await _httpClient.DeleteAsync($"{_baseUrl}/measurements");
            await Task.Delay(CommandDelay);
        }

        /// <summary>
        /// Find measurement by title in current REW measurements
        /// </summary>
        protected async Task<MeasurementSummary?> FindMeasurementByTitleAsync(string title)
        {
            var measurements = await GetMeasurementsAsync();
            return measurements.FirstOrDefault(m => 
                m.Title?.Equals(title, StringComparison.OrdinalIgnoreCase) ?? false
            );
        }

        /// <summary>
        /// Verify baseline sample data is loaded correctly
        /// </summary>
        protected async Task VerifyBaselineLoadedAsync()
        {
            var measurements = await GetMeasurementsAsync();

            Assert.True(measurements.Count >= ExpectedMinimumMeasurements, 
                $"Expected at least {ExpectedMinimumMeasurements} measurements, got {measurements.Count}");
            
            // Verify known measurements exist
            Assert.Contains(measurements, m => m.Title == "Artist 3+Q2070Si");
            Assert.Contains(measurements, m => m.Title == "REL Sub, No EQ");
            Assert.Contains(measurements, m => m.Title == "B&W M1");
            
            // Verify no null titles
            Assert.All(measurements, m => Assert.NotNull(m.Title));
        }
    }

    /// <summary>
    /// Measurement summary model for API responses
    /// Matches REW API measurement structure
    /// </summary>
    public class MeasurementSummary
    {
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public string? Date { get; set; }
        public string? Uuid { get; set; }
        public double StartFreq { get; set; }
        public double EndFreq { get; set; }
        public bool Inverted { get; set; }
        public string? RewVersion { get; set; }
        public double SplOffsetdB { get; set; }
        public double AlignSPLOffsetdB { get; set; }
        public double CumulativeIRShiftSeconds { get; set; }
        public double ClockAdjustmentPPM { get; set; }
        public double TimeOfIRStartSeconds { get; set; }
        public double TimeOfIRPeakSeconds { get; set; }
        public string? GroupName { get; set; }
        public string? GroupNotes { get; set; }
        public string? GroupUuid { get; set; }
    }
}
