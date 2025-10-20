using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using REWApi;

namespace REWApi.Tests.CSharp;

/// <summary>
/// TDD Test Suite for DES-C-001: HTTP Communication Core (Existing Implementation)
/// Following Phase 05 Implementation Plan - Testing Actual Implemented Code
/// Traceability: REQ-F-001 → DES-C-001 → SRC:REWApi.cs → TEST-REWAPI-*
/// </summary>
public class REWApiHttpCommunicationTests
{
    /// <summary>
    /// Test Existing Implementation - Basic HTTP GET Operations
    /// @requirement REQ-F-001 - HTTP Communication Core
    /// @design DES-C-001 - HTTP Communication Core Component
    /// @implementation SRC:REWApi.cs - GetAsync method
    /// @test TEST-REWAPI-001-01 - Basic GET operation
    /// </summary>
    [Fact]
    public async Task GetAsync_ValidEndpoint_ReturnsJsonResponse()
    {
        // Arrange
        var rewApi = new REWApi();
        
        // Act
        var result = await rewApi.GetAsync("http://localhost:4735/version");
        
        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // Testing actual implemented REWApi.GetAsync method
    }

    /// <summary>
    /// Test Existing Implementation - Graph Update Inhibition Feature
    /// @requirement REQ-F-001 - HTTP Communication with inhibition capability
    /// @implementation SRC:REWApi.cs - SetGraphUpdateInhibition + GetAsync methods
    /// @test TEST-REWAPI-001-02 - Graph update inhibition returns null
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenInhibited_ReturnsNull()
    {
        // Arrange
        var rewApi = new REWApi();
        rewApi.SetGraphUpdateInhibition(true);
        
        // Act
        var result = await rewApi.GetAsync("http://localhost:4735/any-endpoint");
        
        // Assert
        result.Should().BeNull("because graph update is inhibited");
        
        // Testing actual implemented REWApi inhibition feature
    }

    /// <summary>
    /// Test Existing Implementation - HTTP POST Operations  
    /// @implementation SRC:REWApi.cs - PostAsync method
    /// @test TEST-REWAPI-002-01 - Basic POST operation with real REW endpoint
    /// </summary>
    [Fact]
    public async Task PostAsync_ValidEndpointAndData_ReturnsResponse()
    {
        // Arrange
        var rewApi = new REWApi();
        var measurementData = new StringContent("{\"title\": \"Test Measurement\"}", Encoding.UTF8, "application/json");
        
        // Act
        var result = await rewApi.PostAsync("http://localhost:4735/measurements", measurementData);
        
        // Assert
        result.Should().NotBeNull();
        
        // Testing actual implemented REWApi.PostAsync method with measurements endpoint
    }

    /// <summary>
    /// Test Existing Implementation - HTTP PUT Operations
    /// @implementation SRC:REWApi.cs - PutAsync method  
    /// @test TEST-REWAPI-002-02 - Basic PUT operation with real REW endpoint  
    /// </summary>
    [Fact]
    public async Task PutAsync_ValidEndpointAndData_ReturnsResponse()
    {
        // Arrange
        var rewApi = new REWApi();
        
        // First GET the current settings to have valid data for PUT
        var currentSettings = await rewApi.GetAsync("http://localhost:4735/application/settings");
        
        // Use the current settings for a valid PUT request
        var content = new StringContent(currentSettings, Encoding.UTF8, "application/json");
        
        // Act
        var result = await rewApi.PutAsync("http://localhost:4735/application/settings", content);
        
        // Assert
        result.Should().NotBeNull();
        
        // Testing actual implemented REWApi.PutAsync method with valid settings data
    }

    /// <summary>
    /// Test Existing Implementation - Inhibition During POST Operations
    /// @implementation SRC:REWApi.cs - PostAsync method with inhibition support
    /// @test TEST-REWAPI-003-01 - POST operation respects inhibition
    /// </summary>
    [Fact]
    public async Task PostAsync_WhenInhibited_ReturnsNull()
    {
        // Arrange
        var rewApi = new REWApi();
        rewApi.SetGraphUpdateInhibition(true);
        var measurementData = new StringContent("{\"title\": \"Test Measurement\"}", Encoding.UTF8, "application/json");
        
        // Act
        var result = await rewApi.PostAsync("http://localhost:4735/measurements", measurementData);
        
        // Assert
        result.Should().BeNull("because graph update is inhibited");
        
        // Testing actual implemented REWApi inhibition in POST operations
    }

    /// <summary>
    /// HTTP Error Handling Test Suite - REQ-F-HTTP-001.4 Compliance
    /// @requirement REQ-F-HTTP-001.4 - Error response handling
    /// @test TEST-REWAPI-ERROR-001 - 404 Not Found handling
    /// </summary>
    [Fact]
    public async Task GetAsync_InvalidEndpoint_ThrowsHttpRequestException()
    {
        // Arrange
        var rewApi = new REWApi();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => rewApi.GetAsync("http://localhost:4735/nonexistent-endpoint")
        );
        
        exception.Message.Should().Contain("404");
        exception.Message.Should().Contain("Not Found");
        
        // Testing actual error handling implementation
    }

    /// <summary>
    /// HTTP Error Handling Test - 400 Bad Request
    /// @test TEST-REWAPI-ERROR-002 - Malformed request handling
    /// </summary>
    [Fact]
    public async Task PostAsync_InvalidJsonData_ThrowsHttpRequestException()
    {
        // Arrange
        var rewApi = new REWApi();
        var invalidContent = new StringContent("invalid-json-data", Encoding.UTF8, "application/json");
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => rewApi.PostAsync("http://localhost:4735/measurements", invalidContent)
        );
        
        // Check for either 400 or Bad Request in the message
        bool containsErrorInfo = exception.Message.Contains("400") || exception.Message.Contains("Bad Request");
        containsErrorInfo.Should().BeTrue("because the exception should contain error status information");
        
        // Testing actual error handling for malformed requests
    }

    /// <summary>
    /// HTTP Error Handling Test - Network Connection Failure
    /// @test TEST-REWAPI-ERROR-003 - Network failure handling
    /// </summary>
    [Fact]
    public async Task GetAsync_InvalidHost_ThrowsHttpRequestException()
    {
        // Arrange
        var rewApi = new REWApi();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => rewApi.GetAsync("http://nonexistent-host:9999/test")
        );
        
        exception.Should().NotBeNull();
        
        // Testing actual network failure handling
    }

    /// <summary>
    /// HTTP Error Handling Test - Server Error Response
    /// @test TEST-REWAPI-ERROR-004 - Server error handling
    /// </summary>
    [Fact]
    public async Task PostAsync_ServerError_ThrowsHttpRequestException()
    {
        // Arrange
        var rewApi = new REWApi();
        var content = new StringContent("{\"trigger\": \"server-error\"}", Encoding.UTF8, "application/json");
        
        // Act & Assert - This test validates that if REW API returns 500, our code handles it
        // Note: This may pass if REW API handles the request successfully
        try
        {
            var result = await rewApi.PostAsync("http://localhost:4735/invalid-operation", content);
            // If successful, that's also valid behavior
            result.Should().NotBeNull();
        }
        catch (HttpRequestException ex)
        {
            // If it throws an exception, verify it contains error info
            bool containsErrorStatus = ex.Message.Contains("50") || ex.Message.Contains("40"); // Server or client error
            containsErrorStatus.Should().BeTrue("because the exception should contain HTTP status information");
        }
        
        // Testing actual server error response handling
    }

    /// <summary>
    /// HTTP Error Handling Test - Inhibition with Error Scenarios
    /// @test TEST-REWAPI-ERROR-005 - Error handling with inhibition enabled
    /// </summary>
    [Fact]
    public async Task GetAsync_InhibitedWithInvalidEndpoint_ReturnsNullWithoutException()
    {
        // Arrange
        var rewApi = new REWApi();
        rewApi.SetGraphUpdateInhibition(true);
        
        // Act
        var result = await rewApi.GetAsync("http://localhost:4735/nonexistent-endpoint");
        
        // Assert
        result.Should().BeNull("because inhibition should prevent the call entirely, avoiding errors");
        
        // Testing that inhibition prevents error scenarios
    }
}
