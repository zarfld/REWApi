using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using REWApi.Domain;
using REWApi.Domain.Services;
using REWApi.Tests.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace REWApi.Tests
{
    /// <summary>
    /// Tests for SPL Meter Service
    /// ISO/IEC/IEEE 12207:2017 - Verification and Validation
    /// IEEE 1012-2016 - Verification and Validation
    /// 
    /// Test REW SPL Meter API endpoints:
    /// - GET/POST /spl-meter/{id}/command
    /// - GET/POST /spl-meter/{id}/configuration
    /// - GET /spl-meter/modes
    /// - GET /spl-meter/weightings
    /// - GET /spl-meter/filters
    /// - GET /spl-meter/{id}/levels
    /// </summary>
    public class SPLMeterServiceTests : IClassFixture<RewClientFixture>
    {
        private readonly RewClientFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly SPLMeterService _splMeterService;

        public SPLMeterServiceTests(RewClientFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _splMeterService = new SPLMeterService(_fixture.HttpClient, _fixture.BaseUrl);
        }

        #region Domain Object Tests

        [Fact]
        public void SPLMeterConfiguration_SetStandardAWeightingFast_ShouldSetCorrectValues()
        {
            // Arrange
            var config = new SPLMeterConfiguration();

            // Act
            config.SetStandardAWeightingFast();

            // Assert
            config.Mode.Should().Be("SPL");
            config.Weighting.Should().Be("A");
            config.Filter.Should().Be("Fast");
            config.HighPassActive.Should().BeFalse();
            config.RollingLeqActive.Should().BeFalse();
            config.RollingLeqMinutes.Should().Be(1);
            config.IsValid().Should().BeTrue();

            _output.WriteLine($"✅ Standard A-weighting Fast configuration: {config.Weighting} weighting, {config.Filter} filter");
        }

        [Fact]
        public void SPLMeterConfiguration_SetLongTermMonitoring_ShouldSetCorrectValues()
        {
            // Arrange
            var config = new SPLMeterConfiguration();

            // Act
            config.SetLongTermMonitoring(15);

            // Assert
            config.Mode.Should().Be("SPL");
            config.Weighting.Should().Be("C");
            config.Filter.Should().Be("Slow");
            config.RollingLeqActive.Should().BeTrue();
            config.RollingLeqMinutes.Should().Be(15);
            config.IsValid().Should().BeTrue();

            _output.WriteLine($"✅ Long-term monitoring configuration: {config.RollingLeqMinutes}min rolling Leq");
        }

        [Fact]
        public void SPLMeterConfiguration_IsValid_ShouldDetectInvalidConfiguration()
        {
            // Arrange - missing required fields
            var config = new SPLMeterConfiguration
            {
                Mode = "SPL",
                // Missing Weighting
                Filter = "Fast",
                RollingLeqMinutes = 1
            };

            // Assert
            config.IsValid().Should().BeFalse();
            _output.WriteLine("✅ Invalid configuration correctly detected (missing weighting)");
        }

        [Fact]
        public void SPLValues_ExceedsThreshold_ShouldDetectExcessiveSPL()
        {
            // Arrange
            var values = new SPLValues
            {
                SPL = 95.5,
                Weighting = "A",
                Filter = "Fast"
            };

            // Act & Assert
            values.ExceedsThreshold(90.0).Should().BeTrue();
            values.ExceedsThreshold(100.0).Should().BeFalse();

            _output.WriteLine($"✅ SPL {values.SPL} dB exceeds 90 dB threshold: {values.ExceedsThreshold(90.0)}");
        }

        [Fact]
        public void SPLValues_FormatReading_ShouldFormatCorrectly()
        {
            // Arrange
            var values = new SPLValues
            {
                SPL = 85.3,
                Weighting = "A",
                Filter = "Fast"
            };

            // Act
            var formatted = values.FormatReading();

            // Assert
            formatted.Should().Be("85.3 dB(A) Fast");
            _output.WriteLine($"✅ Formatted reading: {formatted}");
        }

        [Fact]
        public void SPLValues_GetLeqForTimeWindow_ShouldReturnCorrectLeq()
        {
            // Arrange
            var values = new SPLValues
            {
                Leq1m = 80.0,
                Leq10m = 82.0,
                Leq = 85.0, // Rolling Leq
                IsRollingLeq = true,
                RollingLeqMinutes = 15
            };

            // Act & Assert
            values.GetLeqForTimeWindow(1).Should().Be(80.0);
            values.GetLeqForTimeWindow(10).Should().Be(82.0);
            values.GetLeqForTimeWindow(15).Should().Be(85.0);

            _output.WriteLine($"✅ Leq time windows: 1min={values.Leq1m}, 10min={values.Leq10m}, 15min={values.Leq}");
        }

        #endregion

        #region Command Tests

        [Fact]
        public async Task GetCommands_ShouldReturnCommandList()
        {
            // Act
            var commands = await _splMeterService.GetCommands();

            // Assert
            commands.Should().NotBeNull();
            commands.Should().Contain("Start");
            commands.Should().Contain("Stop");

            _output.WriteLine($"✅ Available SPL meter commands: {string.Join(", ", commands)}");
        }

        [Fact]
        public async Task Start_ShouldStartSPLMeterSuccessfully()
        {
            // Arrange
            var meterNumber = 1;

            // Act
            await _splMeterService.Start(meterNumber);

            // Give meter time to start
            await Task.Delay(500);

            // Assert - verify by getting levels (should not throw if meter is running)
            var levels = await _splMeterService.GetLevels(meterNumber);
            levels.Should().NotBeNull();

            _output.WriteLine($"✅ SPL meter {meterNumber} started successfully");
            _output.WriteLine($"   Current SPL: {levels.FormatReading()}");

            // Cleanup - stop meter
            await _splMeterService.Stop(meterNumber);
        }

        [Fact]
        public async Task Stop_ShouldStopSPLMeterSuccessfully()
        {
            // Arrange
            var meterNumber = 1;
            await _splMeterService.Start(meterNumber);
            await Task.Delay(500);

            // Act
            await _splMeterService.Stop(meterNumber);

            _output.WriteLine($"✅ SPL meter {meterNumber} stopped successfully");
        }

        [Fact]
        public async Task ExecuteCommand_WithInvalidMeterNumber_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var invalidMeterNumber = 5; // Only 1-4 are valid

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await _splMeterService.ExecuteCommand(invalidMeterNumber, "Start")
            );

            _output.WriteLine($"✅ Invalid meter number {invalidMeterNumber} correctly rejected");
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task GetConfiguration_ShouldReturnValidConfiguration()
        {
            // Act
            var config = await _splMeterService.GetConfiguration(1);

            // Assert
            config.Should().NotBeNull();
            config.Mode.Should().NotBeNullOrEmpty();
            config.Weighting.Should().NotBeNullOrEmpty();
            config.Filter.Should().NotBeNullOrEmpty();
            config.IsValid().Should().BeTrue();

            _output.WriteLine($"✅ SPL meter configuration: {config.Mode}, {config.Weighting} weighting, {config.Filter} filter");
            _output.WriteLine($"   Rolling Leq: {config.RollingLeqActive} ({config.RollingLeqMinutes} min)");
        }

        [Fact]
        public async Task UpdateConfiguration_WithStandardAWeighting_ShouldUpdateSuccessfully()
        {
            // Arrange
            var config = new SPLMeterConfiguration();
            config.SetStandardAWeightingFast();

            // Act
            await _splMeterService.UpdateConfiguration(1, config);

            // Verify
            var updated = await _splMeterService.GetConfiguration(1);

            // Assert
            updated.Weighting.Should().Be("A");
            updated.Filter.Should().Be("Fast");

            _output.WriteLine($"✅ Configuration updated to A-weighting Fast");
        }

        [Fact]
        public async Task UpdateConfiguration_WithLongTermMonitoring_ShouldUpdateSuccessfully()
        {
            // Arrange
            var config = new SPLMeterConfiguration();
            config.SetLongTermMonitoring(15);

            // Act
            await _splMeterService.UpdateConfiguration(1, config);

            // Verify
            var updated = await _splMeterService.GetConfiguration(1);

            // Assert
            updated.Weighting.Should().Be("C");
            updated.Filter.Should().Be("Slow");
            updated.RollingLeqActive.Should().BeTrue();
            updated.RollingLeqMinutes.Should().Be(15);

            _output.WriteLine($"✅ Configuration updated to long-term monitoring (15min rolling Leq)");
        }

        [Fact]
        public async Task UpdateConfiguration_WithInvalidConfig_ShouldThrowArgumentException()
        {
            // Arrange - invalid configuration (missing weighting)
            var config = new SPLMeterConfiguration
            {
                Mode = "SPL",
                Filter = "Fast",
                RollingLeqMinutes = 1
                // Missing Weighting
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _splMeterService.UpdateConfiguration(1, config)
            );

            _output.WriteLine($"✅ Invalid configuration correctly rejected");
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task GetModes_ShouldReturnModesList()
        {
            // Act
            var modes = await _splMeterService.GetModes();

            // Assert
            modes.Should().NotBeNull();
            modes.Should().Contain("SPL");

            _output.WriteLine($"✅ Available modes: {string.Join(", ", modes)}");
        }

        [Fact]
        public async Task GetWeightings_ShouldReturnWeightingsList()
        {
            // Act
            var weightings = await _splMeterService.GetWeightings();

            // Assert
            weightings.Should().NotBeNull();
            weightings.Should().Contain("A");
            weightings.Should().Contain("C");
            weightings.Should().Contain("Z");

            _output.WriteLine($"✅ Available weightings: {string.Join(", ", weightings)}");
        }

        [Fact]
        public async Task GetFilters_ShouldReturnFiltersList()
        {
            // Act
            var filters = await _splMeterService.GetFilters();

            // Assert
            filters.Should().NotBeNull();
            filters.Should().Contain("Fast");
            filters.Should().Contain("Slow");

            _output.WriteLine($"✅ Available filters: {string.Join(", ", filters)}");
        }

        #endregion

        #region Data Retrieval Tests

        [Fact]
        public async Task GetLevels_ShouldReturnValidSPLValues()
        {
            // Arrange - start meter first
            await _splMeterService.Start(1);
            await Task.Delay(1000); // Let meter collect some data

            try
            {
                // Act
                var levels = await _splMeterService.GetLevels(1);

                // Assert
                levels.Should().NotBeNull();
                levels.MeterNumber.Should().Be(1);
                levels.Weighting.Should().NotBeNullOrEmpty();
                levels.Filter.Should().NotBeNullOrEmpty();
                levels.SPL.Should().BeGreaterThanOrEqualTo(0);
                levels.Leq.Should().BeGreaterThanOrEqualTo(0);
                levels.SEL.Should().BeGreaterThanOrEqualTo(0);
                levels.ElapsedTime.Should().BeGreaterThan(0);
                levels.IsValid().Should().BeTrue();

                _output.WriteLine($"✅ SPL meter readings:");
                _output.WriteLine($"   {levels.FormatReading()}");
                _output.WriteLine($"   Leq: {levels.Leq:F1} dB, SEL: {levels.SEL:F1} dB");
                _output.WriteLine($"   Elapsed time: {levels.ElapsedTime:F1} seconds");
            }
            finally
            {
                // Cleanup
                await _splMeterService.Stop(1);
            }
        }

        [Fact]
        public async Task GetLevels_WithDifferentConfigurations_ShouldReturnCorrectWeighting()
        {
            // Arrange - configure for C-weighting
            var config = new SPLMeterConfiguration();
            config.SetLongTermMonitoring(1);
            await _splMeterService.UpdateConfiguration(1, config);

            await _splMeterService.Start(1);
            await Task.Delay(1000);

            try
            {
                // Act
                var levels = await _splMeterService.GetLevels(1);

                // Assert
                levels.Weighting.Should().Be("C");
                levels.Filter.Should().Be("Slow");

                _output.WriteLine($"✅ C-weighting levels: {levels.FormatReading()}");
            }
            finally
            {
                // Cleanup
                await _splMeterService.Stop(1);
            }
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task SPLMeter_CompleteWorkflow_ShouldWorkCorrectly()
        {
            // This test demonstrates a complete SPL meter workflow
            _output.WriteLine("=== SPL Meter Complete Workflow ===");

            try
            {
                // 1. Get available options
                var modes = await _splMeterService.GetModes();
                var weightings = await _splMeterService.GetWeightings();
                var filters = await _splMeterService.GetFilters();

                _output.WriteLine($"1. Available options:");
                _output.WriteLine($"   Modes: {string.Join(", ", modes)}");
                _output.WriteLine($"   Weightings: {string.Join(", ", weightings)}");
                _output.WriteLine($"   Filters: {string.Join(", ", filters)}");

                // 2. Configure meter for A-weighting Fast
                var config = new SPLMeterConfiguration();
                config.SetStandardAWeightingFast();
                await _splMeterService.UpdateConfiguration(1, config);

                _output.WriteLine($"2. Configured meter for A-weighting Fast");

                // 3. Start meter
                await _splMeterService.Start(1);
                _output.WriteLine($"3. Started SPL meter");

                // 4. Wait for measurements
                await Task.Delay(2000);

                // 5. Get levels
                var levels = await _splMeterService.GetLevels(1);
                _output.WriteLine($"4. Current readings:");
                _output.WriteLine($"   {levels.FormatReading()}");
                _output.WriteLine($"   Leq: {levels.Leq:F1} dB");
                _output.WriteLine($"   Elapsed: {levels.ElapsedTime:F1} seconds");

                // 6. Check for threshold exceeded
                const double safetyThreshold = 85.0; // OSHA 8-hour exposure limit
                if (levels.ExceedsThreshold(safetyThreshold))
                {
                    _output.WriteLine($"   ⚠️  WARNING: SPL exceeds {safetyThreshold} dB safety threshold!");
                }

                // Assert
                levels.IsValid().Should().BeTrue();
                levels.Weighting.Should().Be("A");
                levels.Filter.Should().Be("Fast");
            }
            finally
            {
                // 7. Stop meter
                await _splMeterService.Stop(1);
                _output.WriteLine($"5. Stopped SPL meter");
            }
        }

        #endregion
    }
}
