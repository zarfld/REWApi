using System;
using System.Collections.Generic;
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
    /// Tests for RTA (Real-Time Analyzer) Service
    /// ISO/IEC/IEEE 12207:2017 - Verification and Validation
    /// IEEE 1012-2016 - Verification and Validation
    /// 
    /// Test REW RTA API endpoints (40 endpoints):
    /// - GET /rta/status
    /// - GET/POST /rta/commands
    /// - GET/POST/PUT /rta/configuration
    /// - GET/POST /rta/appearance-configuration
    /// - GET/POST /rta/distortion-configuration
    /// - GET/POST /rta/y-axis, /rta/graph-limits, /rta/cursor-x
    /// - GET /rta/levels, /rta/captured-data, /rta/captured-peak-data, /rta/distortion
    /// - GET /rta/image-capture-settings
    /// </summary>
    public class RTAServiceTests : IClassFixture<RewClientFixture>, IAsyncLifetime
    {
        private readonly RewClientFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly RTAService _rtaService;
        private readonly RewClient _client;
        private TestPreconditions? _preconditions;
        private PreconditionResult? _setupResult;

        public RTAServiceTests(RewClientFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _rtaService = new RTAService(_fixture.HttpClient, _fixture.BaseUrl);
            _client = _fixture.Client!;
        }

        public async Task InitializeAsync()
        {
            // Setup test preconditions once for all tests
            _preconditions = new TestPreconditions(_client, _output);
            _setupResult = await _preconditions.SetupAllPreconditions();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        #region Domain Object Tests

        [Fact]
        public void RTAStatus_CanStart_ShouldRequireEnabledAndNotRunning()
        {
            // Arrange
            var enabledNotRunning = new RTAStatus { Enabled = true, Running = false };
            var enabledRunning = new RTAStatus { Enabled = true, Running = true };
            var disabledNotRunning = new RTAStatus { Enabled = false, Running = false };

            // Act & Assert
            enabledNotRunning.CanStart().Should().BeTrue("enabled and not running allows start");
            enabledRunning.CanStart().Should().BeFalse("already running prevents start");
            disabledNotRunning.CanStart().Should().BeFalse("disabled prevents start");
        }

        [Fact]
        public void RTAStatus_CanStop_ShouldRequireEnabledAndRunning()
        {
            // Arrange
            var enabledRunning = new RTAStatus { Enabled = true, Running = true };
            var enabledNotRunning = new RTAStatus { Enabled = true, Running = false };
            var disabledRunning = new RTAStatus { Enabled = false, Running = true };

            // Act & Assert
            enabledRunning.CanStop().Should().BeTrue("enabled and running allows stop");
            enabledNotRunning.CanStop().Should().BeFalse("not running prevents stop");
            disabledRunning.CanStop().Should().BeFalse("disabled prevents stop");
        }

        [Fact]
        public void RTAConfiguration_IsValidFFTLength_ShouldCheckPowerOf2()
        {
            // Arrange
            var validConfig = new RTAConfiguration { FFTLength = 32768 }; // 2^15
            var invalidPowerOf2 = new RTAConfiguration { FFTLength = 30000 };
            var tooSmall = new RTAConfiguration { FFTLength = 256 };
            var tooLarge = new RTAConfiguration { FFTLength = 262144 };

            // Act & Assert
            validConfig.IsValidFFTLength().Should().BeTrue("32768 is power of 2 in valid range");
            invalidPowerOf2.IsValidFFTLength().Should().BeFalse("30000 is not power of 2");
            tooSmall.IsValidFFTLength().Should().BeFalse("256 is below minimum 512");
            tooLarge.IsValidFFTLength().Should().BeFalse("262144 exceeds maximum 131072");
        }

        [Fact]
        public void RTAConfiguration_IsValidAveraging_ShouldValidateSettings()
        {
            // Arrange
            var linearValid = new RTAConfiguration { Averaging = "Linear", Averages = 8 };
            var linearInvalid = new RTAConfiguration { Averaging = "Linear", Averages = 150 };
            var exponentialValid = new RTAConfiguration { Averaging = "Exponential", Decay = 0.5 };
            var exponentialInvalid = new RTAConfiguration { Averaging = "Exponential", Decay = 1.5 };
            var none = new RTAConfiguration { Averaging = "None" };

            // Act & Assert
            linearValid.IsValidAveraging().Should().BeTrue("Linear with 8 averages is valid");
            linearInvalid.IsValidAveraging().Should().BeFalse("Linear with 150 averages exceeds max");
            exponentialValid.IsValidAveraging().Should().BeTrue("Exponential with 0.5 decay is valid");
            exponentialInvalid.IsValidAveraging().Should().BeFalse("Exponential with 1.5 decay exceeds max");
            none.IsValidAveraging().Should().BeTrue("None is always valid");
        }

        [Fact]
        public void RTADistortionConfiguration_IsValidHarmonics_ShouldCheckRange()
        {
            // Arrange
            var validMin = new RTADistortionConfiguration { Harmonics = 2 };
            var validMax = new RTADistortionConfiguration { Harmonics = 10 };
            var tooLow = new RTADistortionConfiguration { Harmonics = 1 };
            var tooHigh = new RTADistortionConfiguration { Harmonics = 15 };

            // Act & Assert
            validMin.IsValidHarmonics().Should().BeTrue("2 harmonics is minimum valid");
            validMax.IsValidHarmonics().Should().BeTrue("10 harmonics is maximum valid");
            tooLow.IsValidHarmonics().Should().BeFalse("1 harmonic is below minimum");
            tooHigh.IsValidHarmonics().Should().BeFalse("15 harmonics exceeds maximum");
        }

        [Fact]
        public void RTADistortion_GetHarmonicLevel_ShouldReturnCorrectIndex()
        {
            // Arrange
            var distortion = new RTADistortion
            {
                HarmonicLevels = new[] { -40.0, -50.0, -60.0 } // 2nd, 3rd, 4th harmonics
            };

            // Act & Assert
            distortion.GetHarmonicLevel(2).Should().Be(-40.0, "2nd harmonic is index 0");
            distortion.GetHarmonicLevel(3).Should().Be(-50.0, "3rd harmonic is index 1");
            distortion.GetHarmonicLevel(4).Should().Be(-60.0, "4th harmonic is index 2");
        }

        [Fact]
        public void RTADistortion_IsLowDistortion_ShouldCheckThresholds()
        {
            // Arrange
            var lowPercent = new RTADistortion { THD = 0.5, DistortionUnit = "percent" };
            var highPercent = new RTADistortion { THD = 2.0, DistortionUnit = "percent" };
            var lowDB = new RTADistortion { THD = -50.0, DistortionUnit = "dB" };
            var highDB = new RTADistortion { THD = -30.0, DistortionUnit = "dB" };

            // Act & Assert
            lowPercent.IsLowDistortion().Should().BeTrue("0.5% is below 1% threshold");
            highPercent.IsLowDistortion().Should().BeFalse("2% exceeds 1% threshold");
            lowDB.IsLowDistortion().Should().BeTrue("-50dB is below -40dB threshold");
            highDB.IsLowDistortion().Should().BeFalse("-30dB exceeds -40dB threshold");
        }

        [Fact]
        public void RTAGraphLimits_IsValidRange_ShouldCheckSpan()
        {
            // Arrange
            var validRange = new RTAGraphLimits { Min = 0, Max = 120 };
            var tooSmall = new RTAGraphLimits { Min = 0, Max = 5 };
            var inverted = new RTAGraphLimits { Min = 120, Max = 0 };

            // Act & Assert
            validRange.IsValidRange().Should().BeTrue("120dB range is valid");
            tooSmall.IsValidRange().Should().BeFalse("5dB range is below 10dB minimum");
            inverted.IsValidRange().Should().BeFalse("max must be greater than min");
        }

        [Fact]
        public void RTAImageCaptureSettings_IsValidDimensions_ShouldCheckResolution()
        {
            // Arrange
            var valid1080p = new RTAImageCaptureSettings { Width = 1920, Height = 1080 };
            var valid4K = new RTAImageCaptureSettings { Width = 3840, Height = 2160 };
            var tooSmall = new RTAImageCaptureSettings { Width = 320, Height = 240 };
            var tooLarge = new RTAImageCaptureSettings { Width = 10000, Height = 10000 };

            // Act & Assert
            valid1080p.IsValidDimensions().Should().BeTrue("1080p is valid resolution");
            valid4K.IsValidDimensions().Should().BeTrue("4K is valid resolution");
            tooSmall.IsValidDimensions().Should().BeFalse("320x240 is below minimum");
            tooLarge.IsValidDimensions().Should().BeFalse("10000x10000 exceeds 8K maximum");
        }

        #endregion

        #region Status and Control Tests

        [Fact]
        public async Task GetStatus_ShouldReturnCurrentRTAStatus()
        {
            // Act
            var status = await _rtaService.GetStatus();

            // Assert
            status.Should().NotBeNull("status should be returned");
            _output.WriteLine($"✅ RTA Status: Enabled={status.Enabled}, Running={status.Running}");
        }

        [Fact]
        public async Task GetCommands_ShouldReturnAvailableCommands()
        {
            // Act
            var commands = await _rtaService.GetCommands();

            // Assert
            commands.Should().NotBeNull("commands list should be returned");
            commands.Should().Contain(c => c == "Start" || c == "Stop" || c == "Restart");
            _output.WriteLine($"✅ Available RTA commands: {string.Join(", ", commands)}");
        }

        [Fact]
        public async Task Reset_ShouldExecuteSuccessfully()
        {
            // Arrange - Ensure RTA is started before issuing Reset command
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running for Reset command");

            // Act & Assert (should not throw)
            await _rtaService.Reset();
            _output.WriteLine($"✅ RTA Reset command executed successfully");
        }

        [Fact]
        public async Task SetSPLOffset_ValidOffset_ShouldExecuteSuccessfully()
        {
            // Arrange - Ensure RTA is started before setting SPL offset
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running for SetSPLOffset command");

            double offsetDb = -3.5;

            // Act & Assert (should not throw)
            await _rtaService.SetSPLOffset(offsetDb);
            _output.WriteLine($"✅ RTA SPL Offset set to {offsetDb} dB");
        }

        [Fact]
        public async Task SaveGraphImage_ShouldExecuteSuccessfully()
        {
            // Arrange - Ensure RTA is started and has data before saving graph
            var rtaStarted = await _preconditions!.EnsureRTAStarted();
            rtaStarted.Should().BeTrue("RTA must be running to save graph");

            await Task.Delay(2000); // Wait for RTA to capture data (StateChange timing)

            // Act - Save graph image with explicit filename
            var testImagePath = Path.Combine(Path.GetTempPath(), "REWApi_Test_RTA_Graph.png");
            try
            {
                await _rtaService.SaveGraphImage(testImagePath);
                _output.WriteLine($"✅ RTA graph image saved to: {testImagePath}");
                
                // Verify file was created (optional - REW might save in different location)
                if (File.Exists(testImagePath))
                {
                    var fileInfo = new FileInfo(testImagePath);
                    _output.WriteLine($"   File size: {fileInfo.Length} bytes");
                }
            }
            catch (HttpRequestException ex)
            {
                _output.WriteLine($"❌ Failed to save graph image: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public async Task GetConfiguration_ShouldReturnRTASettings()
        {
            // Act
            var config = await _rtaService.GetConfiguration();

            // Assert
            config.Should().NotBeNull("configuration should be returned");
            config.Mode.Should().NotBeNullOrEmpty("mode should be set");
            config.FFTLength.Should().BeGreaterThan(0, "FFT length should be positive");
            _output.WriteLine($"✅ RTA Configuration: Mode={config.Mode}, FFT={config.FFTLength}, Averaging={config.Averaging}");
        }

        [Fact]
        public async Task GetAppearanceConfiguration_ShouldReturnDisplaySettings()
        {
            // Act
            var config = await _rtaService.GetAppearanceConfiguration();

            // Assert
            config.Should().NotBeNull("appearance configuration should be returned");
            _output.WriteLine($"✅ RTA Appearance: PeakHold={config.PeakHold}, ShowOverlays={config.ShowOverlays}");
        }

        [Fact]
        public async Task GetDistortionConfiguration_ShouldReturnDistortionSettings()
        {
            // Act
            var config = await _rtaService.GetDistortionConfiguration();

            // Assert
            config.Should().NotBeNull("distortion configuration should be returned");
            _output.WriteLine($"✅ RTA Distortion Config: Enabled={config.Enabled}, Harmonics={config.Harmonics}");
        }

        #endregion

        #region Graph Display Settings Tests

        [Fact]
        public async Task GetYAxisUnit_ShouldReturnCurrentUnit()
        {
            // Act
            var unit = await _rtaService.GetYAxisUnit();

            // Assert
            unit.Should().NotBeNullOrEmpty("Y-axis unit should be returned");
            _output.WriteLine($"✅ RTA Y-Axis Unit: {unit}");
        }

        [Fact]
        public async Task GetAvailableYAxisUnits_ShouldReturnUnitList()
        {
            // Act
            var units = await _rtaService.GetAvailableYAxisUnits();

            // Assert
            units.Should().NotBeNull("units list should be returned");
            units.Should().Contain(u => u.Contains("dB"));
            _output.WriteLine($"✅ Available Y-Axis Units: {string.Join(", ", units)}");
        }

        [Fact]
        public async Task GetGraphLimits_ShouldReturnCurrentLimits()
        {
            // Act
            var limits = await _rtaService.GetGraphLimits();

            // Assert
            limits.Should().NotBeNull("graph limits should be returned");
            limits.Max.Should().BeGreaterThan(limits.Min, "max should be greater than min");
            _output.WriteLine($"✅ RTA Graph Limits: Min={limits.Min}, Max={limits.Max}");
        }

        [Fact]
        public async Task GetCursorX_ShouldReturnFrequency()
        {
            // Act
            var frequency = await _rtaService.GetCursorX();

            // Assert
            frequency.Should().BeGreaterOrEqualTo(0, "cursor frequency should not be negative");
            _output.WriteLine($"✅ RTA Cursor X: {frequency} Hz");
        }

        [Fact]
        public async Task GetImageCaptureSettings_ShouldReturnSettings()
        {
            // Act
            var settings = await _rtaService.GetImageCaptureSettings();

            // Assert
            settings.Should().NotBeNull("image capture settings should be returned");
            settings.Width.Should().BeGreaterThan(0, "width should be positive");
            settings.Height.Should().BeGreaterThan(0, "height should be positive");
            _output.WriteLine($"✅ RTA Image Capture: {settings.Width}x{settings.Height} {settings.Format}");
        }

        #endregion

        #region Real-Time Data Access Tests

        [Fact]
        public async Task GetLevels_ShouldReturnInputLevels()
        {
            // Act
            var levels = await _rtaService.GetLevels("dBFS");

            // Assert
            levels.Should().NotBeNull("levels list should be returned");
            if (levels.Count > 0)
            {
                levels[0].Unit.Should().Be("dBFS", "unit should match request");
                _output.WriteLine($"✅ RTA Levels: {levels.Count} input(s), RMS={levels[0].RMS:F1} {levels[0].Unit}");
            }
            else
            {
                _output.WriteLine("⚠️ No RTA levels available (RTA may not be running)");
            }
        }

        [Fact]
        public async Task GetAvailableLevelUnits_ShouldReturnUnitList()
        {
            // Act
            var units = await _rtaService.GetAvailableLevelUnits();

            // Assert
            units.Should().NotBeNull("units list should be returned");
            units.Should().Contain("dBFS");
            _output.WriteLine($"✅ Available Level Units: {string.Join(", ", units)}");
        }

        [Fact]
        public async Task GetCapturedData_ShouldReturnFrequencyResponse()
        {
            // Act
            var data = await _rtaService.GetCapturedData("dBFS");

            // Assert
            data.Should().NotBeNull("captured data should be returned");
            _output.WriteLine($"✅ RTA Captured Data retrieved successfully");
        }

        [Fact]
        public async Task GetCapturedPeakData_ShouldReturnPeakResponse()
        {
            // Act
            var data = await _rtaService.GetCapturedPeakData("dBFS");

            // Assert
            data.Should().NotBeNull("captured peak data should be returned");
            _output.WriteLine($"✅ RTA Captured Peak Data retrieved successfully");
        }

        [Fact]
        public async Task GetAvailableCapturedDataUnits_ShouldReturnUnitList()
        {
            // Act
            var units = await _rtaService.GetAvailableCapturedDataUnits();

            // Assert
            units.Should().NotBeNull("units list should be returned");
            _output.WriteLine($"✅ Available Captured Data Units: {string.Join(", ", units)}");
        }

        [Fact]
        public async Task GetDistortion_ShouldReturnDistortionResults()
        {
            // Act
            var distortion = await _rtaService.GetDistortion("dBFS", "dB");

            // Assert
            distortion.Should().NotBeNull("distortion list should be returned");
            if (distortion.Count > 0)
            {
                distortion[0].LevelUnit.Should().Be("dBFS", "level unit should match request");
                distortion[0].DistortionUnit.Should().Be("dB", "distortion unit should match request");
                _output.WriteLine($"✅ RTA Distortion: THD={distortion[0].THD:F2} {distortion[0].DistortionUnit}, Fundamental={distortion[0].Fundamental:F1} Hz");
            }
            else
            {
                _output.WriteLine("⚠️ No distortion data available (RTA or distortion measurement may not be running)");
            }
        }

        [Fact]
        public async Task GetAvailableDistortionUnits_ShouldReturnUnitList()
        {
            // Act
            var units = await _rtaService.GetAvailableDistortionUnits();

            // Assert
            units.Should().NotBeNull("units list should be returned");
            _output.WriteLine($"✅ Available Distortion Units: {string.Join(", ", units)}");
        }

        [Fact]
        public async Task GetAvailableRelativeDistortionUnits_ShouldReturnUnitList()
        {
            // Act
            var units = await _rtaService.GetAvailableRelativeDistortionUnits();

            // Assert
            units.Should().NotBeNull("units list should be returned");
            units.Should().Contain("dB");
            _output.WriteLine($"✅ Available Relative Distortion Units: {string.Join(", ", units)}");
        }

        #endregion
    }
}
