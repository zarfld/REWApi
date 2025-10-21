---
title: "REW Client Library Requirements - Remaining API Groups (Groups, Import, Measure, Alignment-Tool, Room-Sim, Stepped-Measurement, Input-Levels, Version)"
specType: requirements
phase: 02-requirements
version: 1.0.0
status: draft
author: requirements-analysis
date: 2025-10-21
traceability:
  stakeholderRequirements:
    - STR-001 (Audio measurement automation)
  architecture:
    - ADR-004 (Domain Service Architecture)
description: "Functional requirements for remaining 8 API resource groups (consolidated document)"
---

# REW Client Library - Remaining API Groups Requirements

**Document IDs**: REQ-F-CLIENT-008 through REQ-F-CLIENT-015  
**REW Swagger**: <http://localhost:4735/>  
**Standards**: ISO/IEC/IEEE 29148:2018  
**Status**: ❌ **0% Coverage - All Services Missing**

---

## REQ-F-CLIENT-008: Groups API (11 endpoints)

**REW REST API Resource**: `/groups/*`  
**Client Library Component**: `GroupsService.cs` (Domain Service - NEW)

### Purpose

Manage measurement groups for organizing related measurements.

### Key Endpoints

- GET `/groups` - List all groups
- POST `/groups` - Create new group
- GET `/groups/{id}` - Get group details
- PUT `/groups/{id}` - Update group
- DELETE `/groups/{id}` - Delete group
- GET `/groups/{id}/measurements` - List measurements in group
- POST `/groups/{id}/measurements/{measurementUuid}` - Add measurement to group
- DELETE `/groups/{id}/measurements/{measurementUuid}` - Remove measurement from group
- POST `/groups/subscribe` - Subscribe to group changes
- GET `/groups/subscribers` - List subscribers
- POST `/groups/unsubscribe` - Unsubscribe

### Required Methods

```csharp
public class GroupsService
{
    Task<Group[]> ListGroups()
    Task<Group> GetGroup(string groupId)
    Task<Group> CreateGroup(string name, string description)
    Task<bool> UpdateGroup(string groupId, string name, string description)
    Task<bool> DeleteGroup(string groupId)
    Task<string[]> GetGroupMeasurements(string groupId)
    Task<bool> AddMeasurementToGroup(string groupId, string measurementUuid)
    Task<bool> RemoveMeasurementFromGroup(string groupId, string measurementUuid)
    Task<string> SubscribeToGroupChanges(Action<GroupChangeEvent> handler)
    Task<SubscriptionInfo[]> GetGroupSubscribers()
    Task<bool> UnsubscribeFromGroupChanges(string subscriptionId)
}
```

### Domain Objects

```csharp
public class Group
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] MeasurementUuids { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupChangeEvent
{
    public string GroupId { get; set; }
    public GroupChangeType ChangeType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum GroupChangeType
{
    Created,
    Updated,
    Deleted,
    MeasurementAdded,
    MeasurementRemoved
}
```

**Status**: ❌ 0/11 endpoints (0%)

---

## REQ-F-CLIENT-009: Import API (15 endpoints)

**REW REST API Resource**: `/import/*`  
**Client Library Component**: `ImportService.cs` (Domain Service - NEW)

### Purpose

Import measurement data from external files (FRD, impulse responses, RTA files, sweep recordings).

### Key Endpoints

- POST `/import/frequency-response` - Import frequency response (FRD file)
- POST `/import/impulse-response` - Import impulse response (WAV/text)
- POST `/import/rta` - Import RTA data
- POST `/import/sweep-recording` - Import sweep recording
- POST `/import/calibration` - Import calibration file
- GET `/import/supported-formats` - List supported file formats
- POST `/import/validate` - Validate import file before importing

### Required Methods

```csharp
public class ImportService
{
    Task<string> ImportFrequencyResponse(string filePath, ImportOptions options)
    Task<string> ImportImpulseResponse(string filePath, ImportOptions options)
    Task<string> ImportRTA(string filePath, ImportOptions options)
    Task<string> ImportSweepRecording(string filePath, ImportOptions options)
    Task<string> ImportCalibration(string filePath)
    Task<ImportFormat[]> GetSupportedFormats()
    Task<ValidationResult> ValidateImportFile(string filePath, ImportType type)
}
```

### Domain Objects

```csharp
public class ImportOptions
{
    public double SampleRate { get; set; }
    public string Title { get; set; }
    public string Notes { get; set; }
    public bool AddToCurrentTab { get; set; }
}

public enum ImportType
{
    FrequencyResponse,
    ImpulseResponse,
    RTA,
    SweepRecording,
    Calibration
}

public class ImportFormat
{
    public ImportType Type { get; set; }
    public string[] FileExtensions { get; set; }
    public string Description { get; set; }
}
```

**Status**: ❌ 0/15 endpoints (0%)

---

## REQ-F-CLIENT-010: Measure API (60 endpoints)

**REW REST API Resource**: `/measure/*`  
**Client Library Component**: `MeasureService.cs` (Domain Service - NEW)

### Purpose

Configure and execute sweep measurements (separate from viewing measurement results).

### Key Endpoint Categories

1. **Sweep Configuration** (~20 endpoints)
   - Start/end frequencies, length, sweep type, fade in/out, levels
   
2. **UMIK Settings** (~10 endpoints)
   - UMIK-specific settings for miniDSP microphones
   - Averaging, alignment, etc.

3. **Measurement Execution** (~10 endpoints)
   - Start/stop measurement, check status, get progress

4. **Noise Floor & Calibration** (~8 endpoints)
   - Capture noise floor, manage calibration data

5. **Advanced Settings** (~12 endpoints)
   - Dither, timing reference, file playback options

### Required Methods (Sample)

```csharp
public class MeasureService
{
    // Sweep Configuration
    Task<double> GetStartFrequency()
    Task<bool> SetStartFrequency(double hz)
    Task<double> GetEndFrequency()
    Task<bool> SetEndFrequency(double hz)
    Task<double> GetSweepLength()
    Task<bool> SetSweepLength(double seconds)
    
    // Execution
    Task<string> StartMeasurement()
    Task<bool> StopMeasurement()
    Task<MeasurementStatus> GetMeasurementStatus()
    Task<double> GetMeasurementProgress()
    
    // Noise Floor
    Task<NoiseFloorData> CaptureNoiseFloor()
    Task<NoiseFloorData> GetNoiseFloor()
    
    // UMIK Settings
    Task<bool> GetUMIKAveraging()
    Task<bool> SetUMIKAveraging(bool enabled)
    Task<int> GetUMIKAverageCount()
    Task<bool> SetUMIKAverageCount(int count)
}
```

**Status**: ❌ 0/60 endpoints (0%)  
**Note**: Current `MeasurementsService.RunSweepAndExport()` is high-level convenience method, not granular REW endpoint access

---

## REQ-F-CLIENT-011: Alignment-Tool API (30 endpoints)

**REW REST API Resource**: `/alignment-tool/*`  
**Client Library Component**: `AlignmentToolService.cs` (Domain Service - NEW)

### Purpose

Time-align measurements, adjust gains, invert polarity, manage filtered responses.

### Key Endpoints

- GET/POST `/alignment-tool/delay/{measurementUuid}` - Get/set delay
- GET/POST `/alignment-tool/gain/{measurementUuid}` - Get/set gain adjustment
- GET/POST `/alignment-tool/invert/{measurementUuid}` - Get/set polarity inversion
- GET/POST `/alignment-tool/mode` - Alignment mode
- GET `/alignment-tool/aligned-response/{measurementUuid}` - Get aligned response

### Required Methods

```csharp
public class AlignmentToolService
{
    Task<double> GetDelay(string measurementUuid)
    Task<bool> SetDelay(string measurementUuid, double delayMs)
    Task<double> GetGain(string measurementUuid)
    Task<bool> SetGain(string measurementUuid, double gaindB)
    Task<bool> GetInvert(string measurementUuid)
    Task<bool> SetInvert(string measurementUuid, bool invert)
    Task<AlignmentMode> GetMode()
    Task<bool> SetMode(AlignmentMode mode)
    Task<FrequencyResponse> GetAlignedResponse(string measurementUuid)
}
```

**Status**: ❌ 0/30 endpoints (0%)

---

## REQ-F-CLIENT-012: Room Simulator API (35 endpoints)

**REW REST API Resource**: `/roomsim/*`  
**Client Library Component**: `RoomSimService.cs` (Domain Service - NEW)

### Purpose

Simulate room acoustics with configurable room dimensions, absorption, and source/mic positions.

### Key Endpoints

- GET/POST `/roomsim/room-size` - Room dimensions (L x W x H)
- GET/POST `/roomsim/absorptions` - Wall absorption coefficients by frequency
- GET/POST `/roomsim/mic-positions` - Microphone positions array
- GET/POST `/roomsim/source-positions` - Speaker positions array
- POST `/roomsim/simulate` - Run simulation
- GET `/roomsim/simulated-response` - Get simulation result

### Required Methods

```csharp
public class RoomSimService
{
    Task<RoomDimensions> GetRoomSize()
    Task<bool> SetRoomSize(double lengthM, double widthM, double heightM)
    Task<AbsorptionCoefficients> GetAbsorptions()
    Task<bool> SetAbsorptions(AbsorptionCoefficients coefficients)
    Task<Position3D[]> GetMicPositions()
    Task<bool> SetMicPositions(Position3D[] positions)
    Task<Position3D[]> GetSourcePositions()
    Task<bool> SetSourcePositions(Position3D[] positions)
    Task<string> RunSimulation()
    Task<SimulationResult> GetSimulatedResponse(string jobId)
}
```

**Status**: ❌ 0/35 endpoints (0%)

---

## REQ-F-CLIENT-013: Stepped-Measurement API (25 endpoints)

**REW REST API Resource**: `/stepped-measurement/*`  
**Client Library Component**: `SteppedMeasurementService.cs` (Domain Service - NEW)

### Purpose

Configure and execute stepped sine measurements for distortion analysis.

### Key Endpoints

- GET/POST `/stepped-measurement/fft-length` - FFT size
- GET/POST `/stepped-measurement/frequency-span` - Frequency range to measure
- GET/POST `/stepped-measurement/level-span` - Level range to measure
- GET/POST `/stepped-measurement/steps` - Number of steps
- POST `/stepped-measurement/start` - Start stepped measurement
- GET `/stepped-measurement/status` - Get measurement status

### Required Methods

```csharp
public class SteppedMeasurementService
{
    Task<int> GetFFTLength()
    Task<bool> SetFFTLength(int fftSize)
    Task<FrequencySpan> GetFrequencySpan()
    Task<bool> SetFrequencySpan(double startHz, double endHz, int steps)
    Task<LevelSpan> GetLevelSpan()
    Task<bool> SetLevelSpan(double startdB, double enddB, int steps)
    Task<string> StartSteppedMeasurement()
    Task<SteppedMeasurementStatus> GetStatus()
    Task<SteppedMeasurementResult> GetResult(string jobId)
}
```

**Status**: ❌ 0/25 endpoints (0%)

---

## REQ-F-CLIENT-014: Input-Levels API (7 endpoints)

**REW REST API Resource**: `/input-levels/*`  
**Client Library Component**: `InputLevelsService.cs` (Domain Service - NEW)

### Purpose

Monitor input audio levels in real-time.

### Key Endpoints

- GET `/input-levels/last-levels` - Get most recent input level readings
- POST `/input-levels/subscribe` - Subscribe to level updates
- GET `/input-levels/subscribers` - List subscribers
- POST `/input-levels/unsubscribe` - Unsubscribe
- GET/POST `/input-levels/units` - Level units (dBFS, dBu, dBV, etc.)

### Required Methods

```csharp
public class InputLevelsService
{
    Task<LevelReading> GetLastLevels()
    Task<string> SubscribeToLevels(Action<LevelReading> handler)
    Task<SubscriptionInfo[]> GetLevelSubscribers()
    Task<bool> UnsubscribeFromLevels(string subscriptionId)
    Task<LevelUnit> GetUnits()
    Task<bool> SetUnits(LevelUnit units)
}
```

**Status**: ❌ 0/7 endpoints (0%)  
**Dependency**: Requires SubscriptionManager (TODO #20)

---

## REQ-F-CLIENT-015: Version API (1 endpoint)

**REW REST API Resource**: `/version`  
**Client Library Component**: `ApplicationService` or `VersionService`

### Purpose

Get REW version information.

### Endpoint

- GET `/version` - Get REW version

### Required Method

```csharp
// Option 1: Add to ApplicationService
public class ApplicationService
{
    Task<VersionInfo> GetVersion()
}

// Option 2: Separate VersionService
public class VersionService
{
    Task<VersionInfo> GetVersion()
}
```

### Domain Object

```csharp
public class VersionInfo
{
    public string Version { get; set; }        // e.g., "5.30.5"
    public string BuildDate { get; set; }
    public string ApiVersion { get; set; }     // e.g., "0.9.0"
}
```

**Status**: ❌ 0/1 endpoints (0%)

---

## Summary: Remaining 8 API Groups

| API Group | Endpoints | Priority | Status | Dependency |
|---|---|---|---|---|
| **Groups** (008) | 11 | Medium | ❌ 0% | SubscriptionManager |
| **Import** (009) | 15 | Medium | ❌ 0% | None |
| **Measure** (010) | 60 | High | ❌ 0% | None |
| **Alignment-Tool** (011) | 30 | Medium | ❌ 0% | None |
| **Room-Sim** (012) | 35 | Low | ❌ 0% | None |
| **Stepped-Measurement** (013) | 25 | Low | ❌ 0% | None |
| **Input-Levels** (014) | 7 | Low | ❌ 0% | SubscriptionManager |
| **Version** (015) | 1 | Low | ❌ 0% | None |

**Total**: 184 endpoints, 0% coverage

---

## Common Requirements (All Services)

### Architecture Compliance

- SHALL follow ADR-004 Domain Service Architecture
- SHALL use CommandGate for POST operations
- SHALL use LongRunningJob<T> for 202 responses
- SHALL use ArrayCodec for Base64 arrays
- NO direct HTTP calls in domain layer

### Testing

- Each endpoint SHALL have unit test
- Integration tests SHALL validate against real REW
- >80% code coverage required

### Cross-Language

- SHALL provide identical API across C#, C++, JavaScript, TypeScript

### Documentation

- XML documentation comments required
- Usage examples for complex workflows

---

## References

- **REW REST API**: <http://localhost:4735/>
- **Architecture**: ADR-004
- **Gap Analysis**: `API_COMPLETENESS_GAP_ANALYSIS.md`
- **Detailed Specs**: REQ-F-CLIENT-001 through REQ-F-CLIENT-007 (Application, Audio, Measurements, Generator, RTA, SPL-Meter, EQ)

---

**Document Owner**: Requirements Team  
**Status**: Draft (0% implemented - all 8 services need creation)  
**Next Action**: Prioritize implementation order based on stakeholder needs
