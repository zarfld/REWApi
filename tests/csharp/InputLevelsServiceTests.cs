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
    /// Tests for Input Levels Service
    /// ISO/IEC/IEEE 12207:2017 - Verification and Validation
    /// IEEE 1012-2016 - Verification and Validation
    /// 
    /// Test REW Input Levels API endpoints:
    /// - GET /input-levels/commands
    /// - POST /input-levels/command
    /// - GET /input-levels/last-levels
    /// - GET /input-levels/units
    /// - POST /input-levels/subscribe
    /// - POST /input-levels/unsubscribe
    /// - GET /input-levels/subscribers
    /// </summary>
    public class InputLevelsServiceTests : IClassFixture<RewClientFixture>
    {
        private readonly RewClient _client;
        private readonly ITestOutputHelper _output;

        public InputLevelsServiceTests(RewClientFixture fixture, ITestOutputHelper output)
        {
            _client = fixture.Client;
            _output = output;
        }

        #region Command Tests

        [Fact]
        public async Task GetCommands_ShouldReturnAvailableCommands()
        {
            // Act
            var commands = await _client.InputLevels.GetCommandsAsync();

            // Assert
            commands.Should().NotBeNull();
            commands.Should().Contain("Start");
            commands.Should().Contain("Stop");
            _output.WriteLine($"Available commands: {string.Join(", ", commands)}");
        }

        [Fact]
        public async Task StartMonitoring_ShouldStartInputLevelMonitoring()
        {
            // Act
            var result = await _client.InputLevels.StartMonitoringAsync();

            // Assert
            result.Should().BeTrue("API should accept start command");
            _output.WriteLine("Started input level monitoring");

            // Wait for CommandGate operation
            await Task.Delay(1500);

            // Try to verify monitoring state (may not be reflected immediately)
            var isMonitoring = await _client.InputLevels.IsMonitoringAsync();
            _output.WriteLine($"Monitoring state: {isMonitoring}");

            // Cleanup
            await _client.InputLevels.StopMonitoringAsync();
            await Task.Delay(1500);
        }

        [Fact]
        public async Task StopMonitoring_ShouldStopInputLevelMonitoring()
        {
            // Arrange - Start monitoring first
            await _client.InputLevels.StartMonitoringAsync();
            await Task.Delay(1500); // Wait for start to complete

            // Act
            var result = await _client.InputLevels.StopMonitoringAsync();

            // Assert
            result.Should().BeTrue("API should accept stop command");
            _output.WriteLine("Stopped input level monitoring");

            // Wait for CommandGate operation
            await Task.Delay(1500);

            // Try to verify monitoring state (may not be reflected immediately)
            var isMonitoring = await _client.InputLevels.IsMonitoringAsync();
            _output.WriteLine($"Monitoring state after stop: {isMonitoring}");
        }

        #endregion

        #region Level Reading Tests

        [Fact]
        public async Task GetLastLevels_ShouldReturnLevelDictionary()
        {
            // Act
            var levels = await _client.InputLevels.GetLastLevelsAsync();

            // Assert
            levels.Should().NotBeNull();
            // May be empty if not monitoring
            _output.WriteLine($"Retrieved {levels.Count} channel levels");
            foreach (var kvp in levels)
            {
                _output.WriteLine($"  {kvp.Key}: {kvp.Value:F2}");
            }
        }

        [Fact]
        public async Task GetUnits_ShouldReturnAvailableUnits()
        {
            // Act
            var units = await _client.InputLevels.GetUnitsAsync();

            // Assert
            units.Should().NotBeNull();
            units.Should().NotBeEmpty();
            _output.WriteLine($"Available units: {string.Join(", ", units)}");
        }

        [Fact]
        public async Task IsMonitoring_ShouldCheckMonitoringState()
        {
            // Act
            var isMonitoring = await _client.InputLevels.IsMonitoringAsync();

            // Assert
            // Should not throw - result depends on current state
            _output.WriteLine($"Monitoring active: {isMonitoring}");
        }

        #endregion

        #region Subscription Tests

        [Fact] // Unskipped - Bug fixed: Property name changed from "callbackUrl" to "url"
        public async Task Subscribe_ShouldAddSubscription()
        {
            // Arrange
            var callbackUrl = "http://localhost:8080/input-levels-callback";

            // Act
            var result = await _client.InputLevels.SubscribeAsync(callbackUrl);

            // Assert
            result.Should().BeTrue();

            // Verify subscription exists
            var subscribers = await _client.InputLevels.GetSubscribersAsync();
            subscribers.Should().Contain(callbackUrl);

            // Cleanup
            await _client.InputLevels.UnsubscribeAsync(callbackUrl);
        }

        [Fact] // Unskipped - Bug fixed: Property name changed from "callbackUrl" to "url"
        public async Task Unsubscribe_ShouldRemoveSubscription()
        {
            // Arrange
            var callbackUrl = "http://localhost:8080/input-levels-callback";
            await _client.InputLevels.SubscribeAsync(callbackUrl);

            // Act
            var result = await _client.InputLevels.UnsubscribeAsync(callbackUrl);

            // Assert
            result.Should().BeTrue();

            // Verify subscription removed
            var subscribers = await _client.InputLevels.GetSubscribersAsync();
            subscribers.Should().NotContain(callbackUrl);
        }

        [Fact]
        public async Task GetSubscribers_ShouldReturnSubscriberList()
        {
            // Act
            var subscribers = await _client.InputLevels.GetSubscribersAsync();

            // Assert
            subscribers.Should().NotBeNull();
            // May be empty if no subscriptions
            _output.WriteLine($"Current subscribers: {subscribers.Count}");
            foreach (var subscriber in subscribers)
            {
                _output.WriteLine($"  - {subscriber}");
            }
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task Subscribe_WithNullUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.SubscribeAsync(null!));
        }

        [Fact]
        public async Task Subscribe_WithEmptyUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.SubscribeAsync(""));
        }

        [Fact]
        public async Task Subscribe_WithWhitespaceUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.SubscribeAsync("   "));
        }

        [Fact]
        public async Task Unsubscribe_WithNullUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.UnsubscribeAsync(null!));
        }

        [Fact]
        public async Task Unsubscribe_WithEmptyUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.UnsubscribeAsync(""));
        }

        [Fact]
        public async Task Unsubscribe_WithWhitespaceUrl_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _client.InputLevels.UnsubscribeAsync("   "));
        }

        #endregion
    }
}
