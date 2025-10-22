using Xunit;
using REWApi;
using REWApi.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REWApi.Tests
{
    public class SteppedMeasurementServiceTests : IAsyncLifetime
    {
        private RewClient? _client;

        public async Task InitializeAsync()
        {
            // Initialize client - REW must be running with API enabled
            _client = await RewClient.ConnectAsync("http://127.0.0.1:4735");
        }

        public Task DisposeAsync()
        {
            _client?.Dispose();
            return Task.CompletedTask;
        }

        #region Commands Tests

        [Fact]
        public async Task GetCommands_ShouldReturnAvailableCommands()
        {
            // Act
            var commands = await _client!.SteppedMeasurement.GetCommands();

            // Assert
            Assert.NotNull(commands);
            Assert.NotEmpty(commands);
            
            // Verify expected commands
            Assert.Contains("start", commands, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendCommand_Start_ShouldExecuteSuccessfully()
        {
            // Arrange
            string command = "start";

            // Act
            var result = await _client!.SteppedMeasurement.SendCommand(command);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SendCommand_InvalidCommand_ShouldReturnFalse()
        {
            // Arrange
            string invalidCommand = "invalid_command_xyz";

            // Act
            var result = await _client!.SteppedMeasurement.SendCommand(invalidCommand);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Measurement Type Tests

        [Fact]
        public async Task GetTypes_ShouldReturnAvailableTypes()
        {
            // Act
            var types = await _client!.SteppedMeasurement.GetTypes();

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            
            // Verify expected types exist (actual values from API)
            Assert.Contains("THD vs frequency", types, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetType_ShouldReturnCurrentType()
        {
            // Act
            var currentType = await _client!.SteppedMeasurement.GetType();

            // Assert
            Assert.NotNull(currentType);
            Assert.NotEmpty(currentType);
        }

        [Fact]
        public async Task SetType_ValidType_ShouldSetSuccessfully()
        {
            // Arrange
            var availableTypes = await _client!.SteppedMeasurement.GetTypes();
            string typeToSet = availableTypes.First();

            // Act
            var result = await _client.SteppedMeasurement.SetType(typeToSet);

            // Assert
            Assert.True(result);
            
            // Verify type was set
            var currentType = await _client.SteppedMeasurement.GetType();
            Assert.Equal(typeToSet, currentType, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Frequency Span Tests

        [Fact]
        public async Task GetFrequencySpan_ShouldReturnConfiguration()
        {
            // Act
            var freqSpan = await _client!.SteppedMeasurement.GetFrequencySpan();

            // Assert
            Assert.NotNull(freqSpan);
            Assert.True(freqSpan.StartFreq > 0);
            Assert.True(freqSpan.EndFreq > freqSpan.StartFreq);
            Assert.True(freqSpan.Ppo > 0);
        }

        [Fact]
        public async Task SetFrequencySpan_ValidConfiguration_ShouldSetSuccessfully()
        {
            // Arrange
            var newFreqSpan = new SteppedFreqSpan
            {
                StartFreq = 20.0,
                EndFreq = 20000.0,
                Ppo = 12
            };

            // Act
            var result = await _client!.SteppedMeasurement.SetFrequencySpan(newFreqSpan);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFrequencySpan_ValidConfiguration_ShouldUpdateSuccessfully()
        {
            // Arrange
            var currentFreqSpan = await _client!.SteppedMeasurement.GetFrequencySpan();
            currentFreqSpan.Ppo = 24;

            // Act
            var result = await _client.SteppedMeasurement.UpdateFrequencySpan(currentFreqSpan);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetPpoValues_ShouldReturnAvailableValues()
        {
            // Act
            var ppoValues = await _client!.SteppedMeasurement.GetPpoValues();

            // Assert
            Assert.NotNull(ppoValues);
            Assert.NotEmpty(ppoValues);
            
            // Verify common PPO values
            Assert.Contains(12, ppoValues);
            Assert.Contains(24, ppoValues);
            Assert.Contains(48, ppoValues);
        }

        #endregion

        #region Level Span Tests

        [Fact]
        public async Task GetLevelSpan_ShouldReturnConfiguration()
        {
            // Act
            var levelSpan = await _client!.SteppedMeasurement.GetLevelSpan();

            // Assert
            Assert.NotNull(levelSpan);
            Assert.True(levelSpan.Step > 0);
        }

        [Fact]
        public async Task SetLevelSpan_ValidConfiguration_ShouldSetSuccessfully()
        {
            // Arrange
            var newLevelSpan = new SteppedLevelSpan
            {
                StartLevel = -30.0,
                EndLevel = -10.0,
                Step = 2.0
            };

            // Act
            var result = await _client!.SteppedMeasurement.SetLevelSpan(newLevelSpan);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateLevelSpan_ValidConfiguration_ShouldUpdateSuccessfully()
        {
            // Arrange
            var currentLevelSpan = await _client!.SteppedMeasurement.GetLevelSpan();
            currentLevelSpan.Step = 1.0;

            // Act
            var result = await _client.SteppedMeasurement.UpdateLevelSpan(currentLevelSpan);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region FFT Configuration Tests

        [Fact]
        public async Task GetFftConfiguration_ShouldReturnConfiguration()
        {
            // Act
            var fftConfig = await _client!.SteppedMeasurement.GetFftConfiguration();

            // Assert
            Assert.NotNull(fftConfig);
            Assert.NotNull(fftConfig.FftLength);
            Assert.True(fftConfig.Averages > 0);
            Assert.NotNull(fftConfig.MaximumOverlap);
            Assert.NotNull(fftConfig.Window);
        }

        [Fact]
        public async Task SetFftConfiguration_ValidConfiguration_ShouldSetSuccessfully()
        {
            // Arrange
            var newFftConfig = new SteppedFFTConfiguration
            {
                FftLength = "16k",
                Averages = 4,
                MaximumOverlap = "50%",
                Window = "Hann"
            };

            // Act
            var result = await _client!.SteppedMeasurement.SetFftConfiguration(newFftConfig);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateFftConfiguration_ValidConfiguration_ShouldUpdateSuccessfully()
        {
            // Arrange
            var currentFftConfig = await _client!.SteppedMeasurement.GetFftConfiguration();
            currentFftConfig.Averages = 8;

            // Act
            var result = await _client.SteppedMeasurement.UpdateFftConfiguration(currentFftConfig);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetFftLengths_ShouldReturnAvailableLengths()
        {
            // Act
            var fftLengths = await _client!.SteppedMeasurement.GetFftLengths();

            // Assert
            Assert.NotNull(fftLengths);
            Assert.NotEmpty(fftLengths);
            
            // Verify common FFT lengths
            Assert.Contains("16k", fftLengths, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("32k", fftLengths, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAveragingChoices_ShouldReturnAvailableChoices()
        {
            // Act
            var averagingChoices = await _client!.SteppedMeasurement.GetAveragingChoices();

            // Assert
            Assert.NotNull(averagingChoices);
            Assert.NotEmpty(averagingChoices);
            
            // Verify common averaging values (actual values from API)
            Assert.Contains(2, averagingChoices);
            Assert.Contains(4, averagingChoices);
            Assert.Contains(8, averagingChoices);
        }

        [Fact]
        public async Task GetOverlapChoices_ShouldReturnAvailableChoices()
        {
            // Act
            var overlapChoices = await _client!.SteppedMeasurement.GetOverlapChoices();

            // Assert
            Assert.NotNull(overlapChoices);
            Assert.NotEmpty(overlapChoices);
            
            // Verify common overlap values (actual values from API)
            Assert.Contains("0%", overlapChoices, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("50%", overlapChoices, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetWindowChoices_ShouldReturnAvailableWindows()
        {
            // Act
            var windowChoices = await _client!.SteppedMeasurement.GetWindowChoices();

            // Assert
            Assert.NotNull(windowChoices);
            Assert.NotEmpty(windowChoices);
            
            // Verify common window functions (actual values from API)
            Assert.Contains("Hann", windowChoices, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Blackman-Harris 4", windowChoices, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task GetOptions_ShouldReturnConfiguration()
        {
            // Act
            var options = await _client!.SteppedMeasurement.GetOptions();

            // Assert
            Assert.NotNull(options);
            Assert.True(options.SilenceIntervalSeconds >= 0);
        }

        [Fact]
        public async Task SetOptions_ValidConfiguration_ShouldSetSuccessfully()
        {
            // Arrange
            var newOptions = new SteppedOptions
            {
                SilenceIntervalSeconds = 1,
                CaptureSpectrum = true,
                StopForHeavyClipping = true,
                StopAtDistortionLimit = false,
                DistortionLimitPercent = 5.0,
                ReduceStepIfDistortionLimitHit = false
            };

            // Act
            var result = await _client!.SteppedMeasurement.SetOptions(newOptions);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateOptions_ValidConfiguration_ShouldUpdateSuccessfully()
        {
            // Arrange
            var currentOptions = await _client!.SteppedMeasurement.GetOptions();
            currentOptions.CaptureSpectrum = !currentOptions.CaptureSpectrum;

            // Act
            var result = await _client.SteppedMeasurement.UpdateOptions(currentOptions);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region IMD Stimuli Tests

        [Fact]
        public async Task GetImdStimuli_ShouldReturnAvailableStimuli()
        {
            // Act
            var imdStimuli = await _client!.SteppedMeasurement.GetImdStimuli();

            // Assert
            Assert.NotNull(imdStimuli);
            // IMD stimuli list may be empty if not configured, but should not be null
        }

        #endregion

        #region Progress Monitoring Tests

        [Fact]
        public async Task GetProgress_ShouldReturnCurrentProgress()
        {
            // Act
            var progress = await _client!.SteppedMeasurement.GetProgress();

            // Assert
            Assert.NotNull(progress);
            Assert.True(progress.Points >= 0);
            Assert.True(progress.Point >= 0);
            Assert.True(progress.TimeRemainingSeconds >= 0);
            Assert.NotNull(progress.Message);
        }

        [Fact]
        public async Task SubscribeToProgress_ValidUrl_ShouldSubscribeSuccessfully()
        {
            // Arrange
            string callbackUrl = "http://localhost:8080/progress-callback";

            // Act
            var result = await _client!.SteppedMeasurement.SubscribeToProgress(callbackUrl);

            // Assert
            Assert.True(result);
            
            // Cleanup
            await _client.SteppedMeasurement.UnsubscribeFromProgress(callbackUrl);
        }

        [Fact]
        public async Task UnsubscribeFromProgress_ExistingSubscription_ShouldUnsubscribeSuccessfully()
        {
            // Arrange
            string callbackUrl = "http://localhost:8080/progress-callback";
            await _client!.SteppedMeasurement.SubscribeToProgress(callbackUrl);

            // Act
            var result = await _client.SteppedMeasurement.UnsubscribeFromProgress(callbackUrl);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetProgressSubscribers_ShouldReturnSubscriberList()
        {
            // Act
            var subscribers = await _client!.SteppedMeasurement.GetProgressSubscribers();

            // Assert
            Assert.NotNull(subscribers);
            // May be empty if no active subscriptions
        }

        #endregion

        #region Results Subscription Tests

        [Fact]
        public async Task SubscribeToResults_ValidUrl_ShouldSubscribeSuccessfully()
        {
            // Arrange
            string callbackUrl = "http://localhost:8080/results-callback";

            // Act
            var result = await _client!.SteppedMeasurement.SubscribeToResults(callbackUrl);

            // Assert
            Assert.True(result);
            
            // Cleanup
            await _client.SteppedMeasurement.UnsubscribeFromResults(callbackUrl);
        }

        [Fact]
        public async Task UnsubscribeFromResults_ExistingSubscription_ShouldUnsubscribeSuccessfully()
        {
            // Arrange
            string callbackUrl = "http://localhost:8080/results-callback";
            await _client!.SteppedMeasurement.SubscribeToResults(callbackUrl);

            // Act
            var result = await _client.SteppedMeasurement.UnsubscribeFromResults(callbackUrl);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetResultsSubscribers_ShouldReturnSubscriberList()
        {
            // Act
            var subscribers = await _client!.SteppedMeasurement.GetResultsSubscribers();

            // Assert
            Assert.NotNull(subscribers);
            // May be empty if no active subscriptions
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task SteppedMeasurement_CompleteWorkflow_ShouldExecuteSuccessfully()
        {
            // Arrange
            var freqSpan = new SteppedFreqSpan
            {
                StartFreq = 100.0,
                EndFreq = 10000.0,
                Ppo = 12
            };
            
            var fftConfig = new SteppedFFTConfiguration
            {
                FftLength = "16k",
                Averages = 4,
                MaximumOverlap = "50%",
                Window = "Hann"
            };

            // Act - Configure measurement
            await _client!.SteppedMeasurement.SetFrequencySpan(freqSpan);
            await _client.SteppedMeasurement.SetFftConfiguration(fftConfig);
            
            // Verify configuration
            var currentFreqSpan = await _client.SteppedMeasurement.GetFrequencySpan();
            var currentFftConfig = await _client.SteppedMeasurement.GetFftConfiguration();

            // Assert
            Assert.Equal(freqSpan.StartFreq, currentFreqSpan.StartFreq);
            Assert.Equal(freqSpan.EndFreq, currentFreqSpan.EndFreq);
            Assert.Equal(freqSpan.Ppo, currentFreqSpan.Ppo);
            Assert.Equal(fftConfig.FftLength, currentFftConfig.FftLength, StringComparer.OrdinalIgnoreCase);
        }

        #endregion
    }
}
