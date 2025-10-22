# Generator Service Implementation

**Status**: ✅ COMPLETE - 26/26 tests passing  
**Implementation Date**: October 2024  
**Service**: GeneratorService.cs  
**Integration**: RewClient.Generator property

## Overview

The Generator API service provides comprehensive control over REW's signal generator, including signal selection, level control, frequency configuration, and playback management.

## Architecture

### Domain Objects

1. **GeneratorStatus**
   - Properties: `Enabled`, `Playing`, `Signal`, `Level`, `LevelUnit`
   - Methods: `CanStart()`, `CanStop()`, `CanChangeSettings()`
   - Endpoint: `GET /generator/status`

2. **GeneratorConfiguration**
   - Signal-specific configuration (frequency, harmonics, noise color, dither, etc.)
   - Methods: `IsValidForSine()`, `IsValidForSquare()`, `IsValidForNoise()`
   - Endpoints: `GET/POST /generator/signal/configuration`

3. **GeneratorProtectionSettings**
   - Properties: `SplLimitAbort`, `ClippingAbort`, `DbSPLLimit`
   - Safety limits for generator output
   - Endpoints: `GET/POST /generator/protection`

### JSON Wrapper Objects

The REW Generator API returns wrapped objects instead of primitives, requiring special response classes:

```csharp
// Signal wrapper
public class GeneratorSignalResponse
{
    [JsonPropertyName("signal")]
    public string Signal { get; set; }
}

// Level wrapper  
public class GeneratorLevelResponse
{
    [JsonPropertyName("value")]
    public double Value { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}

// Frequency wrapper
public class GeneratorFrequencyResponse
{
    [JsonPropertyName("value")]  // NOT "frequency"!
    public double Value { get; set; }
    
    [JsonPropertyName("unit")]
    public string Unit { get; set; }
}
```

**Critical Discovery**: The frequency endpoint uses the generic `Value` definition (same as level), not a custom `Frequency` property. This was discovered through OpenAPI spec analysis and manual testing.

## Implementation Highlights

### HTTP Method Corrections

The HTML documentation was incorrect about HTTP methods. OpenAPI spec revealed:

- **Signal Selection**: `POST /generator/signal` (documentation claimed PUT)
- **Commands**: `POST /generator/command` (singular, not /commands plural)
- **Frequency Setting**: `POST /generator/frequency`

### Property Naming Discoveries

Several property names differed from expectations:

1. **Protection Settings**: `splLimitAbort`, `clippingAbort`, `dBSPLLimit` (camelCase with specific capitalization)
2. **Commands**: "Play" and "Stop" (not "Start"/"Stop")
3. **Frequency**: Uses "value" property (not "frequency")

### Conditional Property Handling

The `/generator/frequency` endpoint behavior varies by signal type:

```json
// When frequency is applicable (sine, square)
GET /generator/frequency → {"value": 1000.0, "unit": "Hz"}

// When frequency is NOT applicable (pink noise)
GET /generator/frequency → {"unit": "Hz"}  // No "value" property!
```

The service handles this gracefully:
```csharp
return wrapper?.Value ?? 0.0;  // Returns 0.0 for missing property
```

## REW Pro License Requirement

**Important**: While GET endpoints work with the free version, **Play/Stop commands require a REW Pro upgrade license** to actually control generator playback. Without Pro:

- Commands return `200 OK` success responses
- But generator `playing` status remains `false`
- No actual playback occurs

This is documented in the REW API help:
> "PUT and POST are also supported by default for most endpoints, but to control REW via the API to make automated sweep measurements requires a Pro upgrade license."

Tests that verify playback state may fail on non-Pro installations. The API implementation is correct; this is a REW licensing limitation.

## API Coverage

### Endpoints Implemented (25+ methods)

**Status & Control**:
- `GET /generator/status` → `GetStatus()`
- `GET /generator/commands` → `GetCommands()`
- `POST /generator/command` → `Start()`, `Stop()`, `SendCommand()`

**Signal Management**:
- `GET /generator/signal` → `GetSignal()`
- `POST /generator/signal` → `SetSignal()`
- `GET /generator/signals` → `GetAvailableSignals()`
- `GET /generator/signal/configuration` → `GetConfiguration()`
- `POST /generator/signal/configuration` → `UpdateConfiguration()`
- `GET /generator/signal/commands` → `GetSignalCommands()`
- `POST /generator/signal/command` → `SendSignalCommand()`

**Level Control**:
- `GET /generator/level` → `GetLevel()`
- `POST /generator/level` → `SetLevel()`
- `GET /generator/level/units` → `GetAvailableLevelUnits()`

**Frequency Control**:
- `GET /generator/frequency` → `GetFrequency()`
- `POST /generator/frequency` → `SetFrequency()`

**Output Configuration**:
- `GET /generator/output/invert2nd` → `GetInvertSecondOutput()`
- `POST /generator/output/invert2nd` → `SetInvertSecondOutput()`

**Protection Settings**:
- `GET /generator/protection` → `GetProtection()`
- `POST /generator/protection` → `SetProtection()`

## Test Coverage

**26 tests across 6 categories**:

1. **Domain Objects** (6 tests) - Domain model validation
2. **Status/Control** (4 tests) - Start/stop, status queries
3. **Signal Selection** (6 tests) - Signal types and availability
4. **Level/Frequency** (6 tests) - Level and frequency configuration
5. **Output Control** (2 tests) - Output routing and inversion
6. **Protection** (2 tests) - Safety limits configuration

### Notable Test Cases

**SineWaveWorkflow_ShouldConfigureAndPlayTestTone**:
```csharp
// Complete workflow: Select sine → Set frequency → Set level → Verify
await _generatorService.SetSignal("sine");
await _generatorService.SetFrequency(1000.0);  // 1 kHz
await _generatorService.SetLevel(-30.0);       // -30 dBFS
// Verifies all configuration steps succeed
```

**PinkNoiseWorkflow_ShouldConfigureAndPlayNoise**:
```csharp
// Configure pink noise with dither
var config = new GeneratorConfiguration
{
    AddDither = true,
    DitherBits = 16,
    NoiseColor = "Pink"
};
await _generatorService.UpdateConfiguration(config);
```

## Integration Example

```csharp
using var client = new RewClient("http://localhost:4735");

// Get current generator status
var status = await client.Generator.GetStatus();
Console.WriteLine($"Generator: {status.Signal} at {status.Level} {status.LevelUnit}");

// Configure 1 kHz sine wave at -20 dBFS
await client.Generator.SetSignal("sine");
await client.Generator.SetFrequency(1000.0);
await client.Generator.SetLevel(-20.0);

// Start playback (requires REW Pro)
await client.Generator.Start();
```

## Lessons Learned

1. **Always verify OpenAPI spec** - HTML documentation can be outdated
2. **Manual testing is essential** - Curl/PowerShell tests revealed property naming issues
3. **Handle conditional properties** - Some API responses omit properties based on context
4. **Wrapper objects required** - REW API often wraps primitives in objects
5. **License limitations exist** - Not all endpoints work without Pro upgrade
6. **Iterative debugging works** - Each fix cycle improved test pass rate: 13→20→21→22→26

## Performance Metrics

- **Initial implementation**: 13/26 tests passing
- **After wrapper objects**: 20/26 tests passing
- **After HTTP method fixes**: 21/26 tests passing
- **After endpoint name fix**: 22/26 tests passing
- **After frequency property fix**: 26/26 tests passing ✅

Total debugging iterations: 5  
Time to completion: ~3 hours of iterative refinement

## References

- [REW Generator API Documentation](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#generator)
- OpenAPI Spec: `http://localhost:4735/doc.json`
- Architecture Decision: ADR-004 (Domain-Driven Service Architecture)
- Test Suite: `tests/csharp/GeneratorServiceTests.cs`
