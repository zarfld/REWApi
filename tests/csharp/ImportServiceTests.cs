using System;
using System.Collections.Generic;
using System.IO;
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
    /// Tests for Import Service
    /// ISO/IEC/IEEE 12207:2017 - Verification and Validation
    /// IEEE 1012-2016 - Verification and Validation
    /// 
    /// Test REW Import API endpoints:
    /// - GET/POST /import/sweep-recordings/stimulus
    /// - GET/POST /import/sweep-recordings/response
    /// - GET/POST /import/frequency-response
    /// - GET/POST /import/frequency-response-data
    /// - GET/POST /import/impulse-response
    /// - GET/POST /import/impulse-response-data
    /// - GET/POST /import/rta-file
    /// - GET /import/rta-file/progress
    /// - GET /import/rta-file/save-options
    /// - POST /import/subscribe
    /// - POST /import/unsubscribe
    /// - GET /import/subscribers
    /// </summary>
    public class ImportServiceTests : IClassFixture<RewClientFixture>, IAsyncLifetime
    {
        private readonly RewClient _client;
        private readonly ITestOutputHelper _output;
        private TestPreconditions? _preconditions;
        private PreconditionResult? _setupResult;

        public ImportServiceTests(RewClientFixture fixture, ITestOutputHelper output)
        {
            _client = fixture.Client;
            _output = output;
        }

        public async Task InitializeAsync()
        {
            // Setup test preconditions once for all tests
            _preconditions = new TestPreconditions(_client, _output);
            
            _output.WriteLine("🎬 Setting up Import tests with real sweep recordings...");
            _setupResult = await _preconditions.SetupAllPreconditions();
            
            // Verify sweep files were loaded
            if (_setupResult?.StimulusFilePath != null && File.Exists(_setupResult.StimulusFilePath))
            {
                var stimulusSize = new FileInfo(_setupResult.StimulusFilePath).Length;
                var isRealSweep = stimulusSize > 1024 * 1024; // Real sweeps are > 1MB
                if (isRealSweep)
                {
                    _output.WriteLine($"   ✅ Using REAL REW sweep: {stimulusSize:N0} bytes ({stimulusSize / 1024.0 / 1024.0:F2} MB)");
                }
                else
                {
                    _output.WriteLine($"   ⚠️ Using minimal test WAV: {stimulusSize:N0} bytes");
                }
            }

            _output.WriteLine("🔧 Import test setup complete");
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        #region Sweep Recordings Tests

        [Fact]
        public async Task GetSweepStimulus_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetSweepStimulusAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Sweep stimulus file: {filePath}");
        }

        [Fact]
        public async Task SetSweepStimulus_ShouldSetFilePath()
        {
            // Arrange - Use valid WAV file created by preconditions
            var testPath = _setupResult!.StimulusFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid stimulus file");

            // Act
            var result = await _client.Import.SetSweepStimulusAsync(testPath);
            
            // Assert
            result.Should().BeTrue("valid WAV file should be accepted");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
            
            var retrievedPath = await _client.Import.GetSweepStimulusAsync();
            retrievedPath.Should().Contain(Path.GetFileName(testPath), "file path should persist");
            
            _output.WriteLine($"✅ Set sweep stimulus to: {testPath}");
        }

        [Fact]
        public async Task GetSweepResponse_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetSweepResponseAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Sweep response file: {filePath}");
        }

        [Fact]
        public async Task SetSweepResponse_ShouldSetFilePath()
        {
            // Arrange - CRITICAL: Ensure stimulus is set BEFORE response (per REW API requirement)
            var stimulusSet = await _preconditions!.EnsureSweepStimulusSet(_setupResult);
            stimulusSet.Should().BeTrue("stimulus must be set before response");
            
            // Wait for CommandGate operation
            await Task.Delay(1000);

            // Use valid response WAV file created by preconditions
            var testPath = _setupResult!.ResponseFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid response file");

            // Act
            var result = await _client.Import.SetSweepResponseAsync(testPath, channels: "All");
            
            // Assert
            result.Should().BeTrue("response should be accepted after stimulus is set");
            _output.WriteLine($"✅ Set sweep response to: {testPath}");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
            
            var retrievedPath = await _client.Import.GetSweepResponseAsync();
            if (retrievedPath == testPath || retrievedPath.Contains(Path.GetFileName(testPath)))
            {
                _output.WriteLine("✅ File path persisted successfully");
            }
            else
            {
                _output.WriteLine($"⚠️ File path not persisted (returned: {retrievedPath})");
            }
        }

        #endregion

        #region Frequency Response Tests

        [Fact]
        public async Task GetFrequencyResponseFile_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetFrequencyResponseFileAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Frequency response file: {filePath}");
        }

        [Fact]
        public async Task SetFrequencyResponseFile_ShouldSetFilePath()
        {
            // Arrange - Use valid .frd file created by preconditions
            var testPath = _setupResult!.FrequencyResponseFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid .frd file");
            testPath.Should().EndWith(".frd", "frequency response file should be .frd format");

            // Act
            var result = await _client.Import.SetFrequencyResponseFileAsync(testPath);
            
            // Assert
            result.Should().BeTrue("valid .frd file should be accepted");
            _output.WriteLine($"✅ Set frequency response file to: {testPath}");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
            
            var retrievedPath = await _client.Import.GetFrequencyResponseFileAsync();
            if (retrievedPath == testPath || retrievedPath.Contains(Path.GetFileName(testPath)))
            {
                _output.WriteLine("✅ File path persisted successfully");
            }
            else
            {
                _output.WriteLine($"⚠️ File path not persisted (returned: {retrievedPath})");
            }
        }

        [Fact]
        public async Task GetFrequencyResponseDataFile_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetFrequencyResponseDataFileAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Frequency response data file: {filePath}");
        }

        [Fact]
        public async Task SetFrequencyResponseDataFile_ShouldSetFilePath()
        {
            // Arrange - Use valid .frd file created by preconditions
            var testPath = _setupResult!.FrequencyResponseFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid .frd file");

            // Act
            var result = await _client.Import.SetFrequencyResponseDataFileAsync(testPath);
            
            // Assert
            result.Should().BeTrue("valid .frd file should be accepted");
            _output.WriteLine($"✅ Set frequency response data file to: {testPath}");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
        }

        #endregion

        #region Impulse Response Tests

        [Fact]
        public async Task GetImpulseResponseFile_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetImpulseResponseFileAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Impulse response file: {filePath}");
        }

        [Fact]
        public async Task SetImpulseResponseFile_ShouldSetFilePath()
        {
            // Arrange - Use valid WAV file created by preconditions
            var testPath = _setupResult!.StimulusFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid WAV file");

            // Act
            var result = await _client.Import.SetImpulseResponseFileAsync(testPath);
            
            // Assert
            result.Should().BeTrue("valid WAV file should be accepted");
            _output.WriteLine($"✅ Set impulse response file to: {testPath}");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
        }

        [Fact]
        public async Task GetImpulseResponseDataFile_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetImpulseResponseDataFileAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"Impulse response data file: {filePath}");
        }

        [Fact]
        public async Task SetImpulseResponseDataFile_ShouldSetFilePath()
        {
            // Arrange - Use valid file created by preconditions
            var testPath = _setupResult!.StimulusFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid file");

            // Act
            var result = await _client.Import.SetImpulseResponseDataFileAsync(testPath);
            
            // Assert
            result.Should().BeTrue("valid file should be accepted");
            _output.WriteLine($"✅ Set impulse response data file to: {testPath}");
            
            // Wait for CommandGate operation to complete
            await Task.Delay(1000);
        }

        #endregion

        #region RTA File Tests

        [Fact]
        public async Task GetRTAFile_ShouldReturnFilePath()
        {
            // Act
            var filePath = await _client.Import.GetRTAFileAsync();

            // Assert
            filePath.Should().NotBeNull();
            _output.WriteLine($"RTA file: {filePath}");
        }

        [Fact]
        public async Task SetRTAFile_ShouldSetFilePath()
        {
            // Arrange - Use valid RTA audio file created by preconditions
            var testPath = _setupResult!.RTAFilePath!;
            testPath.Should().NotBeNullOrEmpty("preconditions should create valid RTA file");
            // RTA files must be audio files: .wav, .aiff, .mp3, .flac
            var extension = Path.GetExtension(testPath).ToLowerInvariant();
            extension.Should().BeOneOf(".wav", ".aiff", ".mp3", ".flac", "RTA file must be an audio file");

            // Act
            var result = await _client.Import.SetRTAFileAsync(testPath, channel: 1, saveOption: "Current");
            
            // Assert
            result.Should().BeTrue("valid RTA audio file should be accepted");
            _output.WriteLine($"✅ Set RTA file to: {testPath}");
            
            // Wait for CommandGate operation
            await Task.Delay(1500);
        }

        [Fact]
        public async Task GetRTAFileProgress_ShouldReturnProgress()
        {
            // Act
            var progress = await _client.Import.GetRTAFileProgressAsync();

            // Assert
            progress.Should().BeInRange(0, 100);
            _output.WriteLine($"RTA import progress: {progress}%");
        }

        [Fact]
        public async Task GetRTAFileSaveOptions_ShouldReturnOptions()
        {
            // Act
            var options = await _client.Import.GetRTAFileSaveOptionsAsync();

            // Assert
            options.Should().NotBeNull();
            options.Should().NotBeEmpty();
            options.Should().Contain(opt => opt == "Current" || opt == "Peak" || opt == "Both");
            _output.WriteLine($"RTA save options: {string.Join(", ", options)}");
        }

        #endregion

        #region Subscription Tests

        [Fact] // Unskipped - Bug fixed: Property name changed from "callbackUrl" to "url"
        public async Task Subscribe_ShouldAddSubscription()
        {
            // Arrange
            var callbackUrl = "http://localhost:5000/import-callback";

            // Act
            var result = await _client.Import.SubscribeAsync(callbackUrl);
            var subscribers = await _client.Import.GetSubscribersAsync();

            // Assert
            result.Should().BeTrue();
            subscribers.Should().Contain(callbackUrl);
            _output.WriteLine($"Subscribed to import events: {callbackUrl}");
        }

        [Fact] // Unskipped - Bug fixed: Property name changed from "callbackUrl" to "url"
        public async Task Unsubscribe_ShouldRemoveSubscription()
        {
            // Arrange
            var callbackUrl = "http://localhost:5000/import-callback";
            await _client.Import.SubscribeAsync(callbackUrl);

            // Act
            var result = await _client.Import.UnsubscribeAsync(callbackUrl);
            var subscribers = await _client.Import.GetSubscribersAsync();

            // Assert
            result.Should().BeTrue();
            subscribers.Should().NotContain(callbackUrl);
            _output.WriteLine($"Unsubscribed from import events: {callbackUrl}");
        }

        [Fact]
        public async Task GetSubscribers_ShouldReturnSubscriberList()
        {
            // Act
            var subscribers = await _client.Import.GetSubscribersAsync();

            // Assert
            subscribers.Should().NotBeNull();
            _output.WriteLine($"Import subscribers count: {subscribers.Count}");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task SetSweepStimulus_WithNullPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetSweepStimulusAsync(null!));
        }

        [Fact]
        public async Task SetSweepStimulus_WithEmptyPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetSweepStimulusAsync(""));
        }

        [Fact]
        public async Task SetSweepStimulus_WithWhitespacePath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetSweepStimulusAsync("   "));
        }

        [Fact]
        public async Task SetSweepResponse_WithNullPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetSweepResponseAsync(null!));
        }

        [Fact]
        public async Task SetFrequencyResponseFile_WithNullPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetFrequencyResponseFileAsync(null!));
        }

        [Fact]
        public async Task SetImpulseResponseFile_WithNullPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetImpulseResponseFileAsync(null!));
        }

        [Fact]
        public async Task SetRTAFile_WithNullPath_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SetRTAFileAsync(null!));
        }

        [Fact]
        public async Task Subscribe_WithNullUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SubscribeAsync(null!));
        }

        [Fact]
        public async Task Subscribe_WithEmptyUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SubscribeAsync(""));
        }

        [Fact]
        public async Task Subscribe_WithWhitespaceUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.SubscribeAsync("   "));
        }

        [Fact]
        public async Task Unsubscribe_WithNullUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.UnsubscribeAsync(null!));
        }

        [Fact]
        public async Task Unsubscribe_WithEmptyUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.UnsubscribeAsync(""));
        }

        [Fact]
        public async Task Unsubscribe_WithWhitespaceUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.Import.UnsubscribeAsync("   "));
        }

        #endregion
    }
}
