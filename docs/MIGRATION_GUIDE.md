# Migration Guide: Legacy REWApi → RewClient Domain Service Architecture

## Overview

This guide helps you migrate from the legacy `REWApi` class to the new **RewClient Domain Service Architecture** following ADR-004.

### Why Migrate?

| Legacy `REWApi` | Modern `RewClient` |
|-----------------|-------------------|
| ❌ Returns raw JSON strings | ✅ Returns strongly-typed objects |
| ❌ Manual JSON parsing required | ✅ Automatic deserialization |
| ❌ No domain logic encapsulation | ✅ Clean domain service layer |
| ❌ Direct HTTP coupling | ✅ Facade pattern with services |
| ❌ No advanced patterns | ✅ CommandGate, LongRunningJob, ArrayCodec |

## Quick Start

### Before (Legacy)

```csharp
using REWApi;

var rewApi = new REWApi();
var json = await rewApi.ListMeasurementsAsync();
// Manual JSON parsing needed
```

### After (Modern)

```csharp
using REWApi;

using var rewClient = await RewClient.ConnectAsync("http://localhost:4735");
var measurements = await rewClient.Measurements.ListMeasurements();
// measurements is MeasurementSummary[] - strongly typed!
```

## Migration Patterns

### 1. Connection & Initialization

#### Legacy
```csharp
var rewApi = new REWApi(); // Immediate instantiation
```

#### Modern
```csharp
// Async factory pattern - verifies connection
using var rewClient = await RewClient.ConnectAsync("http://localhost:4735");

// With custom configuration
var config = new RewConfiguration 
{ 
    BaseUrl = "http://localhost:4735",
    Timeout = TimeSpan.FromSeconds(60)
};
using var rewClient = await RewClient.ConnectAsync(config);
```

**Benefits:**
- ✅ Connection verified at startup
- ✅ Implements `IDisposable` for proper cleanup
- ✅ Configuration object for flexibility

---

### 2. List Measurements

#### Legacy
```csharp
var json = await rewApi.ListMeasurementsAsync();
// Returns: JSON string like {"1": {...}, "2": {...}}

// Manual parsing needed:
var doc = JsonDocument.Parse(json);
foreach (var prop in doc.RootElement.EnumerateObject())
{
    var title = prop.Value.GetProperty("title").GetString();
    // ...
}
```

#### Modern
```csharp
var measurements = await rewClient.Measurements.ListMeasurements();
// Returns: MeasurementSummary[]

foreach (var m in measurements)
{
    Console.WriteLine($"{m.Title} - {m.UUID}");
    Console.WriteLine($"  Date: {m.Date}");
    Console.WriteLine($"  Range: {m.StartFreq} - {m.EndFreq} Hz");
}
```

**Benefits:**
- ✅ Strongly-typed `MeasurementSummary` objects
- ✅ Automatic parsing of REW's unconventional format
- ✅ IntelliSense support
- ✅ No manual JSON handling

---

### 3. Get Measurement Details

#### Legacy
```csharp
var json = await rewApi.GetMeasurementAsync(measurementId);
// Returns: JSON string

var doc = JsonDocument.Parse(json);
var title = doc.RootElement.GetProperty("title").GetString();
// Manual extraction for every property
```

#### Modern
```csharp
var measurement = await rewClient.Measurements.GetMeasurement(uuid);
// Returns: MeasurementData with Summary property

Console.WriteLine($"Title: {measurement.Summary.Title}");
Console.WriteLine($"UUID: {measurement.Summary.UUID}");
Console.WriteLine($"Date: {measurement.Summary.Date}");
Console.WriteLine($"Inverted: {measurement.Summary.Inverted}");
Console.WriteLine($"REW Version: {measurement.Summary.REWVersion}");
```

**Benefits:**
- ✅ Nested `MeasurementData` with `Summary` property
- ✅ All properties strongly-typed (bool, DateTime, double, etc.)
- ✅ Easy to extend with additional data

---

### 4. Get Frequency Response

#### Legacy
```csharp
var json = await rewApi.GetFrequencyResponseAsync(
    id: measurementId, 
    unit: "SPL", 
    smoothing: "1/12", 
    ppo: 48
);

// Manual Base64 decoding of magnitude/phase arrays
var doc = JsonDocument.Parse(json);
var magnitudeBase64 = doc.RootElement.GetProperty("magnitude").GetString();
// Complex decoding logic...
```

#### Modern
```csharp
var freqResponse = await rewClient.Measurements.GetFrequencyResponse(
    measurementId: uuid,
    unit: "SPL",
    smoothing: "1/12",
    ppo: 48
);

// Arrays already decoded!
Console.WriteLine($"Magnitude points: {freqResponse.Magnitudes.Length}");
Console.WriteLine($"Phase points: {freqResponse.Phases.Length}");
Console.WriteLine($"Smoothing: {freqResponse.Smoothing}");

// Direct access to float arrays
var avgMagnitude = freqResponse.Magnitudes.Average();
```

**Benefits:**
- ✅ Automatic Base64 decoding via `ArrayCodec`
- ✅ Float arrays ready to use
- ✅ Metadata included (smoothing, unit, freq ranges)

---

### 5. Get Impulse Response

#### Legacy
```csharp
var json = await rewApi.GetImpulseResponseAsync(
    id: measurementId,
    unit: "Percent",
    windowed: false,
    normalised: true
);

// Manual parsing and Base64 decoding
```

#### Modern
```csharp
var impulseResponse = await rewClient.Measurements.GetImpulseResponse(
    measurementId: uuid,
    unit: "Percent",
    windowed: false,
    normalised: true
);

Console.WriteLine($"Sample Rate: {impulseResponse.SampleRate} Hz");
Console.WriteLine($"Duration: {impulseResponse.Duration} seconds");
Console.WriteLine($"Data points: {impulseResponse.Data.Length}");

// Ready-to-use float array
var peak = impulseResponse.Data.Max();
```

**Benefits:**
- ✅ Decoded float array in `Data` property
- ✅ Metadata: SampleRate, SampleInterval, Duration
- ✅ Timing reference information included

---

### 6. Export Measurement

#### Legacy
```csharp
// Not directly supported - required manual command construction
```

#### Modern
```csharp
await rewClient.Measurements.ExportMeasurement(
    measurementId: uuid,
    filePath: @"C:\Temp\measurement.mdat",
    format: ExportFormat.MDAT
);

// Or export as text
await rewClient.Measurements.ExportMeasurement(
    measurementId: uuid,
    filePath: @"C:\Temp\measurement.txt",
    format: ExportFormat.TXT
);
```

**Benefits:**
- ✅ Simple API for common export formats
- ✅ Automatic command queuing via `CommandGate`
- ✅ Type-safe `ExportFormat` enum

---

### 7. Run Sweep and Export (Advanced)

#### Legacy
```csharp
// Complex multi-step process with manual POST commands
```

#### Modern
```csharp
// High-level workflow method
var exportPath = await rewClient.Measurements.RunSweepAndExport(
    outputPath: @"C:\Temp\sweep.mdat",
    sweepConfig: new SweepConfiguration
    {
        StartFreq = 20,
        EndFreq = 20000,
        Level = -12.0,
        Duration = 16.0,
        Repetitions = 3
    },
    format: ExportFormat.MDAT
);

Console.WriteLine($"Sweep complete: {exportPath}");
```

**Benefits:**
- ✅ Encapsulates complex workflow
- ✅ Automatic command sequencing
- ✅ Configuration objects for clarity

---

### 8. Batch Processing

#### Modern Only
```csharp
var measurementIds = new[] { uuid1, uuid2, uuid3 };

var results = await rewClient.Measurements.ProcessBatch(
    measurementIds: measurementIds,
    processFunc: async (measurementsService, id) =>
    {
        var freqResp = await measurementsService.GetFrequencyResponse(id);
        return freqResp.Magnitudes.Average();
    },
    maxConcurrency: 3
);

foreach (var (id, avgMag) in results.Zip(measurementIds))
{
    Console.WriteLine($"{id}: {avgMag} dB");
}
```

**Benefits:**
- ✅ Parallel processing with concurrency control
- ✅ Generic batch operation support
- ✅ Progress tracking support

---

## Service Architecture

The modern `RewClient` provides three main service facades:

```csharp
using var rewClient = await RewClient.ConnectAsync("http://localhost:4735");

// Measurements Service
var measurements = await rewClient.Measurements.ListMeasurements();
var measurement = await rewClient.Measurements.GetMeasurement(uuid);
var freqResp = await rewClient.Measurements.GetFrequencyResponse(uuid);

// Application Service
var version = await rewClient.Application.GetVersion();
var status = await rewClient.Application.GetStatus();

// Audio Service
var devices = await rewClient.Audio.ListDevices();
var settings = await rewClient.Audio.GetSettings();
```

## Infrastructure Components

### CommandGate
Ensures only **one REW POST command** executes at a time (REW API constraint):

```csharp
// Automatic queuing via CommandGate - no manual coordination needed!
var task1 = rewClient.Measurements.ExportMeasurement(uuid1, path1);
var task2 = rewClient.Measurements.ExportMeasurement(uuid2, path2);
await Task.WhenAll(task1, task2); // CommandGate handles serialization
```

### LongRunningJob
Handles 202 Accepted responses for long operations:

```csharp
// Automatic polling for job completion
var result = await rewClient.Measurements.RunSweepAndExport(
    outputPath: path,
    sweepConfig: config
);
// Returns when job completes, no manual polling!
```

### ArrayCodec
Automatically decodes REW's Base64 big-endian float arrays:

```csharp
// No manual decoding needed!
var freqResp = await rewClient.Measurements.GetFrequencyResponse(uuid);
var magnitudes = freqResp.Magnitudes; // Already decoded float[]
```

## Testing

### Legacy Tests
```csharp
[Fact]
public async Task ListMeasurementsAsync_ValidCall_ReturnsListOfMeasurements()
{
    var rewApi = new REWApi();
    var result = await rewApi.ListMeasurementsAsync();
    
    result.Should().NotBeNull();
    result.Should().BeOfType<string>();
    result.Should().Contain("measurements");
}
```

### Modern Tests
```csharp
[Fact]
public async Task ListMeasurements_ValidCall_ReturnsMeasurementArray()
{
    var measurements = await _rewClient.Measurements.ListMeasurements();
    
    measurements.Should().NotBeNull();
    measurements.Should().BeOfType<MeasurementSummary[]>();
    measurements.Should().NotBeEmpty();
    
    var first = measurements[0];
    first.UUID.Should().NotBeNullOrEmpty();
    first.Title.Should().NotBeNullOrEmpty();
}
```

### Test Fixture Pattern
```csharp
public class RewClientFixture : IAsyncLifetime
{
    public RewClient? Client { get; private set; }
    
    public async Task InitializeAsync()
    {
        Client = await RewClient.ConnectAsync("http://localhost:4735");
    }
    
    public async Task DisposeAsync()
    {
        Client?.Dispose();
    }
}

public class MyTests : IClassFixture<RewClientFixture>
{
    private readonly RewClient _rewClient;
    
    public MyTests(RewClientFixture fixture)
    {
        _rewClient = fixture.Client ?? throw new InvalidOperationException();
    }
    
    [Fact]
    public async Task MyTest()
    {
        var measurements = await _rewClient.Measurements.ListMeasurements();
        // ...
    }
}
```

## Migration Checklist

- [ ] Update connection code to use `RewClient.ConnectAsync()`
- [ ] Replace `ListMeasurementsAsync()` with `Measurements.ListMeasurements()`
- [ ] Replace `GetMeasurementAsync()` with `Measurements.GetMeasurement()`
- [ ] Update frequency response calls to use `GetFrequencyResponse()`
- [ ] Remove manual JSON parsing code
- [ ] Remove manual Base64 decoding code
- [ ] Update method signatures to use strongly-typed return values
- [ ] Add `using` statement for proper disposal
- [ ] Update tests to use fixture pattern
- [ ] Replace string assertions with strongly-typed property checks
- [ ] Remove dependencies on legacy `REWApi` class
- [ ] Update documentation and comments

## Common Issues

### Issue: JSON Parsing Errors
**Legacy:** `ListMeasurementsAsync()` expects array but REW returns `{"1": {...}, "2": {...}}`

**Solution:** Modern `ListMeasurements()` handles this correctly:
```csharp
// Automatically parses REW's numeric-key object format
var measurements = await rewClient.Measurements.ListMeasurements();
```

### Issue: Base64 Decoding
**Legacy:** Manual big-endian float decoding required

**Solution:** Modern API auto-decodes via `ArrayCodec`:
```csharp
var freqResp = await rewClient.Measurements.GetFrequencyResponse(uuid);
var magnitudes = freqResp.Magnitudes; // Already decoded!
```

### Issue: Command Sequencing
**Legacy:** Manual delays between POST commands to avoid conflicts

**Solution:** `CommandGate` handles sequencing automatically:
```csharp
// Fire and forget - CommandGate queues them
await Task.WhenAll(
    rewClient.Measurements.ExportMeasurement(uuid1, path1),
    rewClient.Measurements.ExportMeasurement(uuid2, path2)
);
```

## Performance Considerations

| Aspect | Legacy | Modern |
|--------|--------|--------|
| Connection verification | ❌ None | ✅ ConnectAsync validates |
| Response parsing | ⚠️ Manual every call | ✅ Cached parsers |
| Memory | ⚠️ JSON strings kept in memory | ✅ Typed objects, GC-friendly |
| Concurrency | ❌ Manual coordination | ✅ CommandGate handles it |
| Long operations | ❌ Manual polling | ✅ LongRunningJob auto-polls |

## Additional Resources

- **Architecture Decision Record**: `03-architecture/decisions/ADR-004-domain-service-architecture.md`
- **Test Coverage Analysis**: `tests/csharp/TEST_COVERAGE_ANALYSIS.md`
- **Domain Services**: `src/csharp/Domain/Services/`
- **Value Objects**: `src/csharp/Domain/ValueObjects.cs`
- **Infrastructure**: `src/csharp/Infrastructure/`

## Support

For questions or issues:
1. Check test examples in `tests/csharp/REWMeasurementsModernApiTests.cs`
2. Review feature tests in `tests/csharp/REWMeasurementsFeatureTests.cs`
3. See legacy tests in `tests/csharp/REWMeasurementsApiTests.cs` for comparison

---

**Last Updated**: 2025-10-21  
**Migration Guide Version**: 1.0  
**Target Architecture**: ADR-004 Domain Service Architecture
