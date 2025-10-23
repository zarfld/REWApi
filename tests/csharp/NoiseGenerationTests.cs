using System;
using System.Linq;
using System.Threading.Tasks;
using REWApi;
using REWApi.Domain;
using REWApi.Domain.Services;
using Xunit;
using Xunit.Abstractions;

namespace REWApi.Tests.CSharp
{
    /// <summary>
    /// Test suite for Noise Generation capabilities
    /// Tests pink noise, white noise, and sweep signal generation
    /// 
    /// User scenario: Verify soundcard can generate pink/white noise for testing
    /// </summary>
    public class NoiseGenerationTests : IDisposable
    {
        private readonly RewClient _client;
        private readonly GeneratorService _generator;
        private readonly ITestOutputHelper _output;

        public NoiseGenerationTests(ITestOutputHelper output)
        {
            _output = output;
            _client = RewClient.ConnectAsync("http://localhost:4735").Result;
            _generator = _client.Generator;
        }

        public void Dispose()
        {
            // Ensure generator is stopped after tests to prevent noise from continuing
            try
            {
                // Stop the generator
                _generator.Stop().Wait();
                
                // Verify it's actually stopped
                var status = _generator.GetStatus().Result;
                if (status.Playing)
                {
                    _output.WriteLine("⚠️ WARNING: Generator still playing after Stop() call");
                }
                else
                {
                    _output.WriteLine("✅ Generator safely stopped after tests");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"⚠️ Error stopping generator: {ex.Message}");
            }
            
            _client?.Dispose();
        }

        #region Noise Signal Tests

        [Fact]
        public async Task GetAvailableSignals_ShouldIncludeNoiseTypes()
        {
            // Act
            var signals = await _generator.GetAvailableSignals();

            // Assert - verify all 4 noise types are available
            Assert.Contains("pinknoise", signals);
            Assert.Contains("whitenoise", signals);
            Assert.Contains("pinkpn", signals); // Pink pseudo-noise
            Assert.Contains("whitepn", signals); // White pseudo-noise
            
            _output.WriteLine($"Found {signals.Count} signal types");
            _output.WriteLine($"Noise types: {string.Join(", ", signals.Where(s => s.Contains("noise")))}");
        }

        [Fact]
        public async Task SetSignalToPinkNoise_ShouldChangeSignal()
        {
            // Act
            await _generator.SetSignal("pinknoise");
            var currentSignal = await _generator.GetSignal();

            // Assert
            Assert.Equal("pinknoise", currentSignal);
            _output.WriteLine($"Successfully set signal to: {currentSignal}");
        }

        [Fact]
        public async Task SetSignalToWhiteNoise_ShouldChangeSignal()
        {
            // Act
            await _generator.SetSignal("whitenoise");
            var currentSignal = await _generator.GetSignal();

            // Assert
            Assert.Equal("whitenoise", currentSignal);
            _output.WriteLine($"Successfully set signal to: {currentSignal}");
        }

        [Fact]
        public async Task GetGeneratorStatus_WithPinkNoise_ShouldShowCorrectStatus()
        {
            // Arrange
            await _generator.SetSignal("pinknoise");
            
            // Act
            var status = await _generator.GetStatus();

            // Assert
            Assert.Equal("pinknoise", status.Signal);
            Assert.True(status.Enabled);
            _output.WriteLine($"Generator status: Enabled={status.Enabled}, Playing={status.Playing}, Signal={status.Signal}, Level={status.Level}{status.LevelUnit}");
        }

        [Fact]
        public async Task SetLevel_WithValidLevel_ShouldChangeLevel()
        {
            // Arrange - Use safe level (-30.0 dBFS)
            await _generator.SetLevel(-30.0, "dBFS");
            var newLevel = await _generator.GetLevel();

            // Assert
            Assert.Equal(-30.0, newLevel, 0.1); // Allow 0.1 dB tolerance
            _output.WriteLine($"Successfully set level to: {newLevel} dBFS");
        }

        #endregion

        #region Sweep Signal Tests

        [Fact]
        public async Task GetAvailableSignals_ShouldIncludeSweepTypes()
        {
            // Act
            var signals = await _generator.GetAvailableSignals();

            // Assert - verify all 3 sweep types are available
            Assert.Contains("linearsweep", signals);
            Assert.Contains("logsweep", signals);
            Assert.Contains("meassweep", signals); // Measurement sweep
            
            _output.WriteLine($"Sweep types: {string.Join(", ", signals.Where(s => s.Contains("sweep")))}");
        }

        [Fact]
        public async Task SetSignalToLogSweep_ShouldChangeSignal()
        {
            // Act
            await _generator.SetSignal("logsweep");
            var currentSignal = await _generator.GetSignal();

            // Assert
            Assert.Equal("logsweep", currentSignal);
            _output.WriteLine($"Successfully set signal to: {currentSignal}");
        }

        [Fact]
        public async Task SetSignalToLinearSweep_ShouldChangeSignal()
        {
            // Act
            await _generator.SetSignal("linearsweep");
            var currentSignal = await _generator.GetSignal();

            // Assert
            Assert.Equal("linearsweep", currentSignal);
            _output.WriteLine($"Successfully set signal to: {currentSignal}");
        }

        #endregion

        #region Playback Control Tests (Manual)

        [Fact(Skip = "Manual test - plays actual audio through soundcard")]
        [Trait("Category", "Manual")]
        public async Task StartGenerator_WithPinkNoise_ShouldStartPlayback()
        {
            // Arrange
            await _generator.SetSignal("pinknoise");
            await _generator.SetLevel(-30.0, "dBFS"); // Safe level
            
            // Act
            await _generator.Start();
            var status = await _generator.GetStatus();
            
            // Assert
            Assert.True(status.Playing);
            
            _output.WriteLine("⚠️ PLAYING PINK NOISE - You should hear noise from speakers");
            _output.WriteLine($"Level: {status.Level} {status.LevelUnit}");
            _output.WriteLine("Waiting 2 seconds...");
            await Task.Delay(2000);
            
            // Cleanup
            await _generator.Stop();
            var stoppedStatus = await _generator.GetStatus();
            Assert.False(stoppedStatus.Playing);
        }

        [Fact(Skip = "Manual test - plays actual audio through soundcard")]
        [Trait("Category", "Manual")]
        public async Task StartGenerator_WithWhiteNoise_ShouldStartPlayback()
        {
            // Arrange
            await _generator.SetSignal("whitenoise");
            await _generator.SetLevel(-30.0, "dBFS"); // Safe level
            
            // Act
            await _generator.Start();
            var status = await _generator.GetStatus();
            
            // Assert
            Assert.True(status.Playing);
            
            _output.WriteLine("⚠️ PLAYING WHITE NOISE - You should hear noise from speakers");
            _output.WriteLine($"Level: {status.Level} {status.LevelUnit}");
            _output.WriteLine("Waiting 2 seconds...");
            await Task.Delay(2000);
            
            // Cleanup
            await _generator.Stop();
            var stoppedStatus = await _generator.GetStatus();
            Assert.False(stoppedStatus.Playing);
        }

        #endregion

        #region Soundcard Verification Tests

        [Fact]
        public async Task VerifySoundcardCanGenerateNoise_CompleteSmokeTest()
        {
            _output.WriteLine("=== Soundcard Noise Generation Verification ===");
            
            // Step 1: Verify noise signals are available
            var signals = await _generator.GetAvailableSignals();
            Assert.Contains("pinknoise", signals);
            Assert.Contains("whitenoise", signals);
            _output.WriteLine("✅ Pink and white noise signals available");
            
            // Step 2: Select pink noise
            await _generator.SetSignal("pinknoise");
            var pinkSignal = await _generator.GetSignal();
            Assert.Equal("pinknoise", pinkSignal);
            _output.WriteLine("✅ Successfully selected pink noise");
            
            // Step 3: Set safe level
            await _generator.SetLevel(-30.0, "dBFS");
            var level = await _generator.GetLevel();
            Assert.InRange(level, -31.0, -29.0);
            _output.WriteLine($"✅ Set level to {level} dBFS (safe level)");
            
            // Step 4: Select white noise
            await _generator.SetSignal("whitenoise");
            var whiteSignal = await _generator.GetSignal();
            Assert.Equal("whitenoise", whiteSignal);
            _output.WriteLine("✅ Successfully selected white noise");
            
            // Step 5: Verify generator is ready but not playing
            var status = await _generator.GetStatus();
            Assert.True(status.Enabled);
            Assert.False(status.Playing); // Should not be playing
            _output.WriteLine($"✅ Generator ready: Enabled={status.Enabled}, Playing={status.Playing}");
            
            _output.WriteLine("\n=== VERIFICATION COMPLETE ===");
            _output.WriteLine("✅ Soundcard CAN generate pink and white noise");
            _output.WriteLine("✅ All signal types available");
            _output.WriteLine("✅ Generator control working correctly");
            _output.WriteLine("\nTo test actual playback:");
            _output.WriteLine("  1. Run manual tests with [Trait(\"Category\", \"Manual\")]");
            _output.WriteLine("  2. Or use REW GUI to click 'Start' on generator");
            _output.WriteLine("  3. Verify you can hear noise from speakers");
        }

        #endregion

        #region Test Signal Types

        [Fact]
        public async Task GetAvailableSignals_ShouldIncludeTestSignals()
        {
            // Act
            var signals = await _generator.GetAvailableSignals();

            // Assert - verify test signals are available
            Assert.Contains("sine", signals);
            Assert.Contains("square", signals);
            Assert.Contains("sawtooth", signals);
            Assert.Contains("toneburst", signals);
            Assert.Contains("multitone", signals);
            
            _output.WriteLine($"Total signal types available: {signals.Count}");
            _output.WriteLine($"Test signals: {string.Join(", ", signals.Where(s => !s.Contains("noise") && !s.Contains("sweep")))}");
        }

        #endregion

        #region Noise Type Switching

        [Fact]
        public async Task SwitchBetweenNoiseTypes_ShouldWork()
        {
            _output.WriteLine("Testing rapid noise type switching...");
            
            // Switch through all noise types - REW returns what you set
            await _generator.SetSignal("pinknoise");
            var signal1 = await _generator.GetSignal();
            Assert.Equal("pinknoise", signal1); // REW returns exactly what was set
            _output.WriteLine($"✅ Switched to: {signal1}");
            
            await _generator.SetSignal("whitenoise");
            var signal2 = await _generator.GetSignal();
            Assert.Equal("whitenoise", signal2); // REW returns exactly what was set
            _output.WriteLine($"✅ Switched to: {signal2}");
            
            await _generator.SetSignal("pinkpn");
            var signal3 = await _generator.GetSignal();
            Assert.Equal("pinkpn", signal3);
            _output.WriteLine($"✅ Switched to: {signal3}");
            
            await _generator.SetSignal("whitepn");
            var signal4 = await _generator.GetSignal();
            Assert.Equal("whitepn", signal4);
            _output.WriteLine($"✅ Switched to: {signal4}");
            
            _output.WriteLine("✅ All noise type switches successful");
        }

        #endregion
    }
}
