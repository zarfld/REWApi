using System.Threading.Tasks;
using Xunit;

namespace REWApi.Tests
{
    /// <summary>
    /// Tests to verify the baseline sample data loading mechanism works correctly.
    /// These tests validate the test infrastructure before using it in actual service tests.
    /// </summary>
    [Collection("REW Integration Tests")]
    public class BaselineVerificationTests : BaseServiceTests
    {
        [Fact]
        public async Task LoadBaseline_LoadsCorrectNumberOfMeasurements()
        {
            // Arrange & Act
            await LoadBaselineAsync();

            // Assert
            await VerifyBaselineLoadedAsync(); // Verifies 14 measurements loaded
        }

        [Fact]
        public async Task LoadBaseline_LoadsKnownMeasurements()
        {
            // Arrange
            await LoadBaselineAsync();

            // Act
            var artist = await FindMeasurementByTitleAsync("Artist 3+Q2070Si");
            var relSub = await FindMeasurementByTitleAsync("REL Sub, No EQ");
            var bw = await FindMeasurementByTitleAsync("B&W M1");
            var theatre = await FindMeasurementByTitleAsync("Theatre");

            // Assert
            Assert.NotNull(artist);
            Assert.NotNull(relSub);
            Assert.NotNull(bw);
            Assert.NotNull(theatre);

            // Verify measurement characteristics from sample data
            Assert.InRange(artist.StartFreq, 2.0, 2.5);
            Assert.InRange(artist.EndFreq, 24000, 25000);
            
            Assert.InRange(relSub.StartFreq, 2.0, 2.5);
            Assert.InRange(relSub.EndFreq, 199, 201);
        }

        [Fact]
        public async Task LoadBaseline_CanBeCalledMultipleTimes()
        {
            // Arrange & Act - Load baseline twice
            await LoadBaselineAsync();
            var measurements1 = await GetMeasurementsAsync();
            
            await LoadBaselineAsync();
            var measurements2 = await GetMeasurementsAsync();

            // Assert - Should have same measurements both times
            Assert.True(measurements1.Count >= 7, "First load should have at least 7 measurements");
            Assert.True(measurements2.Count >= 7, "Second load should have at least 7 measurements");
        }

        [Fact]
        public async Task ClearAllMeasurements_RemovesAllMeasurements()
        {
            // Arrange
            await LoadBaselineAsync();
            var beforeClear = await GetMeasurementsAsync();
            Assert.NotEmpty(beforeClear);

            // Act
            await ClearAllMeasurementsAsync();

            // Assert
            var afterClear = await GetMeasurementsAsync();
            Assert.Empty(afterClear);
        }

        [Fact]
        public async Task GetMeasurements_ParsesREWObjectFormat()
        {
            // Arrange
            await LoadBaselineAsync();

            // Act
            var measurements = await GetMeasurementsAsync();

            // Assert - Verify proper parsing of REW's object format (not array)
            Assert.NotNull(measurements);
            Assert.IsType<System.Collections.Generic.List<MeasurementSummary>>(measurements);
            Assert.All(measurements, m =>
            {
                Assert.NotNull(m.Uuid);
                Assert.NotNull(m.Title);
            });
        }

        [Fact]
        public async Task FindMeasurementByTitle_FindsExistingMeasurement()
        {
            // Arrange
            await LoadBaselineAsync();

            // Act
            var found = await FindMeasurementByTitleAsync("Artist 3+Q2070Si");

            // Assert
            Assert.NotNull(found);
            Assert.Equal("Artist 3+Q2070Si", found.Title);
            Assert.NotNull(found.Uuid);
        }

        [Fact]
        public async Task FindMeasurementByTitle_ReturnsNullForNonExistent()
        {
            // Arrange
            await LoadBaselineAsync();

            // Act
            var notFound = await FindMeasurementByTitleAsync("NonExistentMeasurement");

            // Assert
            Assert.Null(notFound);
        }

        [Fact]
        public async Task FindMeasurementByTitle_IsCaseInsensitive()
        {
            // Arrange
            await LoadBaselineAsync();

            // Act
            var found1 = await FindMeasurementByTitleAsync("ARTIST 3+Q2070SI");
            var found2 = await FindMeasurementByTitleAsync("artist 3+q2070si");
            var found3 = await FindMeasurementByTitleAsync("Artist 3+Q2070Si");

            // Assert - All should find the same measurement
            Assert.NotNull(found1);
            Assert.NotNull(found2);
            Assert.NotNull(found3);
            Assert.Equal(found1.Uuid, found2.Uuid);
            Assert.Equal(found2.Uuid, found3.Uuid);
        }
    }
}
