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
    public class MeasureServiceTests : IClassFixture<RewClientFixture>
    {
        private readonly RewClient _client;
        private readonly ITestOutputHelper _output;

        public MeasureServiceTests(RewClientFixture fixture, ITestOutputHelper output)
        {
            _client = fixture.Client;
            _output = output;
        }

        #region Domain Object Tests

        [Fact]
        public void MeasureLevel_IsSafeLevel_ShouldValidateRange()
        {
            // Arrange & Act & Assert - dBFS safe range
            new MeasureLevel { Level = -12.0, Unit = "dBFS" }.IsSafeLevel().Should().BeTrue();
            new MeasureLevel { Level = 0.0, Unit = "dBFS" }.IsSafeLevel().Should().BeTrue();
            new MeasureLevel { Level = 5.0, Unit = "dBFS" }.IsSafeLevel().Should().BeFalse();
            new MeasureLevel { Level = -100.0, Unit = "dBFS" }.IsSafeLevel().Should().BeFalse();

            // dBu safe range
            new MeasureLevel { Level = 0.0, Unit = "dBu" }.IsSafeLevel().Should().BeTrue();
            new MeasureLevel { Level = 25.0, Unit = "dBu" }.IsSafeLevel().Should().BeFalse();

            // SPL safe range
            new MeasureLevel { Level = 85.0, Unit = "SPL" }.IsSafeLevel().Should().BeTrue();
            new MeasureLevel { Level = 130.0, Unit = "SPL" }.IsSafeLevel().Should().BeFalse();
        }

        [Fact]
        public void MeasureProtectionOptions_IsValidConfiguration_ShouldCheckLimits()
        {
            // Arrange & Act & Assert
            new MeasureProtectionOptions 
            { 
                ClippingThreshold = 0.5, 
                SPLLimit = 100.0 
            }.IsValidConfiguration().Should().BeTrue();

            new MeasureProtectionOptions 
            { 
                ClippingThreshold = 150.0, // Invalid
                SPLLimit = 100.0 
            }.IsValidConfiguration().Should().BeFalse();

            new MeasureProtectionOptions 
            { 
                ClippingThreshold = 0.5, 
                SPLLimit = 200.0 // Invalid
            }.IsValidConfiguration().Should().BeFalse();
        }

        [Fact]
        public void MeasureProtectionOptions_HasActiveProtection_ShouldCheckFlags()
        {
            // Arrange & Act & Assert
            new MeasureProtectionOptions 
            { 
                AbortOnClipping = false, 
                AbortOnSPLExceeded = false 
            }.HasActiveProtection().Should().BeFalse();

            new MeasureProtectionOptions 
            { 
                AbortOnClipping = true, 
                AbortOnSPLExceeded = false 
            }.HasActiveProtection().Should().BeTrue();

            new MeasureProtectionOptions 
            { 
                AbortOnClipping = false, 
                AbortOnSPLExceeded = true 
            }.HasActiveProtection().Should().BeTrue();
        }

        [Fact]
        public void SweepConfiguration_IsValidFrequencyRange_ShouldCheckBounds()
        {
            // Arrange & Act & Assert
            new SweepConfiguration 
            { 
                StartFreq = 20.0, 
                EndFreq = 20000.0 
            }.IsValidFrequencyRange().Should().BeTrue();

            new SweepConfiguration 
            { 
                StartFreq = 0.0, // Invalid
                EndFreq = 20000.0 
            }.IsValidFrequencyRange().Should().BeFalse();

            new SweepConfiguration 
            { 
                StartFreq = 15000.0, 
                EndFreq = 10000.0 // Invalid (End < Start)
            }.IsValidFrequencyRange().Should().BeFalse();
        }

        [Fact]
        public void SweepConfiguration_IsValidLength_ShouldCheckPowerOf2Lengths()
        {
            // Arrange & Act & Assert
            new SweepConfiguration { Length = "256k" }.IsValidLength().Should().BeTrue();
            new SweepConfiguration { Length = "512k" }.IsValidLength().Should().BeTrue();
            new SweepConfiguration { Length = "1024k" }.IsValidLength().Should().BeTrue();
            new SweepConfiguration { Length = "300k" }.IsValidLength().Should().BeFalse();
        }

        [Fact]
        public void SweepConfiguration_GetFrequencySpanOctaves_ShouldCalculateCorrectly()
        {
            // Arrange
            var config = new SweepConfiguration 
            { 
                StartFreq = 20.0, 
                EndFreq = 20000.0 
            };

            // Act
            var octaves = config.GetFrequencySpanOctaves();

            // Assert
            octaves.Should().BeApproximately(Math.Log2(1000), 0.01); // 20-20000 Hz = 10^3 ratio
            _output.WriteLine($"Frequency span: {octaves:F2} octaves");
        }

        [Fact]
        public void MeasurementMode_IsValidMode_ShouldCheckModes()
        {
            // Arrange & Act & Assert
            new MeasurementMode { Mode = "Single" }.IsValidMode().Should().BeTrue();
            new MeasurementMode { Mode = "Repeated" }.IsValidMode().Should().BeTrue();
            new MeasurementMode { Mode = "Ramped" }.IsValidMode().Should().BeTrue();
            new MeasurementMode { Mode = "Sequential" }.IsValidMode().Should().BeTrue();
            new MeasurementMode { Mode = "InvalidMode" }.IsValidMode().Should().BeFalse();
        }

        [Fact]
        public void MeasurementMode_IsMultiMeasurementMode_ShouldIdentifyMultiModes()
        {
            // Arrange & Act & Assert
            new MeasurementMode { Mode = "Single" }.IsMultiMeasurementMode().Should().BeFalse();
            new MeasurementMode { Mode = "Repeated" }.IsMultiMeasurementMode().Should().BeTrue();
            new MeasurementMode { Mode = "Ramped" }.IsMultiMeasurementMode().Should().BeTrue();
            new MeasurementMode { Mode = "Sequential" }.IsMultiMeasurementMode().Should().BeTrue();
        }

        #endregion

        #region Commands Tests

        [Fact]
        public async Task GetCommands_ShouldReturnCommandList()
        {
            // Act
            var commands = await _client.Measure.GetCommands();

            // Assert
            commands.Should().NotBeNull();
            commands.Should().NotBeEmpty();
            commands.Should().Contain(c => c.Contains("SPL") || c.Contains("measurement"));
            
            _output.WriteLine($"Found {commands.Count} measure commands:");
            foreach (var command in commands)
            {
                _output.WriteLine($"  - {command}");
            }
        }

        #endregion

        #region Naming Configuration Tests

        [Fact]
        public async Task GetNaming_ShouldReturnCurrentConfiguration()
        {
            // Act
            var naming = await _client.Measure.GetNaming();

            // Assert
            naming.Should().NotBeNull();
            naming.Option.Should().NotBeNullOrEmpty();
            naming.SequenceNumber.Should().BeGreaterOrEqualTo(1);
            
            _output.WriteLine($"Naming option: {naming.Option}");
            _output.WriteLine($"Sequence number: {naming.SequenceNumber}");
            _output.WriteLine($"Next name: {naming.NextName}");
        }

        [Fact]
        public async Task GetNamingOptions_ShouldReturnAvailableOptions()
        {
            // Act
            var options = await _client.Measure.GetNamingOptions();

            // Assert
            options.Should().NotBeNull();
            options.Should().NotBeEmpty();
            
            _output.WriteLine($"Found {options.Count} naming options:");
            foreach (var option in options)
            {
                _output.WriteLine($"  - {option}");
            }
        }

        [Fact]
        public async Task GetDateTimeFormats_ShouldReturnFormatList()
        {
            // Act
            var formats = await _client.Measure.GetDateTimeFormats();

            // Assert
            formats.Should().NotBeNull();
            // May be empty if no formats configured
            
            _output.WriteLine($"Found {formats.Count} date/time formats:");
            foreach (var format in formats)
            {
                _output.WriteLine($"  - {format}");
            }
        }

        #endregion

        #region Notes Tests

        [Fact]
        public async Task GetNotes_ShouldReturnCurrentNotes()
        {
            // Act
            var notes = await _client.Measure.GetNotes();

            // Assert
            notes.Should().NotBeNull();
            // Notes may be empty
            
            _output.WriteLine($"Current notes: '{notes.Notes}'");
        }

        #endregion

        #region Level Configuration Tests

        [Fact]
        public async Task GetLevel_ShouldReturnCurrentLevel()
        {
            // Act
            var level = await _client.Measure.GetLevel();

            // Assert
            level.Should().NotBeNull();
            level.Unit.Should().NotBeNullOrEmpty();
            level.Level.Should().BeInRange(-96.0, 20.0); // Reasonable range
            
            _output.WriteLine($"Measurement level: {level.Level:F1} {level.Unit}");
        }

        [Fact]
        public async Task GetLevelUnits_ShouldReturnAvailableUnits()
        {
            // Act
            var units = await _client.Measure.GetLevelUnits();

            // Assert
            units.Should().NotBeNull();
            units.Should().NotBeEmpty();
            units.Should().Contain(u => u == "dBFS" || u == "dBu" || u == "dBV" || u == "SPL");
            
            _output.WriteLine($"Found {units.Count} level units:");
            foreach (var unit in units)
            {
                _output.WriteLine($"  - {unit}");
            }
        }

        #endregion

        #region Protection Options Tests

        [Fact]
        public async Task GetProtectionOptions_ShouldReturnCurrentSettings()
        {
            // Act
            var options = await _client.Measure.GetProtectionOptions();

            // Assert
            options.Should().NotBeNull();
            options.ClippingThreshold.Should().BeGreaterOrEqualTo(0.0);
            options.SPLLimit.Should().BeGreaterThan(0.0);
            
            _output.WriteLine($"Abort on clipping: {options.AbortOnClipping}");
            _output.WriteLine($"Clipping threshold: {options.ClippingThreshold}%");
            _output.WriteLine($"Abort on SPL exceeded: {options.AbortOnSPLExceeded}");
            _output.WriteLine($"SPL limit: {options.SPLLimit} dB");
        }

        #endregion

        #region Sweep Configuration Tests

        [Fact]
        public async Task GetSweepConfiguration_ShouldReturnCurrentSettings()
        {
            // Act
            var config = await _client.Measure.GetSweepConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.StartFreq.Should().BeGreaterThan(0);
            config.EndFreq.Should().BeGreaterThan(config.StartFreq);
            config.Length.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Sweep: {config.StartFreq}Hz - {config.EndFreq}Hz");
            _output.WriteLine($"Length: {config.Length}");
            _output.WriteLine($"Fill silence with dither: {config.FillSilenceWithDither}");
        }

        [Fact]
        public async Task GetSweepRepetitions_ShouldReturnCount()
        {
            // Act
            var repetitions = await _client.Measure.GetSweepRepetitions();

            // Assert
            repetitions.Should().NotBeNull();
            repetitions.Repetitions.Should().BeGreaterOrEqualTo(1);
            repetitions.Repetitions.Should().BeLessOrEqualTo(16);
            
            _output.WriteLine($"Sweep repetitions: {repetitions.Repetitions}");
        }

        #endregion

        #region Timing Configuration Tests

        [Fact]
        public async Task GetTimingReference_ShouldReturnCurrentReference()
        {
            // Act
            var reference = await _client.Measure.GetTimingReference();

            // Assert
            reference.Should().NotBeNull();
            reference.Reference.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Timing reference: {reference.Reference}");
        }

        [Fact]
        public async Task GetTimingOffset_ShouldReturnOffset()
        {
            // Act
            var offset = await _client.Measure.GetTimingOffset();

            // Assert
            offset.Should().NotBeNull();
            offset.Offset.Should().BeInRange(-100.0, 100.0); // Reasonable range
            
            _output.WriteLine($"Timing offset: {offset.Offset} ms");
        }

        #endregion

        #region Playback Configuration Tests

        [Fact]
        public async Task GetPlaybackMode_ShouldReturnCurrentMode()
        {
            // Act
            var mode = await _client.Measure.GetPlaybackMode();

            // Assert
            mode.Should().NotBeNull();
            mode.Mode.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Playback mode: {mode.Mode}");
        }

        [Fact]
        public async Task GetPlaybackModeChoices_ShouldReturnAvailableModes()
        {
            // Act
            var choices = await _client.Measure.GetPlaybackModeChoices();

            // Assert
            choices.Should().NotBeNull();
            choices.Should().NotBeEmpty();
            
            _output.WriteLine($"Found {choices.Count} playback modes:");
            foreach (var choice in choices)
            {
                _output.WriteLine($"  - {choice}");
            }
        }

        #endregion

        #region Measurement Mode Configuration Tests

        [Fact]
        public async Task GetMeasurementMode_ShouldReturnCurrentMode()
        {
            // Act
            var mode = await _client.Measure.GetMeasurementMode();

            // Assert
            mode.Should().NotBeNull();
            mode.Mode.Should().NotBeNullOrEmpty();
            mode.IsValidMode().Should().BeTrue();
            
            _output.WriteLine($"Measurement mode: {mode.Mode}");
            _output.WriteLine($"Is multi-measurement mode: {mode.IsMultiMeasurementMode()}");
        }

        [Fact]
        public async Task GetMeasurementModeChoices_ShouldReturnAvailableModes()
        {
            // Act
            var choices = await _client.Measure.GetMeasurementModeChoices();

            // Assert
            choices.Should().NotBeNull();
            choices.Should().NotBeEmpty();
            choices.Should().Contain("Single");
            
            _output.WriteLine($"Found {choices.Count} measurement modes:");
            foreach (var choice in choices)
            {
                _output.WriteLine($"  - {choice}");
            }
        }

        [Fact]
        public async Task GetNumberOfRepetitions_ShouldReturnCount()
        {
            // Act
            var repetitions = await _client.Measure.GetNumberOfRepetitions();

            // Assert
            repetitions.Should().NotBeNull();
            repetitions.Repetitions.Should().BeGreaterOrEqualTo(1);
            
            _output.WriteLine($"Number of repetitions: {repetitions.Repetitions}");
        }

        [Fact]
        public async Task GetStartLevel_ShouldReturnLevel()
        {
            // Act
            var level = await _client.Measure.GetStartLevel();

            // Assert
            level.Should().NotBeNull();
            level.Unit.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"Start level: {level.Level:F1} {level.Unit}");
        }

        [Fact]
        public async Task GetEndLevel_ShouldReturnLevel()
        {
            // Act
            var level = await _client.Measure.GetEndLevel();

            // Assert
            level.Should().NotBeNull();
            level.Unit.Should().NotBeNullOrEmpty();
            
            _output.WriteLine($"End level: {level.Level:F1} {level.Unit}");
        }

        [Fact]
        public async Task GetSequentialChannels_ShouldReturnChannelList()
        {
            // Act
            var channels = await _client.Measure.GetSequentialChannels();

            // Assert
            channels.Should().NotBeNull();
            // May be empty if not configured
            
            _output.WriteLine($"Sequential channels: {channels.Channels.Count}");
            foreach (var channel in channels.Channels)
            {
                _output.WriteLine($"  - {channel}");
            }
        }

        [Fact]
        public async Task GetSequentialChoices_ShouldReturnAvailableChannels()
        {
            // Act
            var choices = await _client.Measure.GetSequentialChoices();

            // Assert
            choices.Should().NotBeNull();
            // May be empty if no audio configured
            
            _output.WriteLine($"Found {choices.Count} channel choices:");
            foreach (var choice in choices.Take(5)) // Show first 5
            {
                _output.WriteLine($"  - {choice}");
            }
        }

        #endregion

        #region Additional Settings Tests

        [Fact]
        public async Task GetStartDelay_ShouldReturnDelay()
        {
            // Act
            var delay = await _client.Measure.GetStartDelay();

            // Assert
            delay.Should().NotBeNull();
            delay.Delay.Should().BeGreaterOrEqualTo(0.0);
            delay.IsReasonableDelay().Should().BeTrue();
            
            _output.WriteLine($"Start delay: {delay.Delay} seconds");
        }

        [Fact]
        public async Task GetInvertSecondOutput_ShouldReturnSetting()
        {
            // Act
            var invert = await _client.Measure.GetInvertSecondOutput();

            // Assert
            invert.Should().NotBeNull();
            
            _output.WriteLine($"Invert second output: {invert.Invert}");
        }

        [Fact]
        public async Task GetFillSilenceWithDither_ShouldReturnSetting()
        {
            // Act
            var fill = await _client.Measure.GetFillSilenceWithDither();

            // Assert
            fill.Should().NotBeNull();
            
            _output.WriteLine($"Fill silence with dither: {fill.Fill}");
        }

        [Fact]
        public async Task GetCaptureNoiseFloor_ShouldReturnSetting()
        {
            // Act
            var capture = await _client.Measure.GetCaptureNoiseFloor();

            // Assert
            capture.Should().NotBeNull();
            
            _output.WriteLine($"Capture noise floor: {capture.Capture}");
        }

        #endregion
    }
}
