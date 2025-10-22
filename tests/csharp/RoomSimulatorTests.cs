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
    /// </summary>
    public class RoomSimulatorTests : IAsyncLifetime
    {
        private RewClient? _client;

        public async Task InitializeAsync()
        {
            _client = await RewClient.ConnectAsync();
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await Task.CompletedTask;
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

                var updated = await _client.RoomSimulator.GetRoomSize();

                // Assert
                Assert.Equal(original.Length + 0.5, updated.Length, 2);
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

                var updated = await _client.RoomSimulator.GetAbsorptions();

                // Assert
                Assert.Equal(0.2, updated.Front);
                Assert.Equal(0.15, updated.Ceiling);
                Assert.Equal(0.1, updated.Floor);
            }
            finally
            {
                // Cleanup
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

                var updated = await _client.RoomSimulator.GetAbsorptions();

                // Assert
                Assert.Equal(0.25, updated.Ceiling);
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

                var updated = await _client.RoomSimulator.GetOptions();

                // Assert
                Assert.Equal(!original.ShowAnechoicResponses, updated.ShowAnechoicResponses);
                Assert.Equal(100, updated.CrossoverFrequencyHz);
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

                var updated = await _client.RoomSimulator.GetOptions();

                // Assert
                Assert.Equal(90, updated.CrossoverFrequencyHz);
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

                var updated = await _client.RoomSimulator.GetMicPositionOffsets();

                // Assert
                Assert.Equal(0.4, updated.InFront, 2);
                Assert.Equal(0.4, updated.Behind, 2);
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
                Right = original.Right,
                Above = original.Above,
                Below = original.Below
            };

            try
            {
                // Act
                var result = await _client.RoomSimulator.UpdateMicPositionOffsets(modified);
                Assert.True(result);

                var updated = await _client.RoomSimulator.GetMicPositionOffsets();

                // Assert
                Assert.Equal(0.35, updated.InFront, 2);
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
        public async Task GetFrequencyResponse_ReturnsCombinedResponse()
        {
            // Arrange & Act
            var response = await _client!.RoomSimulator.GetFrequencyResponse();

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetSourceFrequencyResponse_ReturnsSourceResponse()
        {
            // Arrange
            var sources = await _client!.RoomSimulator.GetSources();
            Assert.NotEmpty(sources);
            var firstSource = sources.First();

            // Act
            var response = await _client.RoomSimulator.GetSourceFrequencyResponse(firstSource);

            // Assert
            Assert.NotNull(response);
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

                var updated = await _client.RoomSimulator.GetSourceConfiguration(sourceName);

                // Assert
                Assert.Equal(40, updated.LfMinus3dBHz);
                Assert.Equal("Sealed", updated.EnclosureType);
                Assert.Equal(3.0, updated.GaindB);
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
