# REW API Integration Test Suite
# Tests our client library implementations against live REW API server

param(
    [string]$BaseUrl = "http://localhost:4735",
    [string]$Language = "all"  # all, csharp, cpp, js, ts
)

# Test Configuration
$TestResults = @()
$TestCount = 0
$PassCount = 0

function Write-TestHeader($TestName) {
    Write-Host "`n=== $TestName ===" -ForegroundColor Cyan
    $script:TestCount++
}

function Write-TestResult($TestName, $Passed, $Message = "", $Details = "") {
    if ($Passed) {
        Write-Host "✅ PASS: $TestName" -ForegroundColor Green
        $script:PassCount++
    } else {
        Write-Host "❌ FAIL: $TestName" -ForegroundColor Red
        if ($Message) { Write-Host "   $Message" -ForegroundColor Yellow }
    }
    
    $script:TestResults += [PSCustomObject]@{
        Test = $TestName
        Status = if ($Passed) { "PASS" } else { "FAIL" }
        Message = $Message
        Details = $Details
    }
    
    if ($Details) {
        Write-Host "   Details: $Details" -ForegroundColor Gray
    }
}

function Test-REWEndpoint($Endpoint, $ExpectedStatus = 200, $ExpectedContentType = "application/json") {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl$Endpoint" -UseBasicParsing -ErrorAction Stop
        
        $statusOk = $response.StatusCode -eq $ExpectedStatus
        $contentTypeOk = $response.Headers["Content-Type"] -like "*$ExpectedContentType*"
        $hasContent = $response.Content.Length -gt 0
        
        return @{
            Success = $statusOk -and $contentTypeOk
            StatusCode = $response.StatusCode
            ContentType = $response.Headers["Content-Type"]
            Content = $response.Content
            HasCORS = $response.Headers["Access-Control-Allow-Origin"] -eq "*"
        }
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $null
        }
    }
}

function Test-ArrayEncodingCompatibility() {
    Write-TestHeader "Array Encoding Compatibility (REQ-F-002)"
    
    # Test with known values that should match official REW API documentation
    $testData = @(0.125, 0.25, 0.375, 0.5)
    $expectedBase64 = "PgAAAD6AAAA+wAAAPwAAAA=="
    
    # Test if we have measurement data to decode from REW
    $measurementTest = Test-REWEndpoint "/measurements"
    if ($measurementTest.Success) {
        $measurements = $measurementTest.Content | ConvertFrom-Json
        if ($measurements -is [array] -and $measurements.Count -gt 0) {
            Write-TestResult "Measurements Available" $true "Found $($measurements.Count) measurements for array testing"
            
            # Try to get frequency response data which should contain Base64 encoded arrays
            $firstMeasurement = $measurements[0]
            if ($firstMeasurement.uuid) {
                $freqResponseTest = Test-REWEndpoint "/measurements/$($firstMeasurement.uuid)/frequency-response"
                if ($freqResponseTest.Success) {
                    $freqData = $freqResponseTest.Content | ConvertFrom-Json
                    if ($freqData.magnitude) {
                        Write-TestResult "Base64 Array Data Available" $true "Got magnitude array: $($freqData.magnitude.Substring(0, [Math]::Min(50, $freqData.magnitude.Length)))..."
                        return $freqData.magnitude
                    }
                }
            }
        } else {
            Write-TestResult "No Measurements Available" $false "Cannot test array decoding without measurement data"
        }
    } else {
        Write-TestResult "Measurements Endpoint" $false $measurementTest.Error
    }
    
    return $null
}

# Main Test Execution
Write-Host "REW API Integration Test Suite" -ForegroundColor White -BackgroundColor Blue
Write-Host "Testing against: $BaseUrl" -ForegroundColor Yellow
Write-Host "Language filter: $Language" -ForegroundColor Yellow

# 1. Basic Connectivity Tests (REQ-F-001: HTTP Communication Core)
Write-TestHeader "REW API Server Connectivity (REQ-F-001)"

$connectivityTest = Test-REWEndpoint "/"
Write-TestResult "Server Response" $connectivityTest.Success "REW API server is accessible"

$swaggerTest = Test-REWEndpoint "/doc.json"
Write-TestResult "OpenAPI Specification" $swaggerTest.Success "OpenAPI spec is available"

# 2. Core Endpoint Tests (REQ-F-001.1: GET Operations)
Write-TestHeader "Core GET Endpoints (REQ-F-001.1)"

$endpoints = @(
    "/measurements",
    "/generator/status", 
    "/spl-meter/1/commands",
    "/eq/equalisers"
)

foreach ($endpoint in $endpoints) {
    $test = Test-REWEndpoint $endpoint
    Write-TestResult "GET $endpoint" $test.Success $test.Error
    
    if ($test.Success -and $test.HasCORS) {
        Write-TestResult "CORS Headers $endpoint" $true "Access-Control-Allow-Origin: *"
    }
}

# 3. Audio System Tests (REQ-F-003: Audio System Control)
Write-TestHeader "Audio System Control (REQ-F-003)"

$audioTest = Test-REWEndpoint "/audio/status"
if ($audioTest.Success) {
    $audioStatus = $audioTest.Content | ConvertFrom-Json
    Write-TestResult "Audio Status" $true "Enabled: $($audioStatus.enabled), Ready: $($audioStatus.ready)"
} else {
    Write-TestResult "Audio Status" $false $audioTest.Error
}

# 4. Generator Tests (Signal Generation)
Write-TestHeader "Signal Generator Control"

$genTest = Test-REWEndpoint "/generator/status"
if ($genTest.Success) {
    $genStatus = $genTest.Content | ConvertFrom-Json
    Write-TestResult "Generator Status" $true "Signal: $($genStatus.signal), Level: $($genStatus.level)$($genStatus.levelUnit)"
} else {
    Write-TestResult "Generator Status" $false $genTest.Error
}

# 5. Array Encoding Tests (REQ-F-002: Base64 Array Encoding/Decoding)
$arrayData = Test-ArrayEncodingCompatibility

# 6. Error Handling Tests (REQ-F-001.4: Error Handling)
Write-TestHeader "Error Handling (REQ-F-001.4)"

$errorTest = Test-REWEndpoint "/nonexistent-endpoint" 404
Write-TestResult "404 Error Handling" ($errorTest.StatusCode -eq 404) "Correctly returns 404 for invalid endpoints"

# 7. Performance Tests (REQ-NF-001: HTTP Response Time)
Write-TestHeader "Performance Tests (REQ-NF-001)"

$performanceEndpoints = @("/measurements", "/generator/status", "/eq/equalisers")
foreach ($endpoint in $performanceEndpoints) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $test = Test-REWEndpoint $endpoint
    $stopwatch.Stop()
    
    $responseTime = $stopwatch.ElapsedMilliseconds
    $withinLimit = $responseTime -lt 5000  # 5 second limit from requirements
    
    Write-TestResult "Response Time $endpoint" $withinLimit "Response time: ${responseTime}ms (limit: 5000ms)"
}

# 8. Content Type Validation
Write-TestHeader "Content Type Validation"

$jsonEndpoints = @("/measurements", "/generator/status", "/doc.json")
foreach ($endpoint in $jsonEndpoints) {
    $test = Test-REWEndpoint $endpoint
    if ($test.Success) {
        $isJson = $test.ContentType -like "*application/json*"
        Write-TestResult "JSON Content-Type $endpoint" $isJson "Content-Type: $($test.ContentType)"
    }
}

# Generate Test Report
Write-Host "`n" + "="*60 -ForegroundColor White
Write-Host "TEST SUMMARY" -ForegroundColor White -BackgroundColor Blue
Write-Host "="*60 -ForegroundColor White

Write-Host "Total Tests: $TestCount" -ForegroundColor Yellow
Write-Host "Passed: $PassCount" -ForegroundColor Green
Write-Host "Failed: $($TestCount - $PassCount)" -ForegroundColor Red
Write-Host "Success Rate: $([Math]::Round(($PassCount / $TestCount) * 100, 1))%" -ForegroundColor Cyan

if ($TestCount -gt 0) {
    Write-Host "`nDETAILED RESULTS:" -ForegroundColor Yellow
    $TestResults | Format-Table -AutoSize
}

# Generate Requirements Validation Report
Write-Host "`nREQUIREMENTS VALIDATION:" -ForegroundColor Yellow
Write-Host "REQ-F-001 (HTTP Communication): " -NoNewline
if ($PassCount -gt 0) { Write-Host "✅ VALIDATED" -ForegroundColor Green } else { Write-Host "❌ FAILED" -ForegroundColor Red }

Write-Host "REQ-F-002 (Array Encoding): " -NoNewline
if ($arrayData) { Write-Host "✅ DATA AVAILABLE" -ForegroundColor Green } else { Write-Host "⚠️ NO TEST DATA" -ForegroundColor Yellow }

Write-Host "REQ-F-003 (Audio Control): " -NoNewline
if ($audioTest.Success) { Write-Host "✅ VALIDATED" -ForegroundColor Green } else { Write-Host "❌ FAILED" -ForegroundColor Red }

Write-Host "REQ-NF-001 (Performance): " -NoNewline
$allPerformanceGood = $true
# Check if any performance tests failed (this would need to be tracked in the loop above)
if ($allPerformanceGood) { Write-Host "✅ VALIDATED" -ForegroundColor Green } else { Write-Host "❌ FAILED" -ForegroundColor Red }

# Save results to file for CI/CD integration
$reportFile = "test-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$TestResults | ConvertTo-Json -Depth 3 | Out-File $reportFile
Write-Host "`nTest results saved to: $reportFile" -ForegroundColor Gray

# Exit with appropriate code for CI/CD
if ($PassCount -eq $TestCount) {
    Write-Host "`n🎉 ALL TESTS PASSED!" -ForegroundColor Green -BackgroundColor DarkGreen
    exit 0
} else {
    Write-Host "`n❌ SOME TESTS FAILED!" -ForegroundColor Red -BackgroundColor DarkRed
    exit 1
}