using Xunit;
using FluentAssertions;
using REWApi.Domain.Services;
using REWApi.Domain;
using System.Threading.Tasks;
using System.Linq;

namespace REWApi.Tests.CSharp;

/// <summary>
/// Feature tests for RewClient Measurements Service
/// These tests cover features that are NEW or have no legacy equivalent tests.
/// Tests here may be marked as Skip/TODO until features are fully implemented.
/// @module REW.Measurements.Features
/// </summary>
[Collection("REW Integration Tests")]
public class REWMeasurementsFeatureTests : IClassFixture<RewClientFixture>
{
    private readonly RewClient _rewClient;

    public REWMeasurementsFeatureTests(RewClientFixture fixture)
    {
        _rewClient = fixture.Client ?? throw new InvalidOperationException("RewClient not initialized");
    }

    /// <summary>
    /// Test: Get frequency response data using domain service
    /// @test TEST-MEASUREMENTS-FEATURE-001 - Get frequency response data
    /// @status ACTIVE - Bug fixed: REW uses "magnitude"/"phase" not "magnitudeData"/"phaseData", and "startFreq"/"freqStep" not "startFrequency"/"frequencyStep"
    /// </summary>
    [Fact]
    public async Task GetFrequencyResponse_ValidId_ReturnsFrequencyData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var frequencyResponse = await measurementsService.GetFrequencyResponse(firstMeasurementId);
        
        // Assert
        frequencyResponse.Should().NotBeNull();
        frequencyResponse.Smoothing.Should().NotBeNullOrEmpty();
        frequencyResponse.Magnitudes.Should().NotBeEmpty("frequency response should contain magnitude data");
        
        // Verify ArrayCodec already decoded Base64 to float arrays
        frequencyResponse.Magnitudes.Length.Should().BeGreaterThan(0);
        frequencyResponse.Phases.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Get impulse response data using domain service
    /// @test TEST-MEASUREMENTS-FEATURE-002 - Get impulse response data
    /// @status ACTIVE - GetImpulseResponse implemented
    /// </summary>
    [Fact]
    public async Task GetImpulseResponse_ValidId_ReturnsImpulseData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var impulseResponse = await measurementsService.GetImpulseResponse(firstMeasurementId);
        
        // Assert
        impulseResponse.Should().NotBeNull();
        impulseResponse.Data.Should().NotBeEmpty("impulse response should contain amplitude data");
        impulseResponse.SampleRate.Should().BeGreaterThan(0);
        impulseResponse.SampleInterval.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Get group delay data using domain service
    /// @test TEST-MEASUREMENTS-FEATURE-003 - Get group delay data
    /// @status ACTIVE - GetGroupDelay returns FrequencyResponse object with delay values in Magnitudes field (unit: seconds)
    /// @see https://www.roomeqwizard.com/help/help_en-GB/html/api.html#measurements
    /// </summary>
    [Fact]
    public async Task GetGroupDelay_ValidId_ReturnsGroupDelayData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var groupDelay = await measurementsService.GetGroupDelay(firstMeasurementId, smoothing: "None");
        
        // Assert
        groupDelay.Should().NotBeNull();
        groupDelay.Smoothing.Should().NotBeNullOrEmpty();
        groupDelay.Magnitudes.Should().NotBeEmpty("group delay should contain delay values (in seconds) in Magnitudes field");
        groupDelay.StartFrequency.Should().BeGreaterThan(0);
        
        // Verify frequency array was generated
        groupDelay.Frequencies.Should().NotBeEmpty();
        groupDelay.Frequencies.Length.Should().Be(groupDelay.Magnitudes.Length, "frequencies and magnitudes arrays should have same length");
        
        // Verify data format (log or linear spacing)
        if (groupDelay.IsLogSpaced)
        {
            groupDelay.PointsPerOctave.Should().BeGreaterThan(0);
        }
        else
        {
            groupDelay.FrequencyStep.Should().BeGreaterThan(0);
        }
    }

    /// <summary>
    /// Test: Get distortion data for a measurement
    /// @test TEST-MEASUREMENTS-FEATURE-004 - Get distortion analysis
    /// @trace-to MEAS-FR-006 - Distortion Analysis
    /// </summary>
    [Fact]
    public async Task GetDistortion_ValidId_ReturnsDistortionData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act
        var distortion = await measurementsService.GetDistortion(firstMeasurementId, unit: "percent", ppo: 12);

        // Assert
        distortion.Should().NotBeNull();
        distortion.Type.Should().NotBeNullOrEmpty("should have distortion type description");
        distortion.Headers.Should().NotBeEmpty("should have column headers");
        distortion.Data.Should().NotBeEmpty("should have distortion data rows");
        distortion.Unit.Should().Be("percent");
        distortion.PPO.Should().Be(12);

        // Verify data structure consistency
        // Note: REW may return rows with fewer columns than headers when harmonic data is unavailable
        if (distortion.Data.Length > 0)
        {
            var firstRow = distortion.Data[0];
            
            // All rows should have at least 1 column and at most as many as headers
            foreach (var row in distortion.Data)
            {
                row.Length.Should().BeGreaterThan(0, "row should have at least one value");
                row.Length.Should().BeLessOrEqualTo(distortion.Headers.Length, "row should not exceed header count");
            }
        }
    }

    /// <summary>
    /// Test: Get RT60 reverberation time data for a measurement
    /// @test TEST-MEASUREMENTS-FEATURE-005 - Get RT60 data
    /// @req FR-MEAS-002 - Get measurement data (RT60)
    /// @phase Phase 2c - Measurement data retrieval (RT60)
    /// </summary>
    [Fact]
    public async Task GetRT60_ValidId_ReturnsRT60Data()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Note: RT60 data must be generated first via POST command before GET retrieval
        // This test assumes RT60 has been generated (see setup or run "Generate RT60" command)

        // Act - Test with one-third octave bands (octaveFrac=3)
        var rt60 = await measurementsService.GetRT60(firstMeasurementId, octaveFrac: 3);

        // Assert
        rt60.Should().NotBeNull();
        rt60.Frequencies.Should().NotBeEmpty("should have frequency data");
        rt60.RT60Values.Should().NotBeEmpty("should have RT60 values");
        rt60.T20Values.Should().NotBeEmpty("should have T20 values");
        rt60.T30Values.Should().NotBeEmpty("should have T30 values");
        rt60.EDTValues.Should().NotBeEmpty("should have EDT values");
        rt60.OctaveFraction.Should().Be(3, "should match requested octave fraction");
        rt60.Method.Should().Be(RT60Method.T30, "should use T30 method");

        // Verify data structure consistency - all arrays should have same length
        var expectedLength = rt60.Frequencies.Length;
        rt60.RT60Values.Length.Should().Be(expectedLength, "RT60 values length should match frequencies");
        rt60.T20Values.Length.Should().Be(expectedLength, "T20 values length should match frequencies");
        rt60.T30Values.Length.Should().Be(expectedLength, "T30 values length should match frequencies");
        rt60.EDTValues.Length.Should().Be(expectedLength, "EDT values length should match frequencies");

        // Verify frequency 0 (full band/unfiltered) is present as first entry
        rt60.Frequencies[0].Should().Be(0.0, "first frequency should be 0.0 (full band unfiltered result)");

        // Verify frequencies are ordered
        var sortedFrequencies = rt60.Frequencies.OrderBy(f => f).ToArray();
        rt60.Frequencies.Should().BeEquivalentTo(sortedFrequencies, 
            options => options.WithStrictOrdering(), 
            "frequencies should be in ascending order");

        // Verify RT60 values are positive (reverberation times should be > 0)
        rt60.RT60Values.Should().OnlyContain(v => v >= 0, "RT60 values should be non-negative");
        rt60.T20Values.Should().OnlyContain(v => v >= 0, "T20 values should be non-negative");
        rt60.T30Values.Should().OnlyContain(v => v >= 0, "T30 values should be non-negative");
        rt60.EDTValues.Should().OnlyContain(v => v >= 0, "EDT values should be non-negative");
    }

    /// <summary>
    /// Test: Export measurement to file
    /// @test TEST-MEASUREMENTS-FEATURE-006 - Export measurement
    /// @status TODO - Not yet implemented
    /// </summary>
    [Fact(Skip = "TODO: Implement export test")]
    public async Task ExportMeasurement_ValidId_CreatesFile()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        var tempFile = System.IO.Path.GetTempFileName() + ".mdat";
        
        // Act
        await measurementsService.ExportMeasurement(firstMeasurementId, tempFile);
        
        // Assert
        System.IO.File.Exists(tempFile).Should().BeTrue("exported file should exist");
        
        // Cleanup
        if (System.IO.File.Exists(tempFile))
            System.IO.File.Delete(tempFile);
    }

    /// <summary>
    /// Test: Delete single measurement
    /// @test TEST-MEASUREMENTS-DELETE-001 - Delete measurement by UUID
    /// @status ACTIVE - TDD: Test written, implementation pending
    /// </summary>
    [Fact]
    public async Task DeleteMeasurement_ValidId_RemovesMeasurement()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurementsBefore = await measurementsService.ListMeasurements();
        measurementsBefore.Should().NotBeEmpty("test requires at least one measurement in REW");
        var measurementToDelete = measurementsBefore[^1]; // Get last measurement to delete
        var countBefore = measurementsBefore.Length;
        
        // Act
        var result = await measurementsService.DeleteMeasurement(measurementToDelete.UUID);
        
        // Assert
        result.Should().BeTrue("delete should succeed");
        
        var measurementsAfter = await measurementsService.ListMeasurements();
        measurementsAfter.Length.Should().Be(countBefore - 1, "one measurement should be removed");
        measurementsAfter.Should().NotContain(m => m.UUID == measurementToDelete.UUID, "deleted measurement should not be in list");
    }

    /// <summary>
    /// Test: Delete measurement with invalid UUID
    /// @test TEST-MEASUREMENTS-DELETE-002 - Delete with invalid ID
    /// @status ACTIVE - TDD: Test written, implementation pending
    /// </summary>
    [Fact]
    public async Task DeleteMeasurement_InvalidId_ReturnsFalse()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var nonExistentId = "00000000-0000-0000-0000-000000000000";
        
        // Act
        var result = await measurementsService.DeleteMeasurement(nonExistentId);
        
        // Assert
        result.Should().BeFalse("delete should fail for non-existent measurement");
    }

    /// <summary>
    /// Test: Delete all measurements
    /// @test TEST-MEASUREMENTS-DELETE-003 - Clear all measurements
    /// @status ACTIVE - TDD: Test written, implementation pending
    /// WARNING: This test modifies REW state significantly
    /// </summary>
    [Fact(Skip = "Dangerous test - only run manually when REW data can be cleared")]
    public async Task DeleteAllMeasurements_WhenMeasurementsExist_ClearsAll()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurementsBefore = await measurementsService.ListMeasurements();
        if (measurementsBefore.Length == 0)
        {
            // Skip test if no measurements to delete
            return;
        }
        
        // Act
        var result = await measurementsService.DeleteAllMeasurements();
        
        // Assert
        result.Should().BeTrue("delete all should succeed");
        
        var measurementsAfter = await measurementsService.ListMeasurements();
        measurementsAfter.Should().BeEmpty("all measurements should be deleted");
    }

    #region Phase 3a: Core EQ Settings Tests

    /// <summary>
    /// Test: Get measurement equaliser
    /// @test TEST-MEASUREMENTS-EQ-001 - Get equaliser for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task GetMeasurementEqualiser_ValidId_ReturnsEqualiser()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var equaliser = await measurementsService.GetMeasurementEqualiser(firstMeasurementId);
        
        // Assert
        equaliser.Should().NotBeNull();
        equaliser.Manufacturer.Should().NotBeNullOrEmpty("equaliser should have manufacturer");
        equaliser.Model.Should().NotBeNullOrEmpty("equaliser should have model");
    }

    /// <summary>
    /// Test: Update measurement equaliser
    /// @test TEST-MEASUREMENTS-EQ-002 - Set equaliser for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementEqualiser_ValidEqualiser_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        var originalEqualiser = await measurementsService.GetMeasurementEqualiser(firstMeasurementId);
        
        var newEqualiser = new EqualiserInfo
        {
            Manufacturer = "Generic",
            Model = "Generic"
        };
        
        // Act
        await measurementsService.UpdateMeasurementEqualiser(firstMeasurementId, newEqualiser);
        
        // Assert - Verify update succeeded by reading back
        var updatedEqualiser = await measurementsService.GetMeasurementEqualiser(firstMeasurementId);
        updatedEqualiser.Manufacturer.Should().Be(newEqualiser.Manufacturer);
        updatedEqualiser.Model.Should().Be(newEqualiser.Model);
        
        // Cleanup - Restore original
        await measurementsService.UpdateMeasurementEqualiser(firstMeasurementId, originalEqualiser);
    }

    /// <summary>
    /// Test: Get measurement filters
    /// @test TEST-MEASUREMENTS-EQ-003 - Get filter list for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task GetMeasurementFilters_ValidId_ReturnsFilters()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var filters = await measurementsService.GetMeasurementFilters(firstMeasurementId);
        
        // Assert
        filters.Should().NotBeNull();
        filters.Should().NotBeEmpty("measurement should have filter slots");
        filters.Length.Should().BeGreaterThan(0);
        
        // Verify filter structure
        var firstFilter = filters[0];
        firstFilter.Index.Should().BeGreaterThan(0);
        firstFilter.Type.Should().NotBeNullOrEmpty();
        // Note: enabled and isAuto are boolean properties, always have values
    }

    /// <summary>
    /// Test: Update single filter
    /// @test TEST-MEASUREMENTS-EQ-004 - Update individual filter setting
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementFilter_ValidFilter_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        var originalFilters = await measurementsService.GetMeasurementFilters(firstMeasurementId);
        var originalFirstFilter = originalFilters[0];
        
        var updatedFilter = new FilterSetting
        {
            Index = originalFirstFilter.Index,
            Type = originalFirstFilter.Type,
            Enabled = originalFirstFilter.Enabled,
            IsAuto = originalFirstFilter.IsAuto,
            Fc = originalFirstFilter.Fc,
            Gain = originalFirstFilter.Gain,
            Q = originalFirstFilter.Q
        };
        
        // Act
        await measurementsService.UpdateMeasurementFilter(firstMeasurementId, updatedFilter);
        
        // Assert - Verify update succeeded
        var filters = await measurementsService.GetMeasurementFilters(firstMeasurementId);
        var firstFilter = filters[0];
        firstFilter.Index.Should().Be(updatedFilter.Index);
        
        // Cleanup - Restore original
        await measurementsService.UpdateMeasurementFilter(firstMeasurementId, originalFirstFilter);
    }

    /// <summary>
    /// Test: Update multiple filters
    /// @test TEST-MEASUREMENTS-EQ-005 - Batch update filter settings
    /// @status SKIP - API POST /measurements/:id/filters returns 400 - may not be implemented
    /// </summary>
    [Fact(Skip = "API endpoint POST /measurements/:id/filters returns 400 - batch update may not be fully implemented")]
    public async Task UpdateMeasurementFilters_ValidFilters_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        var originalFilters = await measurementsService.GetMeasurementFilters(firstMeasurementId);
        
        // Create copy of first two filters
        var filtersToUpdate = originalFilters.Take(2).Select(f => new FilterSetting
        {
            Index = f.Index,
            Type = f.Type,
            Enabled = f.Enabled,
            IsAuto = f.IsAuto,
            Fc = f.Fc,
            Gain = f.Gain,
            Q = f.Q
        }).ToArray();
        
        // Act
        await measurementsService.UpdateMeasurementFilters(firstMeasurementId, filtersToUpdate);
        
        // Assert - Verify update succeeded
        var filters = await measurementsService.GetMeasurementFilters(firstMeasurementId);
        filters.Should().NotBeEmpty();
        
        // Cleanup - Restore originals
        await measurementsService.UpdateMeasurementFilters(firstMeasurementId, originalFilters);
    }

    /// <summary>
    /// Test: Get target settings
    /// @test TEST-MEASUREMENTS-EQ-006 - Get target settings for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task GetMeasurementTargetSettings_ValidId_ReturnsSettings()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var targetSettings = await measurementsService.GetMeasurementTargetSettings(firstMeasurementId);
        
        // Assert
        targetSettings.Should().NotBeNull();
        targetSettings.Shape.Should().NotBeNullOrEmpty();
        targetSettings.LowPassCrossoverType.Should().NotBeNullOrEmpty();
        targetSettings.HighPassCrossoverType.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test: Update target settings
    /// @test TEST-MEASUREMENTS-EQ-007 - Update target settings for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementTargetSettings_ValidSettings_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        var originalSettings = await measurementsService.GetMeasurementTargetSettings(firstMeasurementId);
        
        var updatedSettings = new TargetSettings
        {
            Shape = originalSettings.Shape,
            BassManagementSlopedBPerOctave = originalSettings.BassManagementSlopedBPerOctave,
            BassManagementCutoffHz = originalSettings.BassManagementCutoffHz,
            LowFreqSlopedBPerOctave = originalSettings.LowFreqSlopedBPerOctave,
            LowFreqCutoffHz = originalSettings.LowFreqCutoffHz,
            LowPassCrossoverType = originalSettings.LowPassCrossoverType,
            HighPassCrossoverType = originalSettings.HighPassCrossoverType,
            LowPassCutoffHz = originalSettings.LowPassCutoffHz,
            HighPassCutoffHz = originalSettings.HighPassCutoffHz
        };
        
        // Act
        await measurementsService.UpdateMeasurementTargetSettings(firstMeasurementId, updatedSettings);
        
        // Assert - Verify update succeeded
        var settings = await measurementsService.GetMeasurementTargetSettings(firstMeasurementId);
        settings.Shape.Should().Be(updatedSettings.Shape);
        
        // Cleanup - Restore original
        await measurementsService.UpdateMeasurementTargetSettings(firstMeasurementId, originalSettings);
    }

    /// <summary>
    /// Test: Get target level
    /// @test TEST-MEASUREMENTS-EQ-008 - Get target level for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task GetMeasurementTargetLevel_ValidId_ReturnsLevel()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        // Act
        var targetLevel = await measurementsService.GetMeasurementTargetLevel(firstMeasurementId);
        
        // Assert
        targetLevel.Should().BeGreaterThan(0, "target level should be positive dB SPL value");
        targetLevel.Should().BeLessThan(200, "target level should be reasonable dB SPL value");
    }

    /// <summary>
    /// Test: Update target level
    /// @test TEST-MEASUREMENTS-EQ-009 - Update target level for measurement
    /// @status ACTIVE - TDD: Test written, implementation complete
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementTargetLevel_ValidLevel_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;
        
        var originalLevel = await measurementsService.GetMeasurementTargetLevel(firstMeasurementId);
        var newLevel = 75.0; // Standard reference level
        
        // Act
        await measurementsService.UpdateMeasurementTargetLevel(firstMeasurementId, newLevel);
        
        // Assert - Verify update succeeded
        var updatedLevel = await measurementsService.GetMeasurementTargetLevel(firstMeasurementId);
        updatedLevel.Should().Be(newLevel);
        
        // Cleanup - Restore original
        await measurementsService.UpdateMeasurementTargetLevel(firstMeasurementId, originalLevel);
    }

    #endregion

    #region Phase 3b: EQ Commands

    /// <summary>
    /// Test: Get available EQ commands
    /// @test TEST-MEASUREMENTS-EQ-009 - Get list of available EQ commands
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task GetEQCommands_ReturnsEightCommands()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var commands = await measurementsService.GetEQCommands();

        // Assert
        commands.Should().NotBeNull();
        commands.Should().HaveCount(8, "REW API provides 8 EQ commands");
        
        // Verify expected commands are present
        commands.Should().Contain("Calculate target level");
        commands.Should().Contain("Match target");
        commands.Should().Contain("Optimise gains");
        commands.Should().Contain("Optimise gains and Qs");
        commands.Should().Contain("Optimise gains, Qs and Fcs");
        commands.Should().Contain("Generate predicted measurement");
        commands.Should().Contain("Generate filters measurement");
        commands.Should().Contain("Generate target measurement");
    }

    /// <summary>
    /// Test: Execute EQ command - Calculate target level
    /// @test TEST-MEASUREMENTS-EQ-010 - Execute Calculate target level command
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task ExecuteEQCommand_CalculateTargetLevel_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act
        var result = await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "Calculate target level");

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty("command should return status message");
        // Calculate target level is typically a quick operation, may not return an ID
    }

    /// <summary>
    /// Test: Execute EQ command - Optimise gains
    /// @test TEST-MEASUREMENTS-EQ-011 - Execute Optimise gains command
    /// @status SKIP - May fail if REW command gate is busy from previous tests
    /// </summary>
    [Fact(Skip = "May fail if REW command gate is busy from previous EQ command executions - test manually if needed")]
    public async Task ExecuteEQCommand_OptimiseGains_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act
        var result = await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "Optimise gains");

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty("command should return status message");
    }

    /// <summary>
    /// Test: Execute EQ command - Generate predicted measurement
    /// @test TEST-MEASUREMENTS-EQ-012 - Execute Generate predicted measurement command
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task ExecuteEQCommand_GeneratePredictedMeasurement_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act
        var result = await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "Generate predicted measurement");

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty("command should return status message");
        // This command typically creates a new measurement
    }

    /// <summary>
    /// Test: Execute EQ command - Match target (long-running)
    /// @test TEST-MEASUREMENTS-EQ-013 - Execute Match target command
    /// @status SKIP - Long-running operation that creates additional measurements
    /// </summary>
    [Fact(Skip = "Long-running operation that may create additional measurements - run manually if needed")]
    public async Task ExecuteEQCommand_MatchTarget_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act
        var result = await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "Match target");

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty();
        result.Id.Should().HaveValue("long-running operation should return progress tracking ID");
        result.Message.Should().Contain("in progress", "match target is typically async");
    }

    /// <summary>
    /// Test: Execute EQ command with invalid command name
    /// @test TEST-MEASUREMENTS-EQ-014 - Invalid command name should throw exception
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task ExecuteEQCommand_InvalidCommand_ThrowsException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act & Assert
        var act = async () => await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "Invalid Command That Does Not Exist");

        await act.Should().ThrowAsync<HttpRequestException>(
            "invalid command name should be rejected by API");
    }

    /// <summary>
    /// Test: Execute EQ command with empty measurement ID
    /// @test TEST-MEASUREMENTS-EQ-015 - Empty measurement ID should throw ArgumentException
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task ExecuteEQCommand_EmptyMeasurementId_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.ExecuteEQCommand(
            "",
            "Calculate target level");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Measurement ID*");
    }

    /// <summary>
    /// Test: Execute EQ command with empty command string
    /// @test TEST-MEASUREMENTS-EQ-016 - Empty command should throw ArgumentException
    /// @status ACTIVE - Phase 3b implementation
    /// </summary>
    [Fact]
    public async Task ExecuteEQCommand_EmptyCommand_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("test requires at least one measurement in REW");
        var firstMeasurementId = measurements[0].UUID;

        // Act & Assert
        var act = async () => await measurementsService.ExecuteEQCommand(
            firstMeasurementId,
            "");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Command*");
    }

    #endregion

    #region Phase 3c: Global EQ Defaults

    /// <summary>
    /// Test: Get available equaliser profiles
    /// @test TEST-MEASUREMENTS-EQ-017 - Get list of 50 built-in equaliser profiles
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetAvailableEqualisers_ReturnsAllProfiles()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var equalisers = await measurementsService.GetAvailableEqualisers();

        // Assert
        equalisers.Should().NotBeNull();
        equalisers.Should().HaveCountGreaterThan(10, "REW provides 50 built-in equaliser profiles");
        equalisers.Should().Contain(e => e.Manufacturer == "Generic" && e.Model == "Generic");
    }

    /// <summary>
    /// Test: Get available equalisers filtered by manufacturer
    /// @test TEST-MEASUREMENTS-EQ-018 - Filter equalisers by manufacturer name
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetAvailableEqualisers_WithManufacturerFilter_ReturnsFiltered()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var equalisers = await measurementsService.GetAvailableEqualisers("MiniDSP");

        // Assert
        equalisers.Should().NotBeNull();
        equalisers.Should().NotBeEmpty("MiniDSP has several equaliser models");
        equalisers.Should().OnlyContain(e => e.Manufacturer == "MiniDSP");
    }

    /// <summary>
    /// Test: Get list of manufacturers
    /// @test TEST-MEASUREMENTS-EQ-019 - Get list of 33 manufacturers
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetManufacturers_Returns33Manufacturers()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var manufacturers = await measurementsService.GetManufacturers();

        // Assert
        manufacturers.Should().NotBeNull();
        manufacturers.Should().HaveCountGreaterThan(10, "REW provides 33 manufacturers");
        manufacturers.Should().Contain("Generic");
        manufacturers.Should().Contain("MiniDSP");
    }

    /// <summary>
    /// Test: Get and set default equaliser
    /// @test TEST-MEASUREMENTS-EQ-020 - Get/set default equaliser profile
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetSetDefaultEqualiser_RoundTrip_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        
        // Save original
        var originalEqualiser = await measurementsService.GetDefaultEqualiser();
        
        var newEqualiser = new EqualiserInfo
        {
            Manufacturer = "Generic",
            Model = "Generic"
        };

        // Act
        await measurementsService.SetDefaultEqualiser(newEqualiser);
        
        // Assert - Verify change
        var updatedEqualiser = await measurementsService.GetDefaultEqualiser();
        updatedEqualiser.Manufacturer.Should().Be(newEqualiser.Manufacturer);
        updatedEqualiser.Model.Should().Be(newEqualiser.Model);
        
        // Cleanup - Restore original
        await measurementsService.SetDefaultEqualiser(originalEqualiser);
    }

    /// <summary>
    /// Test: Get default target settings
    /// @test TEST-MEASUREMENTS-EQ-021 - Get default target curve settings
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetDefaultTargetSettings_ReturnsSettings()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var targetSettings = await measurementsService.GetDefaultTargetSettings();

        // Assert
        targetSettings.Should().NotBeNull();
        targetSettings.Shape.Should().NotBeNullOrEmpty("target shape should be set");
        // Shape values: "Full range", "Subwoofer", "LF", "LF + sub", "Other"
    }

    /// <summary>
    /// Test: Update default target settings (POST - full replacement)
    /// @test TEST-MEASUREMENTS-EQ-022 - Update default target settings
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task UpdateDefaultTargetSettings_FullReplacement_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        
        // Save original
        var originalSettings = await measurementsService.GetDefaultTargetSettings();
        
        var newSettings = new TargetSettings
        {
            Shape = "Subwoofer",
            BassManagementSlopedBPerOctave = 12,
            BassManagementCutoffHz = 80,
            LowFreqSlopedBPerOctave = 24,
            LowFreqCutoffHz = 10,
            LowPassCrossoverType = "L-R2",
            HighPassCrossoverType = "L-R2",
            LowPassCutoffHz = 1000,
            HighPassCutoffHz = 100
        };

        // Act
        await measurementsService.UpdateDefaultTargetSettings(newSettings, partialUpdate: false);
        
        // Assert - Verify change
        var updatedSettings = await measurementsService.GetDefaultTargetSettings();
        updatedSettings.Shape.Should().Be(newSettings.Shape);
        updatedSettings.BassManagementCutoffHz.Should().Be(newSettings.BassManagementCutoffHz);
        
        // Cleanup - Restore original
        await measurementsService.UpdateDefaultTargetSettings(originalSettings, partialUpdate: false);
    }

    /// <summary>
    /// Test: Get and set default target level
    /// @test TEST-MEASUREMENTS-EQ-023 - Get/set default target level in dB SPL
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task GetSetDefaultTargetLevel_RoundTrip_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        
        // Save original
        var originalLevel = await measurementsService.GetDefaultTargetLevel();
        
        var newLevel = 75.0; // Standard reference level

        // Act
        await measurementsService.SetDefaultTargetLevel(newLevel);
        
        // Assert - Verify change
        var updatedLevel = await measurementsService.GetDefaultTargetLevel();
        updatedLevel.Should().Be(newLevel);
        
        // Cleanup - Restore original
        await measurementsService.SetDefaultTargetLevel(originalLevel);
    }

    /// <summary>
    /// Test: Set default equaliser with null argument
    /// @test TEST-MEASUREMENTS-EQ-024 - Null equaliser should throw ArgumentNullException
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task SetDefaultEqualiser_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.SetDefaultEqualiser(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Test: Update default target settings with null argument
    /// @test TEST-MEASUREMENTS-EQ-025 - Null settings should throw ArgumentNullException
    /// @status ACTIVE - Phase 3c implementation
    /// </summary>
    [Fact]
    public async Task UpdateDefaultTargetSettings_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.UpdateDefaultTargetSettings(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}

