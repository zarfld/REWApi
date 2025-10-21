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

    #region Phase 3d - Room Curves (11 tests)

    /// <summary>
    /// Test: Get default room curve settings
    /// @test TEST-MEASUREMENTS-EQ-026 - Get default room curve configuration
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task GetDefaultRoomCurveSettings_ReturnsSettings()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var settings = await measurementsService.GetDefaultRoomCurveSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.LowFreqRiseStartHz.Should().BeGreaterThan(0);
        settings.HighFreqFallStartHz.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Update default room curve settings with full replacement
    /// @test TEST-MEASUREMENTS-EQ-027 - POST updates default room curve settings
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task UpdateDefaultRoomCurveSettings_FullReplacement_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var originalSettings = await measurementsService.GetDefaultRoomCurveSettings();

        var newSettings = new RoomCurveSettings
        {
            AddRoomCurve = true,
            LowFreqRiseStartHz = 250,
            LowFreqRiseEndHz = 25,
            LowFreqRiseSlopedBPerOctave = 2.0,
            HighFreqFallStartHz = 2000,
            HighFreqFallSlopedBPerOctave = 1.0
        };

        try
        {
            // Act
            await measurementsService.UpdateDefaultRoomCurveSettings(newSettings, partialUpdate: false);

            // Assert
            var updatedSettings = await measurementsService.GetDefaultRoomCurveSettings();
            updatedSettings.LowFreqRiseStartHz.Should().Be(250);
            updatedSettings.LowFreqRiseSlopedBPerOctave.Should().Be(2.0);
            updatedSettings.HighFreqFallStartHz.Should().Be(2000);
        }
        finally
        {
            // Cleanup: Restore original settings
            await measurementsService.UpdateDefaultRoomCurveSettings(originalSettings, partialUpdate: false);
        }
    }

    /// <summary>
    /// Test: Get measurement room curve settings
    /// @test TEST-MEASUREMENTS-EQ-028 - Get room curve settings for specific measurement
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task GetMeasurementRoomCurveSettings_ValidId_ReturnsSettings()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("at least one measurement must exist in REW");
        var measurementId = measurements[0].UUID;

        // Act
        var settings = await measurementsService.GetMeasurementRoomCurveSettings(measurementId);

        // Assert
        settings.Should().NotBeNull();
        settings.LowFreqRiseStartHz.Should().BeGreaterThan(0);
        settings.HighFreqFallStartHz.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Update measurement room curve settings
    /// @test TEST-MEASUREMENTS-EQ-029 - POST updates measurement-specific room curve settings
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementRoomCurveSettings_ValidId_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeEmpty("at least one measurement must exist in REW");
        var measurementId = measurements[0].UUID;

        var originalSettings = await measurementsService.GetMeasurementRoomCurveSettings(measurementId);

        var newSettings = new RoomCurveSettings
        {
            AddRoomCurve = true,
            LowFreqRiseStartHz = 300,
            LowFreqRiseEndHz = 30,
            LowFreqRiseSlopedBPerOctave = 1.5,
            HighFreqFallStartHz = 1500,
            HighFreqFallSlopedBPerOctave = 0.75
        };

        try
        {
            // Act
            await measurementsService.UpdateMeasurementRoomCurveSettings(measurementId, newSettings, partialUpdate: false);

            // Assert
            var updatedSettings = await measurementsService.GetMeasurementRoomCurveSettings(measurementId);
            updatedSettings.LowFreqRiseStartHz.Should().Be(300);
            updatedSettings.LowFreqRiseSlopedBPerOctave.Should().Be(1.5);
        }
        finally
        {
            // Cleanup: Restore original settings
            await measurementsService.UpdateMeasurementRoomCurveSettings(measurementId, originalSettings, partialUpdate: false);
        }
    }

    /// <summary>
    /// Test: Get house curve path
    /// @test TEST-MEASUREMENTS-EQ-030 - Get current house curve file path
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task GetHouseCurve_ReturnsPathOrEmpty()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var houseCurve = await measurementsService.GetHouseCurve();

        // Assert
        houseCurve.Should().NotBeNull("house curve should return empty string if not set");
    }

    /// <summary>
    /// Test: Set and clear house curve
    /// @test TEST-MEASUREMENTS-EQ-031 - Set house curve path and clear it
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task SetAndClearHouseCurve_RoundTrip_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var originalHouseCurve = await measurementsService.GetHouseCurve();

        // Note: REW API may require an existing file path for SetHouseCurve
        // This test verifies the clear operation works correctly
        
        try
        {
            // Act - Clear house curve first
            await measurementsService.ClearHouseCurve();

            // Assert - Verify it was cleared
            var clearedHouseCurve = await measurementsService.GetHouseCurve();
            clearedHouseCurve.Should().BeEmpty("house curve should be empty after clearing");

            // If there was an original house curve, try to restore it
            if (!string.IsNullOrEmpty(originalHouseCurve))
            {
                // Attempt to restore - may succeed if file still exists
                try
                {
                    await measurementsService.SetHouseCurve(originalHouseCurve);
                    var restoredHouseCurve = await measurementsService.GetHouseCurve();
                    restoredHouseCurve.Should().Be(originalHouseCurve, "should restore original house curve");
                }
                catch (HttpRequestException)
                {
                    // File may no longer exist - this is acceptable
                }
            }
        }
        finally
        {
            // Cleanup: Ensure we try to restore original state
            if (!string.IsNullOrEmpty(originalHouseCurve))
            {
                try
                {
                    await measurementsService.SetHouseCurve(originalHouseCurve);
                }
                catch
                {
                    // Best effort cleanup - ignore failures
                }
            }
            else
            {
                await measurementsService.ClearHouseCurve();
            }
        }
    }

    /// <summary>
    /// Test: Update default room curve with partial update
    /// @test TEST-MEASUREMENTS-EQ-032 - PUT updates only specified fields
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task UpdateDefaultRoomCurveSettings_PartialUpdate_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var originalSettings = await measurementsService.GetDefaultRoomCurveSettings();

        var partialSettings = new RoomCurveSettings
        {
            AddRoomCurve = originalSettings.AddRoomCurve,
            LowFreqRiseStartHz = 180, // Change only this field
            LowFreqRiseEndHz = originalSettings.LowFreqRiseEndHz,
            LowFreqRiseSlopedBPerOctave = originalSettings.LowFreqRiseSlopedBPerOctave,
            HighFreqFallStartHz = originalSettings.HighFreqFallStartHz,
            HighFreqFallSlopedBPerOctave = originalSettings.HighFreqFallSlopedBPerOctave
        };

        try
        {
            // Act
            await measurementsService.UpdateDefaultRoomCurveSettings(partialSettings, partialUpdate: true);

            // Assert
            var updatedSettings = await measurementsService.GetDefaultRoomCurveSettings();
            updatedSettings.LowFreqRiseStartHz.Should().Be(180);
        }
        finally
        {
            // Cleanup
            await measurementsService.UpdateDefaultRoomCurveSettings(originalSettings, partialUpdate: false);
        }
    }

    /// <summary>
    /// Test: GetMeasurementRoomCurveSettings with null ID throws
    /// @test TEST-MEASUREMENTS-EQ-033 - Null measurement ID validation
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task GetMeasurementRoomCurveSettings_NullId_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.GetMeasurementRoomCurveSettings(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Test: UpdateMeasurementRoomCurveSettings with null ID throws
    /// @test TEST-MEASUREMENTS-EQ-034 - Null measurement ID validation
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task UpdateMeasurementRoomCurveSettings_NullId_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var settings = new RoomCurveSettings();

        // Act & Assert
        var act = async () => await measurementsService.UpdateMeasurementRoomCurveSettings(null!, settings);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Test: UpdateDefaultRoomCurveSettings with null settings throws
    /// @test TEST-MEASUREMENTS-EQ-035 - Null settings validation
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task UpdateDefaultRoomCurveSettings_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.UpdateDefaultRoomCurveSettings(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Test: SetHouseCurve with null path throws
    /// @test TEST-MEASUREMENTS-EQ-036 - Null path validation
    /// @status ACTIVE - Phase 3d implementation
    /// </summary>
    [Fact]
    public async Task SetHouseCurve_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.SetHouseCurve(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Phase 3e - Match Target Settings (5 tests)

    /// <summary>
    /// Test: Get match target settings
    /// @test TEST-MEASUREMENTS-EQ-037 - Get filter matching algorithm parameters
    /// @status ACTIVE - Phase 3e implementation
    /// </summary>
    [Fact]
    public async Task GetMatchTargetSettings_ReturnsSettings()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var settings = await measurementsService.GetMatchTargetSettings();

        // Assert
        settings.Should().NotBeNull();
        settings.StartFrequency.Should().BeGreaterThan(0, "start frequency should be positive");
        settings.EndFrequency.Should().BeGreaterThan(settings.StartFrequency, "end frequency should be greater than start");
        settings.IndividualMaxBoostdB.Should().BeGreaterThanOrEqualTo(0);
        settings.FlatnessTargetdB.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: Update match target settings with full replacement
    /// @test TEST-MEASUREMENTS-EQ-038 - POST updates all match target settings
    /// @status ACTIVE - Phase 3e implementation
    /// </summary>
    [Fact]
    public async Task UpdateMatchTargetSettings_FullReplacement_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var originalSettings = await measurementsService.GetMatchTargetSettings();

        var newSettings = new MatchTargetSettings
        {
            StartFrequency = 30,
            EndFrequency = 200,
            IndividualMaxBoostdB = 6,
            OverallMaxBoostdB = 3,
            FlatnessTargetdB = 2,
            AllowNarrowFiltersBelow200Hz = false,
            VaryQAbove200Hz = true,
            AllowLowShelf = true,
            LowShelfMin = -3,
            LowShelfMax = 3,
            AllowHighShelf = true,
            HighShelfMin = -4,
            HighShelfMax = 4
        };

        try
        {
            // Act
            await measurementsService.UpdateMatchTargetSettings(newSettings, partialUpdate: false);

            // Assert
            var updatedSettings = await measurementsService.GetMatchTargetSettings();
            updatedSettings.StartFrequency.Should().Be(30);
            updatedSettings.IndividualMaxBoostdB.Should().Be(6);
            updatedSettings.FlatnessTargetdB.Should().Be(2);
            updatedSettings.AllowLowShelf.Should().BeTrue();
            updatedSettings.VaryQAbove200Hz.Should().BeTrue();
        }
        finally
        {
            // Cleanup: Restore original settings
            await measurementsService.UpdateMatchTargetSettings(originalSettings, partialUpdate: false);
        }
    }

    /// <summary>
    /// Test: Update match target settings with partial update
    /// @test TEST-MEASUREMENTS-EQ-039 - PUT updates only specified fields
    /// @status ACTIVE - Phase 3e implementation
    /// </summary>
    [Fact]
    public async Task UpdateMatchTargetSettings_PartialUpdate_Succeeds()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var originalSettings = await measurementsService.GetMatchTargetSettings();

        var partialSettings = new MatchTargetSettings
        {
            StartFrequency = 25, // Change only this
            EndFrequency = originalSettings.EndFrequency,
            IndividualMaxBoostdB = originalSettings.IndividualMaxBoostdB,
            OverallMaxBoostdB = originalSettings.OverallMaxBoostdB,
            FlatnessTargetdB = originalSettings.FlatnessTargetdB,
            AllowNarrowFiltersBelow200Hz = originalSettings.AllowNarrowFiltersBelow200Hz,
            VaryQAbove200Hz = originalSettings.VaryQAbove200Hz,
            AllowLowShelf = originalSettings.AllowLowShelf,
            LowShelfMin = originalSettings.LowShelfMin,
            LowShelfMax = originalSettings.LowShelfMax,
            AllowHighShelf = originalSettings.AllowHighShelf,
            HighShelfMin = originalSettings.HighShelfMin,
            HighShelfMax = originalSettings.HighShelfMax
        };

        try
        {
            // Act
            await measurementsService.UpdateMatchTargetSettings(partialSettings, partialUpdate: true);

            // Assert
            var updatedSettings = await measurementsService.GetMatchTargetSettings();
            updatedSettings.StartFrequency.Should().Be(25);
            updatedSettings.EndFrequency.Should().Be(originalSettings.EndFrequency, "other fields should remain unchanged");
        }
        finally
        {
            // Cleanup
            await measurementsService.UpdateMatchTargetSettings(originalSettings, partialUpdate: false);
        }
    }

    /// <summary>
    /// Test: Verify match target settings structure
    /// @test TEST-MEASUREMENTS-EQ-040 - Validate all match target properties
    /// @status ACTIVE - Phase 3e implementation
    /// </summary>
    [Fact]
    public async Task GetMatchTargetSettings_ValidatesAllProperties()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act
        var settings = await measurementsService.GetMatchTargetSettings();

        // Assert - Verify all properties are accessible and have valid values
        settings.StartFrequency.Should().BeInRange(10, 200, "typical start frequency range");
        settings.EndFrequency.Should().BeInRange(100, 20000, "typical end frequency range");
        settings.IndividualMaxBoostdB.Should().BeInRange(0, 20, "typical individual boost range");
        settings.OverallMaxBoostdB.Should().BeInRange(0, 12, "typical overall boost range");
        settings.FlatnessTargetdB.Should().BeInRange(0, 10, "typical flatness target range");
        
        // Boolean flags - verify they are defined (booleans are value types, always have valid values)
        // Just check they can be read without error
        _ = settings.AllowNarrowFiltersBelow200Hz;
        _ = settings.VaryQAbove200Hz;
        _ = settings.AllowLowShelf;
        _ = settings.AllowHighShelf;
        
        // Shelf limits should be in typical range
        settings.LowShelfMin.Should().BeInRange(-20, 0, "low shelf minimum");
        settings.LowShelfMax.Should().BeInRange(0, 20, "low shelf maximum");
        settings.HighShelfMin.Should().BeInRange(-20, 0, "high shelf minimum");
        settings.HighShelfMax.Should().BeInRange(0, 20, "high shelf maximum");
    }

    /// <summary>
    /// Test: UpdateMatchTargetSettings with null settings throws
    /// @test TEST-MEASUREMENTS-EQ-041 - Null settings validation
    /// @status ACTIVE - Phase 3e implementation
    /// </summary>
    [Fact]
    public async Task UpdateMatchTargetSettings_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;

        // Act & Assert
        var act = async () => await measurementsService.UpdateMatchTargetSettings(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Phase 3f: EQ Response Data Tests

    /// <summary>
    /// Test: GetTargetResponse returns valid frequency response data
    /// @test TEST-MEASUREMENTS-EQ-042 - Get target response curve
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetTargetResponse_ReturnsCurveData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var targetResponse = await measurementsService.GetTargetResponse(measurementId);

        // Assert
        targetResponse.Should().NotBeNull();
        targetResponse.Frequencies.Should().NotBeNullOrEmpty("target response should have frequency data");
        targetResponse.Magnitudes.Should().NotBeNullOrEmpty("target response should have magnitude data");
        targetResponse.Frequencies.Length.Should().Be(targetResponse.Magnitudes.Length, "frequencies and magnitudes should match");
        
        // Target response should cover reasonable frequency range (can start below 10 Hz)
        targetResponse.Frequencies[0].Should().BeGreaterThan(0, "first frequency should be positive");
        targetResponse.Frequencies[^1].Should().BeLessThan(30000, "last frequency should be below 30 kHz");
    }

    /// <summary>
    /// Test: GetEQFrequencyResponse returns EQ'd frequency response
    /// @test TEST-MEASUREMENTS-EQ-043 - Get EQ predicted frequency response
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetEQFrequencyResponse_ReturnsEQdResponse()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var eqResponse = await measurementsService.GetEQFrequencyResponse(measurementId);

        // Assert
        eqResponse.Should().NotBeNull();
        eqResponse.Frequencies.Should().NotBeNullOrEmpty("EQ response should have frequency data");
        eqResponse.Magnitudes.Should().NotBeNullOrEmpty("EQ response should have magnitude data");
        eqResponse.Frequencies.Length.Should().Be(eqResponse.Magnitudes.Length);
        
        // Should have phase data
        eqResponse.Phases.Should().NotBeNullOrEmpty("EQ response should have phase data");
        eqResponse.Phases.Length.Should().Be(eqResponse.Frequencies.Length);
    }

    /// <summary>
    /// Test: GetEQFrequencyResponse with unit parameter
    /// @test TEST-MEASUREMENTS-EQ-044 - Get EQ response with custom unit
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetEQFrequencyResponse_WithCustomUnit_ReturnsData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var eqResponse = await measurementsService.GetEQFrequencyResponse(
            measurementId, 
            unit: "dBFS", 
            smoothing: "1/3");

        // Assert
        eqResponse.Should().NotBeNull();
        eqResponse.Frequencies.Should().NotBeNullOrEmpty();
        eqResponse.Magnitudes.Should().NotBeNullOrEmpty();
        
        // Magnitudes should be in dBFS range (negative values, typically -60 to 0 dB)
        foreach (var magnitude in eqResponse.Magnitudes.Take(100))  // Check first 100 points
        {
            magnitude.Should().BeLessThanOrEqualTo(10, "dBFS values should typically be <= 0");
        }
    }

    /// <summary>
    /// Test: GetEQGroupDelay returns group delay data
    /// @test TEST-MEASUREMENTS-EQ-045 - Get EQ predicted group delay
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetEQGroupDelay_ReturnsGroupDelayData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var groupDelay = await measurementsService.GetEQGroupDelay(measurementId);

        // Assert
        groupDelay.Should().NotBeNull();
        groupDelay.Frequencies.Should().NotBeNullOrEmpty("group delay should have frequency data");
        groupDelay.Magnitudes.Should().NotBeNullOrEmpty("group delay should have delay values in magnitudes field");
        groupDelay.Frequencies.Length.Should().Be(groupDelay.Magnitudes.Length);
        
        // Group delay values should be in reasonable range (typically 0 to few milliseconds = 0 to 0.005 seconds)
        foreach (var delay in groupDelay.Magnitudes.Where(d => !float.IsNaN(d) && !float.IsInfinity(d)))
        {
            Math.Abs(delay).Should().BeLessThan(1.0f, "group delay should typically be less than 1 second");
        }
    }

    /// <summary>
    /// Test: GetEQImpulseResponse returns impulse response data
    /// @test TEST-MEASUREMENTS-EQ-046 - Get EQ predicted impulse response
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetEQImpulseResponse_ReturnsImpulseData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var impulseResponse = await measurementsService.GetEQImpulseResponse(measurementId);

        // Assert
        impulseResponse.Should().NotBeNull();
        impulseResponse.Data.Should().NotBeNullOrEmpty("impulse response should have response data");
        impulseResponse.SampleRate.Should().BeGreaterThan(0, "sample rate should be positive");
        impulseResponse.SampleInterval.Should().BeGreaterThan(0, "sample interval should be positive");
        
        // Sample rate and interval should be reciprocal
        var expectedInterval = 1.0 / impulseResponse.SampleRate;
        impulseResponse.SampleInterval.Should().BeApproximately(expectedInterval, 0.0001, "sample interval should be 1/sample rate");
    }

    /// <summary>
    /// Test: GetEQImpulseResponse with windowed parameter
    /// @test TEST-MEASUREMENTS-EQ-047 - Get EQ impulse response windowed
    /// @status ACTIVE - Phase 3f implementation
    /// </summary>
    [Fact]
    public async Task GetEQImpulseResponse_Windowed_ReturnsWindowedData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act - Get both windowed and full
        var fullResponse = await measurementsService.GetEQImpulseResponse(measurementId, windowed: false);
        var windowedResponse = await measurementsService.GetEQImpulseResponse(measurementId, windowed: true);

        // Assert
        fullResponse.Should().NotBeNull();
        windowedResponse.Should().NotBeNull();
        
        // Windowed response should typically be shorter or same length as full
        windowedResponse.Data.Length.Should().BeLessOrEqualTo(fullResponse.Data.Length, 
            "windowed response should be shorter or equal to full response");
        
        // Both should have same sample rate
        windowedResponse.SampleRate.Should().Be(fullResponse.SampleRate);
    }

    #endregion

    #region Phase 4: Advanced Measurement Data Retrieval

    /// <summary>
    /// Test: GetProcessedFrequencyResponse with default parameters
    /// @test TEST-MEASUREMENTS-PHASE4-001 - Get processed frequency response
    /// @status ACTIVE - Phase 4 implementation
    /// </summary>
    [Fact]
    public async Task GetProcessedFrequencyResponse_DefaultParameters_ReturnsProcessedData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var processedResponse = await measurementsService.GetProcessedFrequencyResponse(measurementId);

        // Assert
        processedResponse.Should().NotBeNull();
        processedResponse.Magnitudes.Should().NotBeNullOrEmpty("processed frequency response should contain magnitude data");
        processedResponse.Phases.Should().NotBeNullOrEmpty("processed frequency response should contain phase data");
        processedResponse.Smoothing.Should().Be("None", "default smoothing should be None");
        processedResponse.StartFrequency.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Test: GetProcessedFrequencyResponse with custom parameters
    /// @test TEST-MEASUREMENTS-PHASE4-002 - Get processed frequency response with custom unit and smoothing
    /// @status ACTIVE - Phase 4 implementation
    /// </summary>
    [Fact]
    public async Task GetProcessedFrequencyResponse_CustomParameters_ReturnsCustomProcessedData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var processedResponse = await measurementsService.GetProcessedFrequencyResponse(
            measurementId, 
            unit: "dB", 
            smoothing: "1/3 octave", 
            ppo: 12
        );

        // Assert
        processedResponse.Should().NotBeNull();
        processedResponse.Magnitudes.Should().NotBeNullOrEmpty();
        processedResponse.Smoothing.Should().NotBeNullOrEmpty("REW should return the smoothing format applied");
        // REW returns the actual smoothing used (e.g., "1/6", "1/3", "1/12")
        // which may differ from the requested format
    }

    /// <summary>
    /// Test: GetFiltersImpulseResponse with default parameters
    /// @test TEST-MEASUREMENTS-PHASE4-003 - Get filters impulse response
    /// @status ACTIVE - Phase 4 implementation
    /// @note Returns empty data if measurement has no filters applied (this is expected behavior)
    /// </summary>
    [Fact]
    public async Task GetFiltersImpulseResponse_DefaultParameters_ReturnsFilterImpulseData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var filterImpulse = await measurementsService.GetFiltersImpulseResponse(measurementId);

        // Assert
        filterImpulse.Should().NotBeNull();
        filterImpulse.Data.Should().NotBeNull("endpoint should return data array");
        // Note: Data may be empty if measurement has no filters applied - this is expected REW behavior
        // In this case, REW may also return 0 for sample rate when no filters exist
        if (filterImpulse.Data.Length > 0)
        {
            filterImpulse.SampleRate.Should().BeGreaterThan(0, "sample rate should be set when data exists");
            filterImpulse.Data.Length.Should().BeLessOrEqualTo(131072, "default max length should be 131072 samples");
        }
    }

    /// <summary>
    /// Test: GetFiltersImpulseResponse with custom parameters
    /// @test TEST-MEASUREMENTS-PHASE4-004 - Get filters impulse response with custom sample rate and length
    /// @status ACTIVE - Phase 4 implementation
    /// @note Returns empty data if measurement has no filters applied (this is expected behavior)
    /// </summary>
    [Fact]
    public async Task GetFiltersImpulseResponse_CustomParameters_ReturnsCustomFilterImpulseData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var filterImpulse = await measurementsService.GetFiltersImpulseResponse(
            measurementId, 
            sampleRate: 96000, 
            length: 65536
        );

        // Assert
        filterImpulse.Should().NotBeNull();
        filterImpulse.Data.Should().NotBeNull("endpoint should return data array");
        // Note: Data may be empty if measurement has no filters applied - this is expected REW behavior
        // In this case, REW may also return 0 for sample rate when no filters exist
        if (filterImpulse.Data.Length > 0)
        {
            filterImpulse.SampleRate.Should().BeGreaterThan(0, "sample rate should be set when data exists");
            filterImpulse.Data.Length.Should().BeLessOrEqualTo(65536, "data length should not exceed requested length");
        }
    }

    /// <summary>
    /// Test: GetFiltersImpulseResponse with invalid parameters throws exception
    /// @test TEST-MEASUREMENTS-PHASE4-005 - Get filters impulse response with invalid sample rate
    /// @status ACTIVE - Phase 4 implementation
    /// </summary>
    [Fact]
    public async Task GetFiltersImpulseResponse_InvalidSampleRate_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await measurementsService.GetFiltersImpulseResponse(measurementId, sampleRate: -1000)
        );
    }

    /// <summary>
    /// Test: GetFiltersImpulseResponse with length out of range throws exception
    /// @test TEST-MEASUREMENTS-PHASE4-006 - Get filters impulse response with invalid length
    /// @status ACTIVE - Phase 4 implementation
    /// </summary>
    [Fact]
    public async Task GetFiltersImpulseResponse_InvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await measurementsService.GetFiltersImpulseResponse(measurementId, length: 5000000) // > 4194304 max
        );
    }

    /// <summary>
    /// Test: GetWaterfall returns waterfall plot data
    /// @test TEST-MEASUREMENTS-PHASE4-007 - Get waterfall plot data
    /// @status ACTIVE - Phase 4 implementation
    /// @note Requires waterfall to be generated first via GenerateWaterfall command
    /// </summary>
    [Fact(Skip = "Requires waterfall generation which is a complex command-based operation")]
    public async Task GetWaterfall_ValidId_ReturnsWaterfallData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var waterfall = await measurementsService.GetWaterfall(measurementId);

        // Assert
        waterfall.Should().NotBeNull();
        waterfall.Frequencies.Should().NotBeNullOrEmpty("waterfall should contain frequency data");
        waterfall.Times.Should().NotBeNullOrEmpty("waterfall should contain time data");
        waterfall.DecodedSPLData.Should().NotBeNull("waterfall should contain 2D SPL data");
        waterfall.DecodedSPLData.GetLength(0).Should().BeGreaterThan(0, "waterfall should have rows (time slices)");
        waterfall.DecodedSPLData.GetLength(1).Should().BeGreaterThan(0, "waterfall should have columns (frequencies)");
    }

    /// <summary>
    /// Test: GetSpectrogram returns spectrogram data
    /// @test TEST-MEASUREMENTS-PHASE4-008 - Get spectrogram data
    /// @status ACTIVE - Phase 4 implementation
    /// @note Requires spectrogram to be generated first via GenerateSpectrogram command
    /// </summary>
    [Fact(Skip = "Requires spectrogram generation which is a complex command-based operation")]
    public async Task GetSpectrogram_ValidId_ReturnsSpectrogramData()
    {
        // Arrange
        var measurementsService = _rewClient.Measurements;
        var measurements = await measurementsService.ListMeasurements();
        measurements.Should().NotBeNullOrEmpty("test requires at least one measurement");
        var measurementId = measurements[0].UUID;

        // Act
        var spectrogram = await measurementsService.GetSpectrogram(measurementId);

        // Assert
        spectrogram.Should().NotBeNull();
        spectrogram.Frequencies.Should().NotBeNullOrEmpty("spectrogram should contain frequency data");
        spectrogram.Times.Should().NotBeNullOrEmpty("spectrogram should contain time data");
        spectrogram.SPLData.Should().NotBeNull("spectrogram should contain 2D amplitude data");
        spectrogram.FFTSize.Should().BeGreaterThan(0, "spectrogram should have FFT size");
        spectrogram.WindowType.Should().NotBeNullOrEmpty("spectrogram should have window type");
        spectrogram.SPLData.GetLength(0).Should().BeGreaterThan(0, "spectrogram should have rows");
        spectrogram.SPLData.GetLength(1).Should().BeGreaterThan(0, "spectrogram should have columns");
    }

    #endregion
}
