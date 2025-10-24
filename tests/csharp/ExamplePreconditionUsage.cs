using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using REWApi.Domain;

namespace REWApi.Tests
{
    /// <summary>
    /// Example tests demonstrating how to use TestPreconditions helper
    /// Shows correct patterns for setting up test states before running operations
    /// 
    /// This addresses the 10 environmental failures found in test-failure-analysis.md:
    /// - Import Service (7 tests) - Need valid files + correct order
    /// - RTA Service (3 tests) - Need RTA started
    /// - Generator Service (2 tests) - Need generator stopped
    /// - Measurements Service (5 tests) - Need measurements with IR/FR data
    /// </summary>
    [Collection("REW Integration Tests")]
    public class ExamplePreconditionUsageTests : BaseServiceTests
    {
        private readonly ITestOutputHelper _output;
        private TestPreconditions? _preconditions;
        private PreconditionResult? _setupResult;

        public ExamplePreconditionUsageTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Initialize preconditions helper
            _preconditions = new TestPreconditions(_client!, _output);

            // Setup all preconditions once at class initialization
            _setupResult = await _preconditions.SetupAllPreconditions();

            // Verify preconditions are valid
            _setupResult.IsValid.Should().BeTrue("preconditions must be valid for tests to run");
        }

        #region Import Service Examples

        [Fact]
        public async Task Example_ImportService_SetSweepResponse_WithPreconditions()
        {
            // ✅ CORRECT: Ensure stimulus is set BEFORE response
            var stimulusSet = await _preconditions!.EnsureSweepStimulusSet();
            stimulusSet.Should().BeTrue("stimulus must be set before response");

            await Task.Delay(CommandDelay); // Wait for operation to complete

            // Now safe to set response
            var result = await _client!.Import.SetSweepResponseAsync(
                _setupResult!.ResponseFilePath!,
                channels: "All" // Named parameter for channels
            );

            // Assert
            result.Should().BeTrue("response should be set successfully after stimulus");
        }

        [Fact]
        public async Task Example_ImportService_SetFrequencyResponseFile_WithValidFile()
        {
            // ✅ CORRECT: Use valid test file created by preconditions
            var testFile = _setupResult!.StimulusFilePath!;

            // Act
            var result = await _client!.Import.SetFrequencyResponseFileAsync(testFile);

            // Assert
            result.Should().BeTrue("valid file should be accepted");

            await Task.Delay(CommandDelay);

            // Verify (may not persist if REW validates content)
            var retrievedPath = await _client.Import.GetFrequencyResponseFileAsync();
            _output.WriteLine($"Retrieved path: {retrievedPath}");
        }

        #endregion

        #region RTA Service Examples

        [Fact]
        public async Task Example_RTAService_Reset_WithPreconditions()
        {
            // ✅ CORRECT: Ensure RTA is started before issuing commands
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running for Reset command");

            // Now safe to issue Reset command
            await _client!.RTA.Reset();

            // Verify command executed successfully (no exception thrown)
            _output.WriteLine("✅ RTA Reset command executed successfully");
        }

        [Fact]
        public async Task Example_RTAService_SaveGraphImage_WithPreconditions()
        {
            // ✅ CORRECT: Start RTA before saving graph
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running to save graph");

            await Task.Delay(StateChange); // Wait for RTA to capture data

            // Now safe to save graph (saves to default/configured location)
            await _client!.RTA.SaveGraphImage();

            _output.WriteLine("✅ RTA graph saved successfully (check REW configured location)");
        }

        [Fact]
        public async Task Example_RTAService_SetSPLOffset_WithPreconditions()
        {
            // ✅ CORRECT: Start RTA before setting SPL offset
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running for SPL offset");

            // Now safe to set SPL offset
            var offset = 3.0; // dB
            await _client!.RTA.SetSPLOffset(offset);

            _output.WriteLine($"✅ RTA SPL offset set to {offset} dB");
        }

        #endregion

        #region Generator Service Examples

        [Fact]
        public async Task Example_GeneratorService_SetFrequency_WithPreconditions()
        {
            // ✅ CORRECT: Stop generator before configuration changes
            var generatorStopped = await _preconditions!.EnsureGeneratorStopped();
            generatorStopped.Should().BeTrue("generator should be stopped for configuration");

            // Now safe to set frequency
            var frequency = 1000.0;
            var unit = "Hz";

            await _client!.Generator.SetFrequency(frequency, unit);

            _output.WriteLine($"✅ Generator frequency set to {frequency} {unit}");
        }

        [Fact]
        public async Task Example_GeneratorService_UpdateConfiguration_WithPreconditions()
        {
            // ✅ CORRECT: Stop generator before updating configuration
            var generatorStopped = await _preconditions!.EnsureGeneratorStopped();
            generatorStopped.Should().BeTrue("generator should be stopped for configuration");

            // Get current configuration
            var currentConfig = await _client!.Generator.GetConfiguration();

            // Modify configuration
            var newConfig = new GeneratorConfiguration
            {
                AddDither = false,
                Frequency = 1000.0 // For sine wave signals
            };

            // Now safe to update
            await _client.Generator.UpdateConfiguration(newConfig);

            _output.WriteLine("✅ Generator configuration updated successfully");
        }

        #endregion

        #region Measurements Service Examples

        [Fact]
        public async Task Example_MeasurementsService_UnwrapPhase_WithPreconditions()
        {
            // ✅ CORRECT: Use measurement with impulse response data
            var measurementId = _setupResult!.ImpulseResponseMeasurementId!;
            measurementId.Should().NotBeNullOrEmpty("need valid measurement with IR data");

            // Now safe to unwrap phase
            await _client!.Measurements.UnwrapPhase(measurementId);

            _output.WriteLine($"✅ Phase unwrapped for measurement: {measurementId}");
        }

        [Fact]
        public async Task Example_MeasurementsService_OffsetTimeZero_WithPreconditions()
        {
            // ✅ CORRECT: Use measurement from baseline
            var measurementId = _setupResult!.ImpulseResponseMeasurementId!;

            // Now safe to offset time zero (requires offset parameter)
            var offsetMs = 0.0; // Offset in milliseconds
            await _client!.Measurements.OffsetTimeZero(measurementId, offsetMs);

            _output.WriteLine($"✅ Time zero offset for measurement: {measurementId}");
        }

        [Fact]
        public async Task Example_MeasurementsService_GenerateMinimumPhase_WithPreconditions()
        {
            // ✅ CORRECT: Use measurement with impulse response
            var measurementId = _setupResult!.ImpulseResponseMeasurementId!;

            var measurementsBefore = await GetMeasurementsAsync();
            var countBefore = measurementsBefore.Count;

            // Generate minimum phase version
            await _client!.Measurements.GenerateMinimumPhase(measurementId);

            await Task.Delay(StateChange); // Wait for new measurement to be created

            var measurementsAfter = await GetMeasurementsAsync();
            var countAfter = measurementsAfter.Count;

            _output.WriteLine($"✅ Minimum phase generated (measurements: {countBefore} → {countAfter})");
            countAfter.Should().BeGreaterThan(countBefore, "minimum phase creates new measurement");
        }

        [Fact]
        public async Task Example_MeasurementsService_GenerateMicInBoxCorrection_WithPreconditions()
        {
            // ✅ CORRECT: Use measurement with appropriate data
            var measurementId = _setupResult!.ImpulseResponseMeasurementId!;

            // Generate mic-in-box correction
            await _client!.Measurements.GenerateMicInBoxCorrection(measurementId);

            _output.WriteLine($"✅ Mic-in-box correction generated for: {measurementId}");
        }

        [Fact]
        public async Task Example_MeasurementsService_ExcessPhaseVersion_WithPreconditions()
        {
            // ✅ CORRECT: Use measurement with impulse response
            var measurementId = _setupResult!.ImpulseResponseMeasurementId!;

            // Generate excess phase version
            await _client!.Measurements.ExcessPhaseVersion(measurementId);

            await Task.Delay(StateChange);

            _output.WriteLine($"✅ Excess phase version generated for: {measurementId}");
        }

        #endregion

        #region Demonstrating WRONG vs CORRECT Patterns

        [Fact(Skip = "Demonstration of WRONG pattern - will fail with 400 error")]
        public async Task Example_WRONG_SetSweepResponse_WithoutStimulus()
        {
            // ❌ WRONG: Trying to set response without stimulus
            // This will fail with 400 Bad Request

            var result = await _client!.Import.SetSweepResponseAsync("C:\\test\\response.wav");

            // This assertion will never be reached
            result.Should().BeTrue();
        }

        [Fact(Skip = "Demonstration of WRONG pattern - will fail with 400 error")]
        public async Task Example_WRONG_RTA_Reset_WithoutStarting()
        {
            // ❌ WRONG: Trying to reset RTA without starting it first

            await _client!.RTA.Reset();

            // This will fail with 400 Bad Request
        }

        [Fact(Skip = "Demonstration of WRONG pattern - may fail")]
        public async Task Example_WRONG_Generator_SetFrequency_WhileRunning()
        {
            // ❌ WRONG: Changing frequency while generator is running
            // May fail depending on REW state

            // Start generator
            await _client!.Generator.Start();

            // Try to change frequency (may fail)
            await _client.Generator.SetFrequency(2000.0, "Hz");

            // Should stop generator first!
        }

        #endregion
    }
}
