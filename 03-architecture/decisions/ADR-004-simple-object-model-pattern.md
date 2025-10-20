# ADR-004: Battle-Tested Domain Service Architecture

## Status

**Accepted** (2024-12-19)  
**Supersedes**: Previous simple object model approach

## Context

The REW API client library needs a robust, production-ready architecture that properly encapsulates REW's API complexities while providing a clean domain-oriented interface across multiple programming languages (C#, C++, JavaScript, TypeScript).

### Problems with Previous Approach

1. **JSON Exposure**: Previous design exposed JSON objects and HTTP details to users
2. **No REW-Specific Handling**: Failed to address REW's unique constraints (single-command queue, Base64 arrays, 200/202 responses)
3. **Poor Encapsulation**: Domain logic mixed with transport concerns
4. **Maintenance Burden**: Each language implemented its own patterns inconsistently

### REW API Characteristics Requiring Special Handling

- **Single Command Constraint**: Only one POST command can run at a time
- **Mixed Response Types**: 200 (immediate) vs 202 (long-running) responses  
- **Base64 Big-Endian Arrays**: Frequency/time data encoded as Base64 big-endian floats
- **Subscription Model**: Server push notifications via callback URLs
- **Resource-Centric API**: Clear endpoint groupings (/application, /audio, /measurements, etc.)

## Decision

We will implement a **Battle-Tested Layered Architecture** with proper domain services that hide all HTTP/JSON complexity:

### Core Architecture Principles

1. **Layered Architecture**: Clean separation between Domain, Gateway, and Transport layers
2. **Resource-Centric Services**: One service per REW API resource tree
3. **Domain Object Encapsulation**: Rich objects with behavior, not data containers
4. **REW-Specific Infrastructure**: Handle REW's unique constraints transparently
5. **Cross-Language Consistency**: Identical domain interfaces across all languages

### Implementation Pattern

**C# (Production Pattern)**:

```csharp
// Domain service usage - NO JSON EXPOSURE
var rew = await RewClient.ConnectAsync("localhost:4735");
await rew.Application.EnableBlocking(true);
await rew.Audio.ConfigureJava("Built-in Input", "Built-in Output", 48000);

var result = await rew.Measurements.RunSweepAndExport(new SweepConfig
{
    StartFreq = 20,
    EndFreq = 20000,
    Length = 512,
    OutputPath = "room_response.mdat"
});

// Long-running operations with proper async handling
var job = await rew.Measurements.GenerateWaterfall(measurementId, new WaterfallOptions
{
    Mode = WaterfallMode.Fourier,
    Slices = 101,
    PPO = 48
});
var waterfallData = await job.AwaitCompletion();
```

**JavaScript/TypeScript (Matching Pattern)**:

```javascript
// Domain service usage - NO JSON EXPOSURE
const rew = await RewClient.connectAsync("localhost:4735");
await rew.application.enableBlocking(true);
await rew.audio.configureJava("Built-in Input", "Built-in Output", 48000);

const result = await rew.measurements.runSweepAndExport({
    startFreq: 20,
    endFreq: 20000,
    length: 512,
    outputPath: "room_response.mdat"
});

// Same async handling pattern
const job = await rew.measurements.generateWaterfall(measurementId, {
    mode: "Fourier",
    slices: 101,
    ppo: 48
});
const waterfallData = await job.awaitCompletion();
```

**C++ (Matching Pattern)**:

```cpp
// Domain service usage - NO JSON EXPOSURE
auto rew = RewClient::ConnectAsync("localhost:4735").get();
rew->Application()->EnableBlocking(true).get();
rew->Audio()->ConfigureJava("Built-in Input", "Built-in Output", 48000).get();

SweepConfig config;
config.startFreq = 20;
config.endFreq = 20000;
config.length = 512;
config.outputPath = "room_response.mdat";

auto result = rew->Measurements()->RunSweepAndExport(config).get();

// Same async handling pattern
WaterfallOptions options;
options.mode = WaterfallMode::Fourier;
options.slices = 101;
options.ppo = 48;

auto job = rew->Measurements()->GenerateWaterfall(measurementId, options).get();
auto waterfallData = job->AwaitCompletion().get();
```

## Rationale

### Advantages

1. **Cross-Language Consistency**: Identical method signatures and behavior across all languages
2. **Simplicity**: No complex inheritance hierarchies or validation frameworks
3. **REW Compliance**: Direct mapping to REW API JSON expectations
4. **Maintainability**: Single source of truth (JS/TS) for object model evolution
5. **Developer Experience**: Simple object creation without framework knowledge
6. **Performance**: Minimal serialization overhead

### Trade-offs

1. **Compile-time Validation**: C# loses compile-time type checking (mitigated by runtime validation and testing)
2. **IDE Support**: Reduced IntelliSense in some scenarios (mitigated by comprehensive documentation)
3. **Runtime Errors**: Type errors discovered at runtime rather than compile time (mitigated by comprehensive testing)

## Implementation Details

### Method Signature Standardization

All languages must implement identical method signatures:

```javascript
// Standard pattern (all languages follow this)
async updateEQSettings(data)
async updateAudioSettings(data) 
async issueMeasurementCommandWithParams(trackId, command, parameters)
async addMeasurement(data)
```

### JSON Serialization Requirements

- **C#**: Use `System.Text.Json.JsonSerializer.Serialize(data)` with appropriate property naming configuration
- **C++**: Use `web::json::value` with direct property assignment
- **JavaScript/TypeScript**: Use native `JSON.stringify(data)`
- **All**: Must produce byte-identical JSON for identical input objects

### Validation Strategy

- **Primary Validation**: JavaScript/TypeScript implementations provide comprehensive parameter validation
- **Secondary Validation**: Other languages may add optional validation but should match JS/TS error messages
- **REW API Validation**: Ultimate validation occurs at REW API layer

## Architecture Benefits

### 1. Clean Domain Separation

**Transport Layer** (Hidden from users):
- Auto-generated from OpenAPI specification  
- Handles HTTP, JSON serialization, retry logic
- REW protocol compliance (localhost:4735, specific headers)

**Gateway Layer** (REW-Specific Adaptors):
- `CommandGate`: Manages single-command constraint
- `LongRunningJob<T>`: Handles 200/202 response patterns
- `ArrayCodec`: Decodes REW's Base64 big-endian arrays
- `SubscriptionManager`: Manages server push notifications

**Domain Layer** (User Interface):
- `ApplicationService`, `AudioService`, `MeasurementsService`
- Resource-centric design matching REW API structure
- Rich domain objects with behavior, not data containers
- Complete encapsulation of REW complexities

### 2. REW-Specific Infrastructure

**Single Command Queue**:

```csharp
// CommandGate ensures only one POST operation at a time
await commandGate.ExecuteCommand(async () => 
{
    return await httpClient.PostAsync("/measurements/process", content);
});
```

**Long-Running Job Handling**:

```csharp
// Unified interface for immediate (200) vs long-running (202) operations
var job = await rew.Measurements.GenerateWaterfall(id, options);
var result = await job.AwaitCompletion(progressReporter);
```

**Array Decoding**:

```csharp
// Base64 big-endian float arrays decoded transparently
var frequencies = await measurementService.GetFrequencyResponse(id);
float[] magnitudes = frequencies.DecodeMagnitudes(); // Ready for analysis
```

### 3. Cross-Language Consistency

All languages provide identical domain interfaces:

**Resource Access Pattern** (Identical across languages):
- `client.Application.*` - App control methods
- `client.Audio.*` - Audio configuration methods  
- `client.Measurements.*` - Measurement workflow methods

**Async Operation Pattern** (Consistent handling):
- Immediate operations return results directly
- Long-running operations return `LongRunningJob<T>`
- Progress reporting and cancellation support

**Error Handling Pattern** (Unified across languages):
- Domain-specific exceptions (e.g., `REWAudioException`)
- Rich error context including available options
- Automatic retry and fallback where appropriate

## Rationale

### Why This Approach

1. **Battle-Tested Patterns**: Follows proven enterprise architecture patterns
2. **REW Domain Focus**: Architecture specifically designed for REW's characteristics
3. **Future-Proof**: Clean layers allow evolution without breaking user code
4. **Performance**: Optimized for REW's constraints and data patterns
5. **Developer Experience**: Intuitive domain-oriented API

### Architectural Trade-offs

**Benefits**:
- ✅ Complete encapsulation of REW complexities
- ✅ Consistent cross-language experience
- ✅ Proper async/await patterns for long-running operations
- ✅ Rich domain objects with meaningful operations
- ✅ REW-specific optimizations built-in

**Costs**:
- 🔸 More complex implementation than simple HTTP wrappers
- 🔸 Requires maintenance of multiple layers
- 🔸 Initial development effort higher

**Rejected Alternatives**:
- ❌ Simple JSON/HTTP exposure (poor encapsulation)
- ❌ Generic REST client (ignores REW-specific patterns)
- ❌ Language-specific designs (inconsistent experience)

## Implementation Plan

### Phase 1: Core Infrastructure ✅

- Implement `CommandGate`, `LongRunningJob<T>`, `ArrayCodec`
- Create REW-specific HTTP client with proper retry logic
- Build subscription management system

### Phase 2: Domain Services ✅

- Implement `ApplicationService`, `AudioService`, `MeasurementsService`
- Create rich domain objects with proper validation
- Add comprehensive error handling and recovery

### Phase 3: Cross-Language Implementation

- Port C# implementation to JavaScript/TypeScript
- Create C++ implementation using similar patterns
- Ensure API parity across all languages

### Phase 4: Testing and Validation

- Create comprehensive test suite covering all domain operations
- Validate against real REW instances
- Performance testing and optimization

## Success Metrics

- **API Coverage**: 100% REW API endpoint coverage through domain services
- **Cross-Language Parity**: Identical method signatures and behavior
- **User Experience**: Zero HTTP/JSON exposure in public APIs
- **Performance**: Sub-100ms overhead for typical operations
- **Reliability**: Proper handling of all REW edge cases and constraints

## References

- `03-architecture/views/object-model.md` - Complete architecture specification
- REW API Documentation - Official endpoint specifications
- Battle-Tested Architecture Patterns - Enterprise-grade layered design principles
- Cross-Language Design Consistency - Unified API experience standards

---

**Architecture Champion**: Development Team  
**Review Date**: 2024-12-19  
**Next Review**: When REW API adds new capabilities requiring architectural evolution