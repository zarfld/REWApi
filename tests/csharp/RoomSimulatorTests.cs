using Xunit;
using REWApi;
using REWApi.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace REWApi.Tests
{
    /// <summary>
    /// Tests for Room Simulator Service (all 15 endpoints)
    /// Covers room configuration, source management, and frequency response
    /// 
    /// Uses baseline sample data loading to ensure test isolation and consistent state.
    /// </summary>
    public class RoomSimulatorTests : BaseServiceTests
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            
            // Load baseline sample data before each test to ensure consistent state
            await LoadBaselineAsync();
        }

        #region Length Units Tests

        [Fact]
        public async Task GetLengthUnits_ReturnsValidUnits()
        {
            // Arrange & Act
            var units = await _client!.RoomSimulator.GetLengthUnits();

            // Assert
            Assert.NotNull(units);
            Assert.NotEmpty(units);
            Assert.Contains("metres", units);
        }

        #endregion

        #region Source Names Tests

        [Fact]
        public async Task GetSourceNames_ReturnsValidNames()
        {
            // Arrange & Act
            var names = await _client!.RoomSimulator.GetSourceNames();

            // Assert
            Assert.NotNull(names);
            Assert.Contains("Left", names);
            Assert.Contains("Right", names);
        }

        #endregion

        #region Room Size Tests

        [Fact]
        public async Task GetRoomSize_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var roomSize = await _client!.RoomSimulator.GetRoomSize();

            // Assert
            Assert.NotNull(roomSize);
            Assert.NotEmpty(roomSize.Unit);
            Assert.True(roomSize.Length > 0);
            Assert.True(roomSize.Width > 0);
            Assert.True(roomSize.Height > 0);
        }

        [Fact]
        public async Task SetRoomSize_UpdatesConfiguration()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetRoomSize();
            var newSize = new RoomSize
            {
                Unit = "metres",
                Length = 6.0,
                Width = 5.0,
                Height = 3.0
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetRoomSize(newSize);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetRoomSize();

                // Assert
                Assert.Equal(6.0, updated.Length);
                Assert.Equal(5.0, updated.Width);
                Assert.Equal(3.0, updated.Height);
            }
            finally
            {
                // Cleanup - restore original
                await _client.RoomSimulator.SetRoomSize(original);
            }
        }

        [Fact]
        public async Task UpdateRoomSize_ModifiesConfiguration()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetRoomSize();
            var modified = new RoomSize
            {
                Unit = original.Unit,
                Length = original.Length + 0.5,
                Width = original.Width,
                Height = original.Height
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.UpdateRoomSize(modified);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetRoomSize();

                // Assert - verify length was increased by 0.5
                Assert.Equal(original.Length + 0.5, updated.Length, precision: 2);
                // Verify other dimensions remained unchanged
                Assert.Equal(original.Width, updated.Width, precision: 2);
                Assert.Equal(original.Height, updated.Height, precision: 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.UpdateRoomSize(original);
            }
        }

        #endregion

        #region Room Sealed Tests

        [Fact]
        public async Task GetRoomIsSealed_ReturnsBoolean()
        {
            // Arrange & Act
            var isSealed = await _client!.RoomSimulator.GetRoomIsSealed();

            // Assert
            Assert.IsType<bool>(isSealed);
        }

        [Fact]
        public async Task SetRoomIsSealed_UpdatesStatus()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetRoomIsSealed();

            try
            {
                // Act - toggle status
                var result = await _client.RoomSimulator.SetRoomIsSealed(!original);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetRoomIsSealed();

                // Assert
                Assert.Equal(!original, updated);
            }
            finally
            {
                // Cleanup - restore original
                await _client.RoomSimulator.SetRoomIsSealed(original);
            }
        }

        #endregion

        #region Absorptions Tests

        [Fact]
        public async Task GetAbsorptions_ReturnsValidCoefficients()
        {
            // Arrange & Act
            var absorptions = await _client!.RoomSimulator.GetAbsorptions();

            // Assert
            Assert.NotNull(absorptions);
            Assert.InRange(absorptions.Front, 0.0, 1.0);
            Assert.InRange(absorptions.Back, 0.0, 1.0);
            Assert.InRange(absorptions.Left, 0.0, 1.0);
            Assert.InRange(absorptions.Right, 0.0, 1.0);
            Assert.InRange(absorptions.Ceiling, 0.0, 1.0);
            Assert.InRange(absorptions.Floor, 0.0, 1.0);
        }

        [Fact]
        public async Task SetAbsorptions_UpdatesCoefficients()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetAbsorptions();
            var newAbsorptions = new RoomAbsorptions
            {
                Front = 0.2,
                Back = 0.2,
                Left = 0.2,
                Right = 0.2,
                Ceiling = 0.15,
                Floor = 0.1
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetAbsorptions(newAbsorptions);
                Assert.True(result);

                // Wait a moment for REW to process the change
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetAbsorptions();

                // Assert - use tolerance for floating point comparison
                Assert.Equal(0.2, updated.Front, precision: 2);
                Assert.Equal(0.2, updated.Back, precision: 2);
                Assert.Equal(0.2, updated.Left, precision: 2);
                Assert.Equal(0.2, updated.Right, precision: 2);
                Assert.Equal(0.15, updated.Ceiling, precision: 2);
                Assert.Equal(0.1, updated.Floor, precision: 2);
            }
            finally
            {
                // Cleanup - restore original state
                await _client.RoomSimulator.SetAbsorptions(original);
            }
        }

        [Fact]
        public async Task UpdateAbsorptions_ModifiesCoefficients()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetAbsorptions();
            var modified = new RoomAbsorptions
            {
                Front = original.Front,
                Back = original.Back,
                Left = original.Left,
                Right = original.Right,
                Ceiling = 0.25,
                Floor = original.Floor
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.UpdateAbsorptions(modified);
                Assert.True(result);

                // Wait a moment for REW to process the change
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetAbsorptions();

                // Assert - verify ceiling was updated
                Assert.Equal(0.25, updated.Ceiling, precision: 2);
                // Verify other values remained unchanged
                Assert.Equal(original.Front, updated.Front, precision: 2);
                Assert.Equal(original.Back, updated.Back, precision: 2);
                Assert.Equal(original.Floor, updated.Floor, precision: 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.UpdateAbsorptions(original);
            }
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task GetOptions_ReturnsValidConfiguration()
        {
            // Arrange & Act
            var options = await _client!.RoomSimulator.GetOptions();

            // Assert
            Assert.NotNull(options);
            Assert.IsType<bool>(options.ShowAnechoicResponses);
            Assert.IsType<bool>(options.TimeAlignSpeakersAndSubs);
            Assert.True(options.CrossoverFrequencyHz > 0);
        }

        [Fact]
        public async Task SetOptions_UpdatesConfiguration()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetOptions();
            var newOptions = new RoomSimOptions
            {
                ShowAnechoicResponses = !original.ShowAnechoicResponses,
                TimeAlignSpeakersAndSubs = true,
                AlignSubsIndividually = false,
                UseCrossoverFilter = true,
                CrossoverFrequencyHz = 100,
                SubsHaveSameDelay = false,
                SubsDelayms = 0
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetOptions(newOptions);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetOptions();

                // Assert - verify all set values
                Assert.Equal(!original.ShowAnechoicResponses, updated.ShowAnechoicResponses);
                Assert.True(updated.TimeAlignSpeakersAndSubs);
                Assert.False(updated.AlignSubsIndividually);
                Assert.True(updated.UseCrossoverFilter);
                Assert.Equal(100, updated.CrossoverFrequencyHz);
                Assert.False(updated.SubsHaveSameDelay);
                Assert.Equal(0, updated.SubsDelayms);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.SetOptions(original);
            }
        }

        [Fact]
        public async Task UpdateOptions_ModifiesConfiguration()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetOptions();
            var modified = new RoomSimOptions
            {
                ShowAnechoicResponses = original.ShowAnechoicResponses,
                TimeAlignSpeakersAndSubs = original.TimeAlignSpeakersAndSubs,
                AlignSubsIndividually = original.AlignSubsIndividually,
                UseCrossoverFilter = original.UseCrossoverFilter,
                CrossoverFrequencyHz = 90,
                SubsHaveSameDelay = original.SubsHaveSameDelay,
                SubsDelayms = original.SubsDelayms
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.UpdateOptions(modified);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetOptions();

                // Assert - verify CrossoverFrequencyHz was updated
                Assert.Equal(90, updated.CrossoverFrequencyHz);
                // Verify other values remained unchanged
                Assert.Equal(original.ShowAnechoicResponses, updated.ShowAnechoicResponses);
                Assert.Equal(original.TimeAlignSpeakersAndSubs, updated.TimeAlignSpeakersAndSubs);
                Assert.Equal(original.AlignSubsIndividually, updated.AlignSubsIndividually);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.UpdateOptions(original);
            }
        }

        #endregion

        #region Head Position Tests

        [Fact]
        public async Task GetHeadPosition_ReturnsValidPosition()
        {
            // Arrange & Act
            var position = await _client!.RoomSimulator.GetHeadPosition();

            // Assert
            Assert.NotNull(position);
            Assert.NotEmpty(position.Unit);
            Assert.True(position.FromRear >= 0);
            Assert.True(position.FromLeft >= 0);
            Assert.True(position.FromFloor >= 0);
        }

        [Fact]
        public async Task SetHeadPosition_UpdatesPosition()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetHeadPosition();
            var newPosition = new RoomPosition
            {
                Unit = "metres",
                FromRear = 2.0,
                FromLeft = 2.5,
                FromFloor = 1.2
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetHeadPosition(newPosition);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetHeadPosition();

                // Assert
                Assert.Equal(2.0, updated.FromRear, 2);
                Assert.Equal(2.5, updated.FromLeft, 2);
                Assert.Equal(1.2, updated.FromFloor, 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.SetHeadPosition(original);
            }
        }

        #endregion

        #region Microphone Position Offsets Tests

        [Fact]
        public async Task GetMicPositionOffsets_ReturnsValidOffsets()
        {
            // Arrange & Act
            var offsets = await _client!.RoomSimulator.GetMicPositionOffsets();

            // Assert
            Assert.NotNull(offsets);
            Assert.NotEmpty(offsets.Unit);
            Assert.True(offsets.InFront >= 0);
            Assert.True(offsets.Behind >= 0);
        }

        [Fact]
        public async Task SetMicPositionOffsets_UpdatesOffsets()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetMicPositionOffsets();
            var newOffsets = new MicPositionOffsets
            {
                Unit = "metres",
                InFront = 0.4,
                Behind = 0.4,
                Left = 0.4,
                Right = 0.4,
                Above = 0.4,
                Below = 0.4
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetMicPositionOffsets(newOffsets);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetMicPositionOffsets();

                // Assert - verify all offsets were set
                Assert.Equal(0.4, updated.InFront, precision: 2);
                Assert.Equal(0.4, updated.Behind, precision: 2);
                Assert.Equal(0.4, updated.Left, precision: 2);
                Assert.Equal(0.4, updated.Right, precision: 2);
                Assert.Equal(0.4, updated.Above, precision: 2);
                Assert.Equal(0.4, updated.Below, precision: 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.SetMicPositionOffsets(original);
            }
        }

        [Fact]
        public async Task UpdateMicPositionOffsets_ModifiesOffsets()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetMicPositionOffsets();
            var modified = new MicPositionOffsets
            {
                Unit = original.Unit,
                InFront = 0.35,
                Behind = original.Behind,
                Left = original.Left,
                Right = 0.35,
                Above = original.Above,
                Below = original.Below
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.UpdateMicPositionOffsets(modified);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetMicPositionOffsets();

                // Assert - verify modified values
                Assert.Equal(0.35, updated.InFront, precision: 2);
                Assert.Equal(0.35, updated.Right, precision: 2);
                // Verify unmodified values remained unchanged
                Assert.Equal(original.Behind, updated.Behind, precision: 2);
                Assert.Equal(original.Left, updated.Left, precision: 2);
                Assert.Equal(original.Above, updated.Above, precision: 2);
                Assert.Equal(original.Below, updated.Below, precision: 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.UpdateMicPositionOffsets(original);
            }
        }

        #endregion

        #region Sources Tests

        [Fact]
        public async Task GetSources_ReturnsActiveSourcesList()
        {
            // Arrange & Act
            var sources = await _client!.RoomSimulator.GetSources();

            // Assert
            Assert.NotNull(sources);
        }

        [Fact]
        public async Task SetSources_UpdatesActiveSources()
        {
            // Arrange
            var original = await _client!.RoomSimulator.GetSources();
            var newSources = new List<string> { "Left", "Right" };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetSources(newSources);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetSources();

                // Assert
                Assert.Contains("Left", updated);
                Assert.Contains("Right", updated);
            }
            finally
            {
                // Cleanup - restore original sources
                await _client.RoomSimulator.SetSources(original);
            }
        }

        #endregion

        #region Microphone Positions Tests

        [Fact]
        public async Task GetMicPositions_ReturnsPositionNames()
        {
            // Arrange & Act
            var positions = await _client!.RoomSimulator.GetMicPositions();

            // Assert
            Assert.NotNull(positions);
            Assert.NotEmpty(positions);
            Assert.Contains("Main", positions);
        }

        #endregion

        #region Frequency Response Tests

        [Fact]
        public async Task GetFrequencyResponse_WithDefaultMicPosition_ReturnsCombinedResponse()
        {
            // Act - Uses default "Main" mic position as required by API
            var response = await _client!.RoomSimulator.GetFrequencyResponse();

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Frequencies);
            Assert.NotNull(response.Magnitudes);
        }

        [Fact]
        public async Task GetFrequencyResponse_WithSpecifiedMicPosition_ReturnsResponse()
        {
            // Act - Explicitly specify "Main" mic position
            var response = await _client!.RoomSimulator.GetFrequencyResponse("Main");

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Frequencies);
            Assert.NotNull(response.Magnitudes);
        }

        [Fact]
        public async Task GetSourceFrequencyResponse_WithMicPosition_ReturnsSourceResponse()
        {
            // Arrange - Get first source name (GetSources returns SourcesList with Sources = List<string>)
            var sourcesList = await _client!.RoomSimulator.GetSourceNames();
            var sourceName = sourcesList.FirstOrDefault() ?? "L";

            // Act - Specify "Main" mic position as required by API documentation
            var response = await _client.RoomSimulator.GetSourceFrequencyResponse(sourceName, "Main");

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Frequencies);
            Assert.NotNull(response.Magnitudes);
        }

        #endregion

        #region Source Position Tests

        [Fact]
        public async Task GetSourcePosition_ReturnsValidPosition()
        {
            // Arrange
            var sources = await _client!.RoomSimulator.GetSources();
            if (!sources.Any())
            {
                // Set a default source if none active
                await _client.RoomSimulator.SetSources(new List<string> { "Left" });
                sources = await _client.RoomSimulator.GetSources();
            }
            var sourceName = sources.First();

            // Act
            var position = await _client.RoomSimulator.GetSourcePosition(sourceName);

            // Assert
            Assert.NotNull(position);
            Assert.NotEmpty(position.Unit);
        }

        [Fact]
        public async Task SetSourcePosition_UpdatesSourcePosition()
        {
            // Arrange
            var sources = await _client!.RoomSimulator.GetSources();
            if (!sources.Any())
            {
                await _client.RoomSimulator.SetSources(new List<string> { "Left" });
                sources = await _client.RoomSimulator.GetSources();
            }
            var sourceName = sources.First();
            var original = await _client.RoomSimulator.GetSourcePosition(sourceName);
            
            var newPosition = new RoomPosition
            {
                Unit = "metres",
                FromRear = 0.5,
                FromLeft = 1.0,
                FromFloor = 0.5
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetSourcePosition(sourceName, newPosition);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetSourcePosition(sourceName);

                // Assert
                Assert.Equal(0.5, updated.FromRear, 2);
                Assert.Equal(1.0, updated.FromLeft, 2);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.SetSourcePosition(sourceName, original);
            }
        }

        #endregion

        #region Source Configuration Tests

        [Fact]
        public async Task GetSourceConfiguration_ReturnsValidConfig()
        {
            // Arrange
            var sources = await _client!.RoomSimulator.GetSources();
            if (!sources.Any())
            {
                await _client.RoomSimulator.SetSources(new List<string> { "Sub1" });
                sources = await _client.RoomSimulator.GetSources();
            }
            var sourceName = sources.First();

            // Act
            var config = await _client.RoomSimulator.GetSourceConfiguration(sourceName);

            // Assert
            Assert.NotNull(config);
            Assert.True(config.LfMinus3dBHz > 0);
        }

        [Fact]
        public async Task SetSourceConfiguration_UpdatesConfig()
        {
            // Arrange
            var sources = await _client!.RoomSimulator.GetSources();
            if (!sources.Any())
            {
                await _client.RoomSimulator.SetSources(new List<string> { "Sub1" });
                sources = await _client.RoomSimulator.GetSources();
            }
            var sourceName = sources.First();
            var original = await _client.RoomSimulator.GetSourceConfiguration(sourceName);

            var newConfig = new SourceConfiguration
            {
                LfMinus3dBHz = 40,
                EnclosureType = "Sealed",
                Invert = false,
                Delayms = 1.0,
                GaindB = 3.0
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.SetSourceConfiguration(sourceName, newConfig);
                Assert.True(result);

                // Wait for REW to process
                await Task.Delay(100);

                var updated = await _client.RoomSimulator.GetSourceConfiguration(sourceName);

                // Assert - verify all set values with appropriate precision
                Assert.Equal(40, updated.LfMinus3dBHz);
                Assert.Equal("Sealed", updated.EnclosureType);
                Assert.False(updated.Invert);
                Assert.Equal(1.0, updated.Delayms, precision: 1);
                Assert.Equal(3.0, updated.GaindB, precision: 1);
            }
            finally
            {
                // Cleanup
                await _client.RoomSimulator.SetSourceConfiguration(sourceName, original);
            }
        }

        #endregion
    }
}
