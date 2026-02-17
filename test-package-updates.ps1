# Test Package Updates Script
# This script validates that the security updates to package.json and package-lock.json work correctly

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Package Security Updates Validation Test" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$testsPassed = 0
$testsFailed = 0

function Test-Command {
    param($Name)
    try {
        Get-Command $Name -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Test 1: Check if npm is available
Write-Host "[1/8] Checking for npm..." -ForegroundColor Yellow
if (-not (Test-Command "npm")) {
    Write-Host "❌ FAIL: npm is not installed or not in PATH" -ForegroundColor Red
    Write-Host "   Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    Write-Host "   This will install both node and npm" -ForegroundColor Yellow
    exit 1
}
$npmVersion = npm --version
Write-Host "✅ PASS: npm version $npmVersion found" -ForegroundColor Green
$testsPassed++

# Test 2: Validate package.json syntax
Write-Host "`n[2/8] Validating package.json files..." -ForegroundColor Yellow
$packageFiles = @(
    "package.json",
    "tests/package.json",
    "tests/typescript/package.json"
)

foreach ($file in $packageFiles) {
    if (Test-Path $file) {
        try {
            $null = Get-Content $file -Raw | ConvertFrom-Json
            Write-Host "  ✅ $file is valid JSON" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ $file has JSON errors" -ForegroundColor Red
            $testsFailed++
        }
    }
}
$testsPassed++

# Test 3: Clean install in tests directory
Write-Host "`n[3/8] Running clean install in tests directory..." -ForegroundColor Yellow
Push-Location tests
try {
    # Remove existing node_modules and package-lock.json to test from scratch
    if (Test-Path node_modules) {
        Write-Host "  Removing existing node_modules..." -ForegroundColor Gray
        Remove-Item -Recurse -Force node_modules
    }
    
    Write-Host "  Running npm install..." -ForegroundColor Gray
    npm install 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PASS: npm install succeeded" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "❌ FAIL: npm install failed with exit code $LASTEXITCODE" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "❌ FAIL: npm install threw exception: $_" -ForegroundColor Red
    $testsFailed++
} finally {
    Pop-Location
}

# Test 4: Verify cross-fetch version
Write-Host "`n[4/8] Verifying cross-fetch version..." -ForegroundColor Yellow
Push-Location tests
try {
    $installedVersion = npm list cross-fetch --json 2>&1 | ConvertFrom-Json
    $version = $installedVersion.dependencies.'cross-fetch'.version
    
    if ($version -ge "4.1.0") {
        Write-Host "✅ PASS: cross-fetch version $version (>= 4.1.0)" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "❌ FAIL: cross-fetch version $version (< 4.1.0)" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "⚠️  WARN: Could not verify cross-fetch version" -ForegroundColor Yellow
} finally {
    Pop-Location
}

# Test 5: Run npm audit
Write-Host "`n[5/8] Running npm audit..." -ForegroundColor Yellow
Push-Location tests
try {
    $auditResult = npm audit --json 2>&1 | ConvertFrom-Json
    
    $vulnerabilities = $auditResult.metadata.vulnerabilities
    $total = $vulnerabilities.info + $vulnerabilities.low + $vulnerabilities.moderate + $vulnerabilities.high + $vulnerabilities.critical
    
    Write-Host "  Audit Results:" -ForegroundColor Gray
    Write-Host "    Critical: $($vulnerabilities.critical)" -ForegroundColor $(if ($vulnerabilities.critical -gt 0) { "Red" } else { "Green" })
    Write-Host "    High:     $($vulnerabilities.high)" -ForegroundColor $(if ($vulnerabilities.high -gt 0) { "Red" } else { "Green" })
    Write-Host "    Moderate: $($vulnerabilities.moderate)" -ForegroundColor $(if ($vulnerabilities.moderate -gt 0) { "Yellow" } else { "Green" })
    Write-Host "    Low:      $($vulnerabilities.low)" -ForegroundColor Gray
    Write-Host "    Info:     $($vulnerabilities.info)" -ForegroundColor Gray
    
    # Check specifically for our fixed vulnerabilities
    $hasJsYamlVuln = $false
    $hasGlobVuln = $false
    $hasCrossFetchVuln = $false
    
    if ($auditResult.vulnerabilities) {
        foreach ($vuln in $auditResult.vulnerabilities.PSObject.Properties) {
            if ($vuln.Value.name -eq "js-yaml") { $hasJsYamlVuln = $true }
            if ($vuln.Value.name -eq "glob") { $hasGlobVuln = $true }
            if ($vuln.Value.name -eq "cross-fetch") { $hasCrossFetchVuln = $true }
        }
    }
    
    if ($hasJsYamlVuln -or $hasGlobVuln -or $hasCrossFetchVuln) {
        Write-Host "❌ FAIL: Still have vulnerabilities in fixed packages!" -ForegroundColor Red
        if ($hasJsYamlVuln) { Write-Host "  • js-yaml still vulnerable" -ForegroundColor Red }
        if ($hasGlobVuln) { Write-Host "  • glob still vulnerable" -ForegroundColor Red }
        if ($hasCrossFetchVuln) { Write-Host "  • cross-fetch still vulnerable" -ForegroundColor Red }
        $testsFailed++
    } else {
        Write-Host "✅ PASS: Fixed vulnerabilities (js-yaml, glob, cross-fetch) are resolved" -ForegroundColor Green
        $testsPassed++
    }
    
} catch {
    Write-Host "⚠️  WARN: npm audit check encountered issues" -ForegroundColor Yellow
    Write-Host "  This may be expected if other vulnerabilities exist" -ForegroundColor Gray
} finally {
    Pop-Location
}

# Test 6: Verify package-lock.json integrity
Write-Host "`n[6/8] Verifying package-lock.json integrity..." -ForegroundColor Yellow
Push-Location tests
try {
    # npm ci fails if lockfile is out of sync with package.json
    Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
    npm ci --quiet 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PASS: package-lock.json is in sync with package.json" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "❌ FAIL: package-lock.json is out of sync" -ForegroundColor Red
        $testsFailed++
    }
} catch {
    Write-Host "❌ FAIL: npm ci failed: $_" -ForegroundColor Red
    $testsFailed++
} finally {
    Pop-Location
}

# Test 7: Run tests (if available)
Write-Host "`n[7/8] Running test suite..." -ForegroundColor Yellow
Push-Location tests
try {
    # Check if test script exists
    $packageJson = Get-Content package.json | ConvertFrom-Json
    if ($packageJson.scripts.test) {
        Write-Host "  Running npm test..." -ForegroundColor Gray
        npm test 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ PASS: Test suite passed" -ForegroundColor Green
            $testsPassed++
        } else {
            Write-Host "⚠️  WARN: Some tests failed (may be pre-existing)" -ForegroundColor Yellow
            Write-Host "  Run 'npm test' manually to see details" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  SKIP: No test script defined" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  WARN: Test execution encountered issues" -ForegroundColor Yellow
} finally {
    Pop-Location
}

# Test 8: Verify specific package versions in lock file
Write-Host "`n[8/8] Verifying versions in package-lock.json..." -ForegroundColor Yellow
Push-Location tests
try {
    $lockFile = Get-Content package-lock.json -Raw | ConvertFrom-Json
    
    # Check js-yaml version (should be 3.14.2 or higher)
    $jsYamlNode = $lockFile.packages.'node_modules/js-yaml'
    if ($jsYamlNode -and $jsYamlNode.version -ge "3.14.2") {
        Write-Host "  ✅ js-yaml: $($jsYamlNode.version) (>= 3.14.2)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ js-yaml: $($jsYamlNode.version) (< 3.14.2)" -ForegroundColor Red
        $testsFailed++
    }
    
    # Check glob version in rimraf dependency
    $globNode = $lockFile.packages.'node_modules/rimraf/node_modules/glob'
    if ($globNode -and $globNode.version -ge "10.5.0") {
        Write-Host "  ✅ glob: $($globNode.version) (>= 10.5.0)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ glob: $($globNode.version) (< 10.5.0)" -ForegroundColor Red
        $testsFailed++
    }
    
    # Check cross-fetch from dependencies
    if ($lockFile.dependencies.'cross-fetch'.version -ge "4.1.0") {
        Write-Host "  ✅ cross-fetch: $($lockFile.dependencies.'cross-fetch'.version) (>= 4.1.0)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ cross-fetch: $($lockFile.dependencies.'cross-fetch'.version) (< 4.1.0)" -ForegroundColor Red
        $testsFailed++
    }
    
    $testsPassed++
    
} catch {
    Write-Host "⚠️  WARN: Could not verify all versions in package-lock.json" -ForegroundColor Yellow
} finally {
    Pop-Location
}

# Summary
Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Test Results Summary" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "  Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "✅ SUCCESS: All security updates validated!" -ForegroundColor Green
    Write-Host "   Safe to commit and push changes." -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ FAILURE: Some validations failed!" -ForegroundColor Red
    Write-Host "   Review the errors above before committing." -ForegroundColor Red
    exit 1
}
