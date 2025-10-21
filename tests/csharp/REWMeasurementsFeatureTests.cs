using Xunit;
using FluentAssertions;
using REWApi.Domain.Services;
using System.Threading.Tasks;

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
    /// Test: Export measurement to file
    /// @test TEST-MEASUREMENTS-FEATURE-004 - Export measurement
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
}
