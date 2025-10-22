using System;
using System.Collections.Generic;
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

        protected RewClient? _client;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:4735";

        protected BaseServiceTests()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Initialize client connection
        /// </summary>
        public virtual async Task InitializeAsync()
        {
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
        /// Load baseline sample data before each test.
        /// Call this in test SetUp/constructor if you want baseline for every test.
        /// </summary>
        protected async Task LoadBaselineAsync()
        {
            try
            {
                // Clear all existing measurements
                await _httpClient.DeleteAsync($"{_baseUrl}/measurements");
                await Task.Delay(CommandDelay);

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
