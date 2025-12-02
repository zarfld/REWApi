---
title: "REW Client Library Requirements - Audio API"
specType: requirements
standard: "IEEE/ISO/IEC 29148:2018"
phase: 02-requirements
version: 1.0.0
status: draft
author: requirements-analysis
date: "2025-10-21"
traceability:
  stakeholderRequirements:
    - StR-001
  architecture:
    - ADR-004
  verification:
    - TEST-AUDIO-XXX
description: "Functional requirements for AudioService exposing REW REST API /audio/* endpoints"
---

# REW Client Library - Audio API Requirements

**Document ID**: REQ-F-CLIENT-002  
**REW REST API Resource**: `/audio/*`  
**Client Library Component**: `AudioService.cs` (Domain Service)  
**Standards**: ISO/IEC/IEEE 29148:2018 (System Requirements)  
**REW Swagger**: <http://localhost:4735/>

---

## 1. Purpose

Define functional requirements for the **AudioService** domain service class that provides object-oriented access to REW REST API `/audio/*` endpoints.

**What We're Building**:

- **Client Library Component**: `AudioService` class
- **Consumes**: REW REST API `/audio/*` endpoints (~60 endpoints)
- **Provides**: Clean OOP interface for audio device configuration (ASIO + Java Audio)
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoint Groups

**REW Swagger Documentation**: <http://localhost:4735/>

### 2.1 ASIO Audio Endpoints (~21 endpoints)

| HTTP Method | REW Endpoint | Purpose | Status |
|---|---|---|---|
| POST | `/audio/asio/command` | Execute ASIO command | ❌ Missing |
| GET | `/audio/asio/commands` | List available ASIO commands | ❌ Missing |
| GET | `/audio/asio/device` | Get current ASIO device | ❌ Missing |
| POST | `/audio/asio/device` | Set ASIO device | ⚠️ Partial (via ConfigureASIO) |
| GET | `/audio/asio/devices` | List available ASIO devices | ❌ Missing |
| GET | `/audio/asio/input` | Get ASIO input channel | ❌ Missing |
| POST | `/audio/asio/input` | Set ASIO input channel | ❌ Missing |
| GET | `/audio/asio/inputs` | List ASIO input channels | ❌ Missing |
| GET | `/audio/asio/last-input` | Get last used ASIO input | ❌ Missing |
| POST | `/audio/asio/last-input` | Set last used ASIO input | ❌ Missing |
| GET | `/audio/asio/output` | Get ASIO output channel | ❌ Missing |
| POST | `/audio/asio/output` | Set ASIO output channel | ❌ Missing |
| GET | `/audio/asio/outputs` | List ASIO output channels | ❌ Missing |
| GET | `/audio/asio/ref-input` | Get ASIO reference input | ❌ Missing |
| POST | `/audio/asio/ref-input` | Set ASIO reference input | ❌ Missing |
| GET | `/audio/asio/ref-output` | Get ASIO reference output | ❌ Missing |
| POST | `/audio/asio/ref-output` | Set ASIO reference output | ❌ Missing |
| GET | `/audio/asio/secondary-output` | Get secondary output | ❌ Missing |
| POST | `/audio/asio/secondary-output` | Set secondary output | ❌ Missing |
| GET | `/audio/asio/secondary-output-enable` | Get secondary output enabled | ❌ Missing |
| POST | `/audio/asio/secondary-output-enable` | Enable secondary output | ❌ Missing |

### 2.2 Java Audio Endpoints (~20 endpoints)

| HTTP Method | REW Endpoint | Purpose | Status |
|---|---|---|---|
| GET | `/audio/java/input-device` | Get Java input device | ❌ Missing |
| POST | `/audio/java/input-device` | Set Java input device | ⚠️ Partial (via ConfigureJava) |
| GET | `/audio/java/input-devices` | List Java input devices | ❌ Missing |
| GET | `/audio/java/output-device` | Get Java output device | ❌ Missing |
| POST | `/audio/java/output-device` | Set Java output device | ⚠️ Partial (via ConfigureJava) |
| GET | `/audio/java/output-devices` | List Java output devices | ❌ Missing |
| GET | `/audio/java/ref-input-device` | Get reference input device | ❌ Missing |
| POST | `/audio/java/ref-input-device` | Set reference input device | ❌ Missing |
| GET | `/audio/java/ref-output-device` | Get reference output device | ❌ Missing |
| POST | `/audio/java/ref-output-device` | Set reference output device | ❌ Missing |
| GET | `/audio/java/samplerate` | Get sample rate | ❌ Missing |
| POST | `/audio/java/samplerate` | Set sample rate | ⚠️ Partial (via ConfigureJava) |
| GET | `/audio/java/samplerates` | List available sample rates | ❌ Missing |

### 2.3 Audio Calibration Endpoints (~10+ endpoints)

| HTTP Method | REW Endpoint | Purpose | Status |
|---|---|---|---|
| GET | `/audio/calibration/...` | Various calibration endpoints | ❌ Missing |

### 2.4 General Audio Configuration (~9 endpoints)

| HTTP Method | REW Endpoint | Purpose | Status |
|---|---|---|---|
| GET | `/audio/output-meter-range` | Get output meter range | ❌ Missing |
| POST | `/audio/output-meter-range` | Set output meter range | ❌ Missing |
| GET | `/audio/use-asio` | Check if using ASIO | ⚠️ Partial |
| POST | `/audio/use-asio` | Set use ASIO | ⚠️ Partial |
| GET | `/audio/use-java` | Check if using Java Audio | ⚠️ Partial |
| POST | `/audio/use-java` | Set use Java Audio | ⚠️ Partial |

**Total Endpoints**: ~60 endpoints in `/audio/*` group  
**Currently Implemented**: ~5 endpoints via high-level methods (8% coverage)  
**Target**: 100% coverage with granular endpoint access

---

## 3. Current Implementation Issues

### Problem: High-Level Abstraction Instead of Granular Access

**Current Implementation** (`AudioService.cs`):

```csharp
// High-level methods that call multiple REW endpoints internally
⚠️ ConfigureJava(string inputDevice, string outputDevice, int sampleRate)
⚠️ ConfigureASIO(string device, int sampleRate)
```

**Issue**: These methods hide REW's granular REST API structure, making it impossible to:

- Query available devices before selection
- Get current configuration
- Configure only one aspect (e.g., change sample rate without changing device)
- Access reference inputs/outputs
- Enable secondary outputs
- Get/set individual channels

**Required Refactoring**: Replace high-level methods with granular methods that map 1:1 to REW endpoints.

---

## 4. Functional Requirements

### REQ-F-CLIENT-002.1: AudioService Domain Service

**ID**: REQ-F-CLIENT-002.1  
**Priority**: Critical  
**Type**: Functional

**Requirement**: The client library SHALL provide an `AudioService` domain service class that exposes all REW REST API `/audio/*` functionality through an object-oriented interface with granular control.

**Acceptance Criteria**:

- ✅ `AudioService` class exists in `src/csharp/Domain/Services/`
- ❌ Currently only 5/60 REW endpoints accessible (need 55 more methods)
- ⚠️ Current high-level methods (ConfigureJava/ConfigureASIO) should be refactored or marked as convenience methods
- ✅ Follows ADR-004 layered architecture
- ⚠️ Cross-language consistency pending

**Current Status**: 8% implemented (needs refactoring)

**Rationale**: Users need granular control over audio configuration matching REW's REST API capabilities.

**Traces To**:

- **Architecture**: ADR-004 (Domain Service Architecture)
- **REW API**: `/audio/*` endpoints
- **Tests**: `TEST-AUDIO-001` through `TEST-AUDIO-060`

---

### REQ-F-CLIENT-002.2: ASIO Device Management

**ID**: REQ-F-CLIENT-002.2  
**Priority**: High  
**Type**: Functional

**Requirement**: The `AudioService` SHALL provide methods for ASIO device enumeration, selection, and configuration.

#### Required Methods

```csharp
// Device Selection
Task<string[]> ListAsioDevices()
Task<string> GetAsioDevice()
Task<bool> SetAsioDevice(string deviceName)

// Channel Selection
Task<string[]> ListAsioInputs(string deviceName)
Task<string> GetAsioInput()
Task<bool> SetAsioInput(string channel)

Task<string[]> ListAsioOutputs(string deviceName)
Task<string> GetAsioOutput()
Task<bool> SetAsioOutput(string channel)

// Reference Channels
Task<string> GetAsioRefInput()
Task<bool> SetAsioRefInput(string channel)
Task<string> GetAsioRefOutput()
Task<bool> SetAsioRefOutput(string channel)

// Secondary Output
Task<string> GetAsioSecondaryOutput()
Task<bool> SetAsioSecondaryOutput(string channel)
Task<bool> GetAsioSecondaryOutputEnabled()
Task<bool> SetAsioSecondaryOutputEnabled(bool enabled)

// Commands
Task<string[]> GetAsioCommands()
Task<CommandResult> ExecuteAsioCommand(string command, Dictionary<string, object> parameters = null)
```

**Acceptance Criteria**:

- ❌ All methods implemented
- ❌ Domain objects for device/channel information
- ❌ >80% unit test coverage

**Traces To**:

- **REW API**: `/audio/asio/*` endpoints
- **Tests**: `TEST-AUDIO-ASIO-XXX`

---

### REQ-F-CLIENT-002.3: Java Audio Device Management

**ID**: REQ-F-CLIENT-002.3  
**Priority**: High  
**Type**: Functional

**Requirement**: The `AudioService` SHALL provide methods for Java Audio device enumeration, selection, and configuration.

#### Required Methods

```csharp
// Device Selection
Task<string[]> ListJavaInputDevices()
Task<string> GetJavaInputDevice()
Task<bool> SetJavaInputDevice(string deviceName)

Task<string[]> ListJavaOutputDevices()
Task<string> GetJavaOutputDevice()
Task<bool> SetJavaOutputDevice(string deviceName)

// Reference Devices
Task<string> GetJavaRefInputDevice()
Task<bool> SetJavaRefInputDevice(string deviceName)
Task<string> GetJavaRefOutputDevice()
Task<bool> SetJavaRefOutputDevice(string deviceName)

// Sample Rate
Task<int[]> ListJavaSampleRates()
Task<int> GetJavaSampleRate()
Task<bool> SetJavaSampleRate(int sampleRate)
```

**Acceptance Criteria**:

- ❌ All methods implemented
- ❌ Domain objects for device information
- ❌ >80% unit test coverage

**Traces To**:

- **REW API**: `/audio/java/*` endpoints
- **Tests**: `TEST-AUDIO-JAVA-XXX`

---

### REQ-F-CLIENT-002.4: Audio System Selection

**ID**: REQ-F-CLIENT-002.4  
**Priority**: High  
**Type**: Functional

**Requirement**: The `AudioService` SHALL provide methods to switch between ASIO and Java Audio systems.

#### Required Methods

```csharp
// System Selection
Task<bool> GetUseAsio()
Task<bool> SetUseAsio(bool useAsio)

Task<bool> GetUseJava()
Task<bool> SetUseJava(bool useJava)

// Convenience method
Task<AudioSystemType> GetActiveAudioSystem()
```

**Acceptance Criteria**:

- ❌ Methods implemented
- ❌ AudioSystemType enum (ASIO, JavaAudio)
- ❌ Unit test coverage

**Traces To**:

- **REW API**: `/audio/use-asio`, `/audio/use-java`
- **Tests**: `TEST-AUDIO-SYSTEM-XXX`

---

### REQ-F-CLIENT-002.5: Audio Calibration

**ID**: REQ-F-CLIENT-002.5  
**Priority**: Medium  
**Type**: Functional

**Requirement**: The `AudioService` SHALL provide methods for audio calibration management.

**Note**: Detailed calibration endpoints need to be mapped from REW Swagger when REW is running.

**Acceptance Criteria**:

- ❌ Calibration methods implemented
- ❌ Calibration domain objects
- ❌ Unit test coverage

**Traces To**:

- **REW API**: `/audio/calibration/*`
- **Tests**: `TEST-AUDIO-CALIBRATION-XXX`

---

### REQ-F-CLIENT-002.6: Output Meter Configuration

**ID**: REQ-F-CLIENT-002.6  
**Priority**: Low  
**Type**: Functional

**Requirement**: The `AudioService` SHALL provide methods to configure output meter range.

#### Required Methods

```csharp
Task<double> GetOutputMeterRange()
Task<bool> SetOutputMeterRange(double rangedB)
```

**Acceptance Criteria**:

- ❌ Methods implemented
- ❌ Unit test coverage

**Traces To**:

- **REW API**: `/audio/output-meter-range`
- **Tests**: `TEST-AUDIO-METER-XXX`

---

## 5. Domain Objects

### AudioDevice Class

```csharp
/// <summary>
/// Represents an audio device (ASIO or Java Audio)
/// </summary>
public class AudioDevice
{
    public string Name { get; set; }
    public AudioSystemType SystemType { get; set; }
    public string[] AvailableInputs { get; set; }
    public string[] AvailableOutputs { get; set; }
    public int[] SupportedSampleRates { get; set; }
}
```

### AudioSystemType Enum

```csharp
public enum AudioSystemType
{
    ASIO,
    JavaAudio
}
```

### AudioConfiguration Class

```csharp
/// <summary>
/// Complete audio configuration state
/// </summary>
public class AudioConfiguration
{
    public AudioSystemType SystemType { get; set; }
    
    // ASIO Configuration
    public string AsioDevice { get; set; }
    public string AsioInput { get; set; }
    public string AsioOutput { get; set; }
    public string AsioRefInput { get; set; }
    public string AsioRefOutput { get; set; }
    public string AsioSecondaryOutput { get; set; }
    public bool AsioSecondaryOutputEnabled { get; set; }
    
    // Java Audio Configuration
    public string JavaInputDevice { get; set; }
    public string JavaOutputDevice { get; set; }
    public string JavaRefInputDevice { get; set; }
    public string JavaRefOutputDevice { get; set; }
    public int JavaSampleRate { get; set; }
    
    // General
    public double OutputMeterRangedB { get; set; }
}
```

---

## 6. Migration from Legacy API

### Current High-Level Methods (To Be Refactored)

```csharp
// LEGACY: High-level convenience methods
[Obsolete("Use granular methods: SetAsioDevice, SetAsioInput, SetAsioOutput")]
public async Task ConfigureASIO(string device, int sampleRate)

[Obsolete("Use granular methods: SetJavaInputDevice, SetJavaOutputDevice, SetJavaSampleRate")]
public async Task ConfigureJava(string inputDevice, string outputDevice, int sampleRate)
```

**Migration Strategy**:

1. Mark existing methods as `[Obsolete]` with guidance
2. Implement all granular methods
3. Keep convenience methods as thin wrappers calling granular methods
4. Update all tests to use new granular API
5. Provide migration guide in documentation

---

## 7. Summary: Audio API Coverage

| Category | Total Endpoints | Implemented | Status |
|---|---|---|---|
| **ASIO Device Management** | 21 | 1 (partial) | ❌ 5% |
| **Java Audio Management** | 20 | 3 (partial) | ⚠️ 15% |
| **Audio System Selection** | 4 | 2 (partial) | ⚠️ 50% |
| **Calibration** | 10+ | 0 | ❌ 0% |
| **Output Meter** | 2 | 0 | ❌ 0% |
| **General Configuration** | 3 | 0 | ❌ 0% |
| **TOTAL** | ~60 | ~5 | ⚠️ **8%** |

**Critical Gaps**:

1. ❌ 55 endpoints not exposed via granular OOP methods
2. ⚠️ High-level methods (ConfigureJava/ConfigureASIO) hide REST API structure
3. ❌ No device enumeration methods
4. ❌ No reference input/output configuration
5. ❌ No calibration management
6. ❌ No channel selection methods

---

## 8. Test Requirements

### REQ-F-CLIENT-002.7: Test Coverage

**Requirement**: SHALL have comprehensive automated tests for all functionality.

**Current Status**:

- ⚠️ Legacy tests exist but test high-level methods
- ❌ No tests for granular methods (not implemented yet)
- ❌ No integration tests against real REW

**Required**:

- 60 unit tests (one per endpoint)
- 10+ integration tests (ASIO/Java workflows)
- >80% code coverage

**Traces To**:

- **Tests**: `tests/csharp/AudioServiceTests.cs` (TO BE CREATED)
- **Integration**: `tests/csharp/AudioIntegrationTests.cs` (TO BE CREATED)

---

## 9. Non-Functional Requirements

### Performance

- Device enumeration SHALL complete within 500ms
- Configuration changes SHALL complete within 200ms

### Error Handling

- SHALL provide `REWAudioException` with device-specific error messages
- SHALL handle device not found gracefully
- SHALL validate sample rates before sending to REW

### Cross-Language Consistency

- SHALL provide identical functionality across C#, C++, JavaScript, TypeScript

---

## 10. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ All 60 REW `/audio/*` endpoints accessible via OOP interface
- ✅ Granular methods replace/supplement high-level convenience methods
- ✅ `AudioService` class implements ADR-004 architecture
- ✅ Domain objects encapsulate audio concepts
- ✅ >80% unit test coverage
- ✅ Integration tests pass against real REW (ASIO + Java Audio)
- ✅ Cross-language parity verified
- ✅ Migration guide from legacy API complete

**Current Status**: ⚠️ **8% Complete** (5/60 endpoints, needs refactoring)

---

## 11. References

- **REW REST API**: <http://localhost:4735/> (Swagger documentation)
- **Architecture**: `03-architecture/views/object-model.md`
- **ADR-004**: `03-architecture/decisions/ADR-004-simple-object-model-pattern.md`
- **Implementation**: `src/csharp/Domain/Services/AudioService.cs`
- **Gap Analysis**: `02-requirements/functional/API_COMPLETENESS_GAP_ANALYSIS.md`

---

**Document Owner**: Requirements Team  
**Status**: Draft (8% implemented, needs refactoring)  
**Next Review**: After granular method implementation
