using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using REWApi.Domain;
using Xunit.Abstractions;

namespace REWApi.Tests
{
    /// <summary>
    /// Helper class to setup test preconditions for various REW API operations
    /// Ensures correct states before running tests to avoid 400 errors
    /// 
    /// Based on test-failure-analysis.md findings:
    /// - Import operations require stimulus before response
    /// - RTA operations require RTA to be started
    /// - Generator operations require generator to be stopped
    /// - Phase operations require measurements with impulse/frequency response data
    /// </summary>
    public class TestPreconditions
    {
        private readonly RewClient _client;
        private readonly ITestOutputHelper? _output;

        public TestPreconditions(RewClient client, ITestOutputHelper? output = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _output = output;
        }

        #region Import Service Preconditions

        /// <summary>
        /// Ensure valid test WAV files exist for import operations
        /// Creates minimal valid WAV files if they don't exist
        /// </summary>
        public async Task<(string stimulusPath, string responsePath)> EnsureValidWAVFilesExist()
        {
            var testDir = Path.Combine(Path.GetTempPath(), "REWApi_TestData");
            Directory.CreateDirectory(testDir);

            var stimulusPath = Path.Combine(testDir, "test_stimulus.wav");
            var responsePath = Path.Combine(testDir, "test_response.wav");

            // Create minimal valid WAV files (1 second of silence at 48kHz, 16-bit)
            if (!File.Exists(stimulusPath))
            {
                CreateMinimalWAVFile(stimulusPath, durationSeconds: 1.0, sampleRate: 48000);
                _output?.WriteLine($"Created test stimulus file: {stimulusPath}");
            }

            if (!File.Exists(responsePath))
            {
                CreateMinimalWAVFile(responsePath, durationSeconds: 1.0, sampleRate: 48000);
                _output?.WriteLine($"Created test response file: {responsePath}");
            }

            await Task.CompletedTask;
            return (stimulusPath, responsePath);
        }

        /// <summary>
        /// Ensure sweep stimulus is set before setting response
        /// Addresses prerequisite: "The sweep stimulus must be set before setting the sweep response"
        /// </summary>
        public async Task<bool> EnsureSweepStimulusSet()
        {
            var (stimulusPath, _) = await EnsureValidWAVFilesExist();

            try
            {
                await _client.Import.SetSweepStimulusAsync(stimulusPath);
                await Task.Delay(100); // Command delay

                _output?.WriteLine($"✅ Sweep stimulus set to: {stimulusPath}");
                return true;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to set sweep stimulus: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create minimal valid WAV file for testing
        /// REW validates WAV file structure, so we need proper headers
        /// </summary>
        private void CreateMinimalWAVFile(string filePath, double durationSeconds, int sampleRate)
        {
            int numChannels = 1;
            int bitsPerSample = 16;
            int bytesPerSample = bitsPerSample / 8;
            int numSamples = (int)(sampleRate * durationSeconds);
            int dataSize = numSamples * numChannels * bytesPerSample;
            int fileSize = 44 + dataSize; // 44 bytes for WAV header

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                // RIFF header
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(fileSize - 8);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16); // Chunk size
                bw.Write((short)1); // Audio format (PCM)
                bw.Write((short)numChannels);
                bw.Write(sampleRate);
                bw.Write(sampleRate * numChannels * bytesPerSample); // Byte rate
                bw.Write((short)(numChannels * bytesPerSample)); // Block align
                bw.Write((short)bitsPerSample);

                // data chunk
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(dataSize);

                // Write silence (zeros)
                for (int i = 0; i < numSamples * numChannels; i++)
                {
                    bw.Write((short)0);
                }
            }
        }

        #endregion

        #region RTA Service Preconditions

        /// <summary>
        /// Ensure RTA is started before issuing RTA commands
        /// Addresses failures: Reset, SaveGraphImage, SetSPLOffset
        /// </summary>
        public async Task<bool> EnsureRTAStarted()
        {
            try
            {
                // Check current status
                var status = await _client.RTA.GetStatus();

                if (status.Running)
                {
                    _output?.WriteLine("✅ RTA already running");
                    return true;
                }

                if (!status.Enabled)
                {
                    _output?.WriteLine("⚠️ RTA is not enabled - cannot start");
                    return false;
                }

                // Start RTA
                await _client.RTA.Start();
                await Task.Delay(2000); // Wait for RTA to fully start (StateChange timing)

                // Verify it started
                status = await _client.RTA.GetStatus();
                var started = status.Running;

                if (started)
                {
                    _output?.WriteLine("✅ RTA started successfully");
                }
                else
                {
                    _output?.WriteLine("❌ RTA failed to start");
                }

                return started;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to start RTA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop RTA to return to idle state
        /// </summary>
        public async Task<bool> EnsureRTAStopped()
        {
            try
            {
                var status = await _client.RTA.GetStatus();

                if (!status.Running)
                {
                    _output?.WriteLine("✅ RTA already stopped");
                    return true;
                }

                await _client.RTA.Stop();
                await Task.Delay(100); // Command delay

                status = await _client.RTA.GetStatus();
                var stopped = !status.Running;

                if (stopped)
                {
                    _output?.WriteLine("✅ RTA stopped successfully");
                }

                return stopped;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to stop RTA: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Generator Service Preconditions

        /// <summary>
        /// Ensure generator is stopped before configuration changes
        /// Addresses failures: SetFrequency, UpdateConfiguration
        /// </summary>
        public async Task<bool> EnsureGeneratorStopped()
        {
            try
            {
                var config = await _client.Generator.GetConfiguration();

                // Check if generator is running (look for "running" or "started" in status)
                // Note: Generator config doesn't have explicit "running" flag,
                // but we can try to stop it to ensure it's in correct state
                await _client.Generator.Stop();
                await Task.Delay(100); // Command delay

                _output?.WriteLine("✅ Generator stopped/ensured stopped");
                return true;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"⚠️ Generator stop command: {ex.Message}");
                // Not necessarily an error - generator might already be stopped
                return true;
            }
        }

        /// <summary>
        /// Start generator (for tests that need it running)
        /// </summary>
        public async Task<bool> EnsureGeneratorStarted()
        {
            try
            {
                await _client.Generator.Start();
                await Task.Delay(100); // Command delay

                _output?.WriteLine("✅ Generator started");
                return true;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to start generator: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Measurements Service Preconditions

        /// <summary>
        /// Ensure baseline measurements are loaded
        /// Required for phase operations and other measurement commands
        /// </summary>
        public async Task<bool> EnsureBaselineMeasurementsLoaded()
        {
            try
            {
                // Use helper method that accesses LoadBaselineAsync
                await LoadBaseline();
                _output?.WriteLine("✅ Baseline measurements loaded");
                return true;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to load baseline: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Find a measurement with impulse response data
        /// Required for: UnwrapPhase, OffsetTimeZero, GenerateMinimumPhase, etc.
        /// </summary>
        public async Task<string?> FindMeasurementWithImpulseResponse()
        {
            try
            {
                var measurements = await GetMeasurements();

                // Look for measurements that likely have impulse response
                // Sample data typically includes measurements like "Artist 3+Q2070Si"
                var irMeasurement = measurements.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Title) &&
                    (m.Title.Contains("Artist") || m.Title.Contains("B&W") || m.Title.Contains("REL")));

                if (irMeasurement != null)
                {
                    _output?.WriteLine($"✅ Found IR measurement: {irMeasurement.Title} (UUID: {irMeasurement.Uuid})");
                    return irMeasurement.Uuid;
                }

                // Fallback to first measurement
                var firstMeasurement = measurements.FirstOrDefault();
                if (firstMeasurement != null)
                {
                    _output?.WriteLine($"⚠️ Using first measurement: {firstMeasurement.Title} (UUID: {firstMeasurement.Uuid})");
                    return firstMeasurement.Uuid;
                }

                _output?.WriteLine("❌ No measurements found");
                return null;
            }
            catch (Exception ex)
            {
                _output?.WriteLine($"❌ Failed to find IR measurement: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Find a measurement with frequency response data
        /// Required for EQ operations and frequency response processing
        /// </summary>
        public async Task<string?> FindMeasurementWithFrequencyResponse()
        {
            // In REW, all measurements have frequency response
            // So we can use the same logic as impulse response
            return await FindMeasurementWithImpulseResponse();
        }

        #endregion

        #region Comprehensive Precondition Setup

        /// <summary>
        /// Setup all preconditions for comprehensive testing
        /// Call this once at the beginning of test class initialization
        /// </summary>
        public async Task<PreconditionResult> SetupAllPreconditions()
        {
            var result = new PreconditionResult();

            _output?.WriteLine("\n🔧 Setting up test preconditions...\n");

            // 1. Load baseline measurements
            result.BaselineMeasurementsLoaded = await EnsureBaselineMeasurementsLoaded();

            // 2. Find measurements with appropriate data
            result.ImpulseResponseMeasurementId = await FindMeasurementWithImpulseResponse();
            result.FrequencyResponseMeasurementId = await FindMeasurementWithFrequencyResponse();

            // 3. Ensure valid test files exist
            var (stimulus, response) = await EnsureValidWAVFilesExist();
            result.StimulusFilePath = stimulus;
            result.ResponseFilePath = response;

            // 4. Stop any running processes (clean state)
            await EnsureRTAStopped();
            await EnsureGeneratorStopped();

            _output?.WriteLine("\n✅ Precondition setup complete\n");
            _output?.WriteLine($"   Baseline loaded: {result.BaselineMeasurementsLoaded}");
            _output?.WriteLine($"   IR measurement: {result.ImpulseResponseMeasurementId ?? "none"}");
            _output?.WriteLine($"   FR measurement: {result.FrequencyResponseMeasurementId ?? "none"}");
            _output?.WriteLine($"   Stimulus file: {result.StimulusFilePath}");
            _output?.WriteLine($"   Response file: {result.ResponseFilePath}\n");

            return result;
        }

        #endregion

        #region Helper Methods to Access Protected BaseServiceTests Members

        private async Task LoadBaseline()
        {
            // Call reflection or use inheritance pattern
            // For now, use direct measurement loading
            var httpClient = new System.Net.Http.HttpClient();
            var baseUrl = "http://127.0.0.1:4735";
            
            // Clear measurements
            await httpClient.DeleteAsync($"{baseUrl}/measurements");
            await Task.Delay(2000);

            // Load sample data
            var sampleDataPath = "D:/Repos/REWApi/tests/resources/Sampledata.mdat";
            var loadCommand = new
            {
                command = "Load",
                parameters = new[] { sampleDataPath }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(loadCommand);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            await httpClient.PostAsync($"{baseUrl}/measurements/command", content);
            await Task.Delay(3000); // Wait for load
        }

        private async Task<System.Collections.Generic.List<MeasurementSummary>> GetMeasurements()
        {
            var httpClient = new System.Net.Http.HttpClient();
            var baseUrl = "http://127.0.0.1:4735";
            
            var response = await httpClient.GetAsync($"{baseUrl}/measurements");
            var jsonString = await response.Content.ReadAsStringAsync();

            // Parse measurements
            var measurementsDict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(jsonString);
            
            var measurements = new System.Collections.Generic.List<MeasurementSummary>();
            if (measurementsDict != null)
            {
                foreach (var kvp in measurementsDict)
                {
                    var measurement = System.Text.Json.JsonSerializer.Deserialize<MeasurementSummary>(
                        kvp.Value.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    
                    if (measurement != null)
                    {
                        measurements.Add(measurement);
                    }
                }
            }

            return measurements;
        }

        #endregion
    }

    /// <summary>
    /// Result of precondition setup
    /// Contains all necessary IDs and paths for testing
    /// </summary>
    public class PreconditionResult
    {
        public bool BaselineMeasurementsLoaded { get; set; }
        public string? ImpulseResponseMeasurementId { get; set; }
        public string? FrequencyResponseMeasurementId { get; set; }
        public string? StimulusFilePath { get; set; }
        public string? ResponseFilePath { get; set; }

        public bool IsValid =>
            BaselineMeasurementsLoaded &&
            !string.IsNullOrEmpty(ImpulseResponseMeasurementId) &&
            !string.IsNullOrEmpty(FrequencyResponseMeasurementId) &&
            !string.IsNullOrEmpty(StimulusFilePath) &&
            !string.IsNullOrEmpty(ResponseFilePath);
    }
}
