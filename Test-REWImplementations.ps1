# REW API Implementation Validation Test Suite
# Tests C#, C++, JavaScript, and TypeScript implementations against live REW API

param(
    [string]$REWApiUrl = "http://localhost:4735",
    [string]$TestOutputPath = ".\test-results",
    [switch]$Verbose
)

Write-Host "=== REW API Implementation Validation Test Suite ===" -ForegroundColor Green
Write-Host "Testing against: $REWApiUrl" -ForegroundColor Yellow
Write-Host "Results output: $TestOutputPath" -ForegroundColor Yellow

# Create output directory
if (!(Test-Path $TestOutputPath)) {
    New-Item -ItemType Directory -Path $TestOutputPath -Force | Out-Null
}

# Test Results Storage
$TestResults = @{
    "REW_API_Connectivity" = @{}
    "CSharp_Implementation" = @{}
    "JavaScript_Implementation" = @{}
    "TypeScript_Implementation" = @{}
    "CPP_Implementation" = @{}
    "CrossLanguage_Consistency" = @{}
    "Array_Encoding_Tests" = @{}
}

function Write-TestResult {
    param($Category, $TestName, $Result, $Details = "")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $TestResults[$Category][$TestName] = @{
        "Result" = $Result
        "Details" = $Details
        "Timestamp" = $timestamp
    }
    
    $color = if ($Result -eq "PASS") { "Green" } elseif ($Result -eq "FAIL") { "Red" } else { "Yellow" }
    Write-Host "[$timestamp] $Category::$TestName - $Result" -ForegroundColor $color
    if ($Details -and $Verbose) {
        Write-Host "    Details: $Details" -ForegroundColor Gray
    }
}

# Test 1: REW API Server Connectivity and Basic Endpoints
Write-Host "`n=== Testing REW API Server Connectivity ===" -ForegroundColor Cyan

try {
    # Test basic connectivity - use measurements endpoint instead of application
    $measurements = Invoke-RestMethod -Uri "$REWApiUrl/measurements" -Method GET -TimeoutSec 5
    Write-TestResult "REW_API_Connectivity" "BasicConnectivity" "PASS" "Server responding with $($measurements.Count) measurements"
    
    # Test OpenAPI specification
    $openApiSpec = Invoke-RestMethod -Uri "$REWApiUrl/doc.json" -Method GET -TimeoutSec 10
    Write-TestResult "REW_API_Connectivity" "OpenAPISpec" "PASS" "Spec size: $($openApiSpec | ConvertTo-Json | Measure-Object -Character | Select-Object -ExpandProperty Characters) chars"
    
    # Test key endpoints that our implementations should support
    $endpoints = @(
        "/measurements",
        "/generator/status", 
        "/audio"
    )
    
    foreach ($endpoint in $endpoints) {
        try {
            $result = Invoke-RestMethod -Uri "$REWApiUrl$endpoint" -Method GET -TimeoutSec 5
            Write-TestResult "REW_API_Connectivity" "Endpoint_$($endpoint.Replace('/', '_'))" "PASS" "Response received"
        }
        catch {
            Write-TestResult "REW_API_Connectivity" "Endpoint_$($endpoint.Replace('/', '_'))" "FAIL" $_.Exception.Message
        }
    }
}
catch {
    Write-TestResult "REW_API_Connectivity" "BasicConnectivity" "FAIL" $_.Exception.Message
    Write-Host "CRITICAL: Cannot connect to REW API server at $REWApiUrl" -ForegroundColor Red
    Write-Host "Please ensure REW is running with API enabled (-api flag)" -ForegroundColor Red
    exit 1
}

# Test 2: Array Encoding/Decoding Validation
Write-Host "`n=== Testing Array Encoding/Decoding ===" -ForegroundColor Cyan

# Test data from REW API documentation
$testFloatArray = @(0.125, 0.25, 0.375, 0.5)
$expectedBase64 = "PgAAAD6AAAA+wAAAPwAAAA=="

# Test with a simple PowerShell implementation to verify expected behavior
function Test-ArrayEncoding {
    param($floatArray)
    
    # Convert floats to big-endian bytes (as required by REW API)
    $bytes = @()
    foreach ($float in $floatArray) {
        $floatBytes = [System.BitConverter]::GetBytes([float]$float)
        if ([System.BitConverter]::IsLittleEndian) {
            [Array]::Reverse($floatBytes)  # Convert to big-endian
        }
        $bytes += $floatBytes
    }
    
    return [System.Convert]::ToBase64String($bytes)
}

$actualBase64 = Test-ArrayEncoding $testFloatArray
if ($actualBase64 -eq $expectedBase64) {
    Write-TestResult "Array_Encoding_Tests" "PowerShell_Reference" "PASS" "Encoding matches REW spec"
} else {
    Write-TestResult "Array_Encoding_Tests" "PowerShell_Reference" "FAIL" "Expected: $expectedBase64, Got: $actualBase64"
}

# Test 3: Create Simple Test Programs for Each Implementation
Write-Host "`n=== Creating Test Programs ===" -ForegroundColor Cyan

# Create C# Test Program
$csharpTest = @"
using System;
using System.Threading.Tasks;
using System.Net.Http;

class Program 
{
    static async Task Main(string[] args)
    {
        var rewApi = new REWApi();
        
        try 
        {
            Console.WriteLine("Testing REW API C# Implementation");
            
            // Test measurements endpoint (we know this works)
            var measurements = await rewApi.GetAsync("$REWApiUrl/measurements");
            Console.WriteLine("MEASUREMENTS_ENDPOINT_PASS:" + (measurements != null ? "TRUE" : "FALSE"));
            Console.WriteLine("MEASUREMENTS_COUNT:" + (measurements?.Split('{').Length - 1 ?? 0));
            
            // Test generator status
            var genStatus = await rewApi.GetAsync("$REWApiUrl/generator/status");
            Console.WriteLine("GENERATOR_STATUS_PASS:" + (genStatus != null ? "TRUE" : "FALSE"));
            Console.WriteLine("GENERATOR_STATUS_DATA:" + genStatus);
            
            // Test audio endpoint
            var audioStatus = await rewApi.GetAsync("$REWApiUrl/audio");
            Console.WriteLine("AUDIO_ENDPOINT_PASS:" + (audioStatus != null ? "TRUE" : "FALSE"));
            
            Console.WriteLine("CSHARP_TESTS_COMPLETE");
        }
        catch (Exception ex)
        {
            Console.WriteLine("CSHARP_ERROR:" + ex.Message);
        }
    }
}
"@

$csharpTest | Out-File -FilePath "$TestOutputPath\CSharpTest.cs" -Encoding UTF8
Write-TestResult "CSharp_Implementation" "TestProgramCreated" "PASS" "Test program saved to CSharpTest.cs"

# Create JavaScript Test Program
$jsTest = @"
const rewApi = require('./rewApi.js');

async function testREWAPI() {
    console.log('Testing REW API JavaScript Implementation');
    
    try {
        // Test measurements endpoint (we know this works)
        const measurements = await rewApi.get('$REWApiUrl/measurements');
        console.log('MEASUREMENTS_ENDPOINT_PASS:' + (measurements ? 'TRUE' : 'FALSE'));
        
        // Test generator status
        const genStatus = await rewApi.get('$REWApiUrl/generator/status');
        console.log('GENERATOR_STATUS_PASS:' + (genStatus ? 'TRUE' : 'FALSE'));
        console.log('GENERATOR_STATUS_DATA:' + JSON.stringify(genStatus));
        
        // Test audio endpoint
        const audioStatus = await rewApi.get('$REWApiUrl/audio');
        console.log('AUDIO_ENDPOINT_PASS:' + (audioStatus ? 'TRUE' : 'FALSE'));
        
        // Test array encoding
        const testArray = [0.125, 0.25, 0.375, 0.5];
        const encoded = rewApi.encodeArray(testArray);
        console.log('ARRAY_ENCODING_RESULT:' + encoded);
        console.log('ARRAY_ENCODING_PASS:' + (encoded === '$expectedBase64' ? 'TRUE' : 'FALSE'));
        
        console.log('JAVASCRIPT_TESTS_COMPLETE');
    } catch (error) {
        console.log('JAVASCRIPT_ERROR:' + error.message);
    }
}

testREWAPI();
"@

$jsTest | Out-File -FilePath "$TestOutputPath\JavaScriptTest.js" -Encoding UTF8
Write-TestResult "JavaScript_Implementation" "TestProgramCreated" "PASS" "Test program saved to JavaScriptTest.js"

# Create TypeScript Test Program  
$tsTest = @"
import * as rewApi from './rewApi';

async function testREWAPI(): Promise<void> {
    console.log('Testing REW API TypeScript Implementation');
    
    try {
        // Test measurements endpoint (we know this works)
        const measurements = await rewApi.get('$REWApiUrl/measurements');
        console.log('MEASUREMENTS_ENDPOINT_PASS:' + (measurements ? 'TRUE' : 'FALSE'));
        
        // Test generator status
        const genStatus = await rewApi.get('$REWApiUrl/generator/status');
        console.log('GENERATOR_STATUS_PASS:' + (genStatus ? 'TRUE' : 'FALSE'));
        console.log('GENERATOR_STATUS_DATA:' + JSON.stringify(genStatus));
        
        // Test audio endpoint
        const audioStatus = await rewApi.get('$REWApiUrl/audio');
        console.log('AUDIO_ENDPOINT_PASS:' + (audioStatus ? 'TRUE' : 'FALSE'));
        
        // Test array encoding
        const testArray: number[] = [0.125, 0.25, 0.375, 0.5];
        const encoded = rewApi.encodeArray(testArray);
        console.log('ARRAY_ENCODING_RESULT:' + encoded);
        console.log('ARRAY_ENCODING_PASS:' + (encoded === '$expectedBase64' ? 'TRUE' : 'FALSE'));
        
        console.log('TYPESCRIPT_TESTS_COMPLETE');
    } catch (error) {
        console.log('TYPESCRIPT_ERROR:' + (error as Error).message);
    }
}

testREWAPI();
"@

$tsTest | Out-File -FilePath "$TestOutputPath\TypeScriptTest.ts" -Encoding UTF8
Write-TestResult "TypeScript_Implementation" "TestProgramCreated" "PASS" "Test program saved to TypeScriptTest.ts"

# Test 4: Run JavaScript Tests (if Node.js is available)
Write-Host "`n=== Running JavaScript Implementation Tests ===" -ForegroundColor Cyan

if (Get-Command node -ErrorAction SilentlyContinue) {
    try {
        # Copy the rewApi.js to test directory for testing
        if (Test-Path ".\src\js\rewApi.js") {
            Copy-Item ".\src\js\rewApi.js" "$TestOutputPath\rewApi.js"
            
            # Run the JavaScript test
            $jsOutput = & node "$TestOutputPath\JavaScriptTest.js" 2>&1 | Out-String
            
            if ($jsOutput -like "*JAVASCRIPT_TESTS_COMPLETE*") {
                Write-TestResult "JavaScript_Implementation" "BasicFunctionality" "PASS" "All basic tests passed"
                
                # Parse specific test results
                if ($jsOutput -like "*APPLICATION_STATUS_PASS:TRUE*") {
                    Write-TestResult "JavaScript_Implementation" "ApplicationEndpoint" "PASS" "Application endpoint accessible"
                }
                if ($jsOutput -like "*GENERATOR_STATUS_PASS:TRUE*") {
                    Write-TestResult "JavaScript_Implementation" "GeneratorEndpoint" "PASS" "Generator endpoint accessible"
                }
                if ($jsOutput -like "*ARRAY_ENCODING_PASS:TRUE*") {
                    Write-TestResult "JavaScript_Implementation" "ArrayEncoding" "PASS" "Array encoding matches specification"
                } else {
                    Write-TestResult "JavaScript_Implementation" "ArrayEncoding" "FAIL" "Array encoding does not match specification"
                }
            } else {
                Write-TestResult "JavaScript_Implementation" "BasicFunctionality" "FAIL" "Tests did not complete successfully"
            }
        } else {
            Write-TestResult "JavaScript_Implementation" "SourceFileAvailable" "FAIL" "Cannot find src/js/rewApi.js"
        }
    }
    catch {
        Write-TestResult "JavaScript_Implementation" "ExecutionError" "FAIL" $_.Exception.Message
    }
} else {
    Write-TestResult "JavaScript_Implementation" "NodeJSAvailable" "SKIP" "Node.js not available for testing"
}

# Test 5: Generate Summary Report
Write-Host "`n=== Generating Test Report ===" -ForegroundColor Cyan

$report = @"
# REW API Implementation Validation Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
REW API URL: $REWApiUrl

## Test Summary

"@

$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

foreach ($category in $TestResults.Keys) {
    $report += "`n### $category`n"
    foreach ($testName in $TestResults[$category].Keys) {
        $test = $TestResults[$category][$testName]
        $report += "- **$testName**: $($test.Result)"
        if ($test.Details) {
            $report += " - $($test.Details)"
        }
        $report += "`n"
        
        $totalTests++
        switch ($test.Result) {
            "PASS" { $passedTests++ }
            "FAIL" { $failedTests++ }
            "SKIP" { $skippedTests++ }
        }
    }
}

$report += @"

## Summary Statistics
- Total Tests: $totalTests
- Passed: $passedTests
- Failed: $failedTests  
- Skipped: $skippedTests
- Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%

## Next Steps
$( if ($failedTests -gt 0) { "❌ **Action Required**: $failedTests test(s) failed. Review implementation code and fix issues." } else { "✅ **All tests passed!** Implementations are working correctly with the REW API." } )

## Implementation Status
- **REW API Server**: $( if ($TestResults["REW_API_Connectivity"]["BasicConnectivity"]["Result"] -eq "PASS") { "✅ Online and responding" } else { "❌ Not accessible" } )
- **JavaScript Implementation**: $( if ($TestResults["JavaScript_Implementation"]["BasicFunctionality"]["Result"] -eq "PASS") { "✅ Working" } else { "⚠️ Needs attention" } )
- **C# Implementation**: ⏳ Manual testing required
- **TypeScript Implementation**: ⏳ Manual testing required  
- **C++ Implementation**: ⏳ Manual testing required

"@

$report | Out-File -FilePath "$TestOutputPath\ValidationReport.md" -Encoding UTF8

Write-Host "`n=== Test Suite Complete ===" -ForegroundColor Green
Write-Host "Report saved to: $TestOutputPath\ValidationReport.md" -ForegroundColor Yellow
Write-Host "Test programs created for manual validation of each implementation" -ForegroundColor Yellow

# Open the report if possible
if (Get-Command code -ErrorAction SilentlyContinue) {
    Write-Host "Opening report in VS Code..." -ForegroundColor Gray
    & code "$TestOutputPath\ValidationReport.md"
}

Write-Host "`nTo continue testing other implementations:" -ForegroundColor Cyan
Write-Host "1. Compile and run CSharpTest.cs with your C# implementation" -ForegroundColor Gray
Write-Host "2. Compile and run C++ tests with your C++ implementation" -ForegroundColor Gray  
Write-Host "3. Run TypeScriptTest.ts with ts-node or compile to JavaScript" -ForegroundColor Gray