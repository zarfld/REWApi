---
title: "REW Client Library Requirements - Measurements API"
specType: requirements
standard: "IEEE/ISO/IEC 29148:2018"
phase: 02-requirements
version: 1.0.0
status: approved
author: requirements-analysis
date: "2025-10-21"
traceability:
  stakeholderRequirements:
    - StR-001
    - StR-002
  architecture:
    - ADR-004
  verification:
    - TEST-MEASUREMENTS-001 through TEST-MEASUREMENTS-050
description: "Functional requirements for MeasurementsService exposing REW REST API /measurements/* endpoints"
---

# REW Client Library - Measurements API Requirements

**Document ID**: REQ-F-CLIENT-003  
**REW REST API Resource**: `/measurements/*`  
**Client Library Component**: `MeasurementsService.cs` (Domain Service)  
**Standards**: ISO/IEC/IEEE 29148:2018 (System Requirements)

---

## 1. Purpose

Define functional requirements for the **MeasurementsService** domain service class that provides object-oriented access to REW REST API `/measurements/*` endpoints.

**What We're Building**:

- **Client Library Component**: `MeasurementsService` class
- **Consumes**: REW REST API `/measurements/*` endpoints (~50 endpoints)
- **Provides**: Clean OOP interface following ADR-004 Domain Service Architecture
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints (Source of Truth)

**REW Swagger Documentation**: <http://localhost:4735/> (Official API specification)

### Endpoints Covered by This Requirement

| HTTP Method | REW Endpoint | Purpose | Response | Our Status |
|---|---|---|---|---|
| GET | `/measurements` | List all measurements | JSON array | ✅ Implemented |
| DELETE | `/measurements` | Delete all measurements | 200 OK | ❌ Missing |
| GET | `/measurements/{id}` | Get measurement details | JSON | ✅ Implemented |
| PUT | `/measurements/{id}` | Update measurement metadata | JSON | ⚠️ Partial |
| DELETE | `/measurements/{id}` | Delete single measurement | 200 OK | ❌ Missing |
| POST | `/measurements/{id}/command` | Execute measurement command | 200/202 | ⚠️ Partial |
| GET | `/measurements/{id}/commands` | Get available commands | JSON array | ❌ Missing |
| GET | `/measurements/{id}/distortion` | Get distortion data | JSON | ❌ Missing |
| POST | `/measurements/{id}/eq/command` | Execute EQ command | 200/202 | ❌ Missing |
| GET | `/measurements/{id}/eq/frequency-response` | Get EQ'd frequency response | JSON | ❌ Missing |
| GET | `/measurements/{id}/eq/group-delay` | Get EQ'd group delay | JSON | ❌ Missing |
| GET | `/measurements/{id}/eq/impulse-response` | Get EQ'd impulse response | JSON | ❌ Missing |
| GET | `/measurements/{id}/equaliser` | Get equaliser settings | JSON | ❌ Missing |
| POST | `/measurements/{id}/equaliser` | Set equaliser settings | 200 OK | ❌ Missing |
| GET | `/measurements/{id}/filters` | Get filter settings | JSON | ❌ Missing |
| POST | `/measurements/{id}/filters` | Set filter settings | 200 OK | ❌ Missing |
| PUT | `/measurements/{id}/filters` | Update filter settings | 200 OK | ❌ Missing |
| GET | `/measurements/{id}/filters-impulse-response` | Get filtered impulse response | JSON | ❌ Missing |
| GET | `/measurements/{id}/frequency-response` | Get frequency response data | JSON | ⚠️ BROKEN (empty Magnitudes) |
| GET | `/measurements/{id}/group-delay` | Get group delay data | JSON | ❌ Missing |
| GET | `/measurements/{id}/impulse-response` | Get impulse response data | JSON | ❌ Missing |
| GET | `/measurements/{id}/ir-windows` | Get IR window settings | JSON | ❌ Missing |
| POST | `/measurements/{id}/ir-windows` | Set IR window settings | 200 OK | ❌ Missing |
| PUT | `/measurements/{id}/ir-windows` | Update IR window settings | 200 OK | ❌ Missing |
| GET | `/measurements/{id}/processed-frequency-response` | Get processed frequency response | JSON | ❌ Missing |
| GET | `/measurements/{id}/rt60` | Get RT60 reverberation time data | JSON | ❌ Missing |
| GET | `/measurements/{id}/schroeder` | Get Schroeder integration | JSON | ❌ Missing |
| GET | `/measurements/{id}/selected` | Check if measurement is selected | boolean | ❌ Missing |
| POST | `/measurements/{id}/selected` | Select/deselect measurement | 200 OK | ❌ Missing |
| GET | `/measurements/{id}/spectrogram` | Get spectrogram data | JSON | ❌ Missing |
| GET | `/measurements/{id}/target-settings` | Get target settings | JSON | ❌ Missing |
| POST | `/measurements/{id}/target-settings` | Set target settings | 200 OK | ❌ Missing |
| PUT | `/measurements/{id}/target-settings` | Update target settings | 200 OK | ❌ Missing |
| GET | `/measurements/{id}/waterfall` | Get waterfall data | JSON | ❌ Missing |
| POST | `/measurements/subscribe` | Subscribe to measurement changes | 200 OK | ❌ Missing |
| GET | `/measurements/subscribers` | Get current subscribers | JSON array | ❌ Missing |
| POST | `/measurements/unsubscribe` | Unsubscribe from changes | 200 OK | ❌ Missing |

**Total Endpoints**: ~38 endpoints in `/measurements/*` group  
**Currently Implemented**: ~3 endpoints (8% coverage)  
**Target**: 100% coverage

---

## 3. Functional Requirements

### REQ-F-CLIENT-003.1: MeasurementsService Domain Service

**ID**: REQ-F-CLIENT-003.1  
**Priority**: Critical  
**Type**: Functional

**Requirement**: The client library SHALL provide a `MeasurementsService` domain service class that exposes all REW REST API `/measurements/*` functionality through an object-oriented interface.

**Acceptance Criteria**:

- ✅ `MeasurementsService` class exists in `src/csharp/Domain/Services/`
- ⚠️ Currently 3/38 REW endpoints accessible (need 35 more methods)
- ✅ Follows ADR-004 layered architecture (Domain → Gateway → Transport)
- ✅ No HTTP/JSON exposure in public API
- ⚠️ Cross-language consistency (C# exists, JS/TS/C++ pending)

**Current Status**: Partially implemented

**Rationale**: Users need OOP access to all measurement functionality without dealing with REST details.

**Traces To**:

- **Architecture**: ADR-004 (Domain Service Architecture)
- **REW API**: `/measurements/*` endpoints
- **Tests**: `TEST-MEASUREMENTS-001` through `TEST-MEASUREMENTS-050`

---

### REQ-F-CLIENT-003.2: Basic Measurement Operations

**ID**: REQ-F-CLIENT-003.2  
**Priority**: Critical  
**Type**: Functional

**Requirement**: The `MeasurementsService` SHALL provide methods for basic measurement CRUD operations.

#### REQ-F-CLIENT-003.2.1: ListMeasurements() - List All Measurements

**Status**: ✅ **IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Lists all measurements currently loaded in REW
/// Maps to: GET /measurements
/// </summary>
/// <returns>Array of measurement summaries</returns>
public async Task<MeasurementSummary[]> ListMeasurements()
```

**REW REST API Mapping**:

- **Endpoint**: `GET /measurements`
- **Response**: JSON object with numeric keys: `{"1": {...}, "2": {...}}`
- **Status Codes**: 200 (success)

**JSON Schema** (from REW Swagger - **ACTUAL FORMAT**):

```json
{
  "1": {
    "title": "string",
    "notes": "string",
    "date": "string",
    "uuid": "string",
    "startFreq": 20.0,
    "endFreq": 20000.0,
    "rewVersion": "string",
    "signalToNoisedB": 85.5
  },
  "2": { "..." }
}
```

**Implementation Status**:

- ✅ Method implemented in `MeasurementsService.cs` (line 238-267)
- ✅ Handles REW's numeric-key object format (not standard array)
- ✅ Parser fixed during REFACTOR phase
- ✅ Unit test: `TEST-MEASUREMENTS-MODERN-001` (GREEN)

**Acceptance Criteria**:

- ✅ Method exists in `MeasurementsService` class
- ✅ Handles REW's non-standard JSON format
- ✅ Returns strongly-typed `MeasurementSummary[]`
- ✅ Unit test coverage >80%
- ✅ Integration test against real REW instance

**Traces To**:

- **REW API**: `GET /measurements`
- **Tests**: `TEST-MEASUREMENTS-MODERN-001`
- **Implementation**: `src/csharp/Domain/Services/MeasurementsService.cs:238-267`

---

#### REQ-F-CLIENT-003.2.2: GetMeasurement() - Get Measurement Details

**Status**: ✅ **IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Gets detailed information for a specific measurement
/// Maps to: GET /measurements/{id}
/// </summary>
/// <param name="uuid">Measurement UUID</param>
/// <returns>Detailed measurement data including summary and processing options</returns>
public async Task<Measurement> GetMeasurement(string uuid)
```

**REW REST API Mapping**:

- **Endpoint**: `GET /measurements/{id}`
- **Request**: UUID in path
- **Response**: JSON with measurement details
- **Status Codes**: 200 (success), 404 (not found)

**JSON Schema** (from REW Swagger):

```json
{
  "summary": {
    "uuid": "string",
    "title": "string",
    "notes": "string",
    "date": "string",
    "startFreq": 20.0,
    "endFreq": 20000.0,
    "rewVersion": "string",
    "signalToNoisedB": 85.5,
    "timingReference": "string",
    "delay": 0.0,
    "splOffsetdB": 0.0
  },
  "processingOptions": {
    "includeCalibration": true,
    "appendLowFrequencyTail": false,
    "frequencyWarping": true
  }
}
```

**Implementation Status**:

- ✅ Method implemented in `MeasurementsService.cs`
- ✅ Returns strongly-typed `Measurement` object
- ✅ Unit test: `TEST-MEASUREMENTS-MODERN-002` (GREEN)

**Acceptance Criteria**:

- ✅ Method exists in `MeasurementsService` class
- ✅ Handles 404 errors gracefully
- ✅ Returns full measurement details
- ✅ Unit test coverage >80%

**Traces To**:

- **REW API**: `GET /measurements/{id}`
- **Tests**: `TEST-MEASUREMENTS-MODERN-002`
- **Implementation**: `src/csharp/Domain/Services/MeasurementsService.cs`

---

#### REQ-F-CLIENT-003.2.3: DeleteMeasurement() - Delete Single Measurement

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Deletes a specific measurement from REW
/// Maps to: DELETE /measurements/{id}
/// </summary>
/// <param name="uuid">Measurement UUID to delete</param>
/// <returns>True if deleted successfully</returns>
public async Task<bool> DeleteMeasurement(string uuid)
```

**REW REST API Mapping**:

- **Endpoint**: `DELETE /measurements/{id}`
- **Request**: UUID in path
- **Response**: 200 OK
- **Status Codes**: 200 (success), 404 (not found)

**Acceptance Criteria**:

- ❌ Method does not exist yet
- ❌ No unit tests
- ❌ Not accessible via OOP interface

**Traces To**:

- **REW API**: `DELETE /measurements/{id}`
- **Tests**: `TEST-MEASUREMENTS-DELETE-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-003.2.4: DeleteAllMeasurements() - Clear All Measurements

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Deletes all measurements from REW
/// Maps to: DELETE /measurements
/// </summary>
/// <returns>Number of measurements deleted</returns>
public async Task<int> DeleteAllMeasurements()
```

**REW REST API Mapping**:

- **Endpoint**: `DELETE /measurements`
- **Response**: 200 OK
- **Status Codes**: 200 (success)

**Acceptance Criteria**:

- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:

- **REW API**: `DELETE /measurements`
- **Tests**: `TEST-MEASUREMENTS-DELETE-ALL-001` (TO BE CREATED)

---

### REQ-F-CLIENT-003.3: Frequency Response Data Retrieval

**ID**: REQ-F-CLIENT-003.3  
**Priority**: High  
**Type**: Functional

**Requirement**: The `MeasurementsService` SHALL provide methods to retrieve frequency response data from measurements.

#### REQ-F-CLIENT-003.3.1: GetFrequencyResponse() - Get Frequency Response

**Status**: ⚠️ **IMPLEMENTED BUT BROKEN** (Magnitudes array empty)

**Method Signature** (C#):

```csharp
/// <summary>
/// Gets frequency response data for a measurement
/// Maps to: GET /measurements/{id}/frequency-response?smoothing={smoothing}
/// </summary>
/// <param name="uuid">Measurement UUID</param>
/// <param name="smoothing">Smoothing setting (e.g., "1/48 octave", "None")</param>
/// <returns>Frequency response with frequencies and magnitudes</returns>
public async Task<FrequencyResponse> GetFrequencyResponse(string uuid, string smoothing = "None")
```

**REW REST API Mapping**:

- **Endpoint**: `GET /measurements/{id}/frequency-response?smoothing={smoothing}`
- **Request**: UUID in path, smoothing in query
- **Response**: JSON with frequency/magnitude arrays (Base64 big-endian floats)
- **Status Codes**: 200 (success), 404 (not found)

**JSON Schema** (from REW Swagger):

```json
{
  "smoothing": "1/48 octave",
  "frequencies": "Base64EncodedBigEndianFloatArray",
  "magnitudes": "Base64EncodedBigEndianFloatArray",
  "phases": "Base64EncodedBigEndianFloatArray"
}
```

**Implementation Status**:

- ⚠️ Method implemented in `MeasurementsService.cs`
- ❌ **BUG**: Returns empty `Magnitudes` array
- ⚠️ Test: `TEST-MEASUREMENTS-FEATURE-FREQ-001` (SKIPPED - bug)

**Known Issues**:

1. **Empty Magnitudes Bug**: Magnitudes array comes back empty from REW
2. **Root Cause**: Unknown - needs investigation against REW Swagger
3. **Impact**: Frequency analysis features blocked

**Acceptance Criteria**:

- ⚠️ Method exists but not working correctly
- ❌ Magnitudes array must not be empty
- ❌ ArrayCodec must decode Base64 big-endian floats correctly
- ❌ Unit test must pass

**Traces To**:

- **REW API**: `GET /measurements/{id}/frequency-response`
- **Tests**: `TEST-MEASUREMENTS-FEATURE-FREQ-001`
- **Bug**: GitHub Issue #[TBD] - GetFrequencyResponse returns empty Magnitudes

---

### REQ-F-CLIENT-003.4: Impulse Response Data Retrieval

**ID**: REQ-F-CLIENT-003.4  
**Priority**: High  
**Type**: Functional

**Requirement**: The `MeasurementsService` SHALL provide methods to retrieve impulse response data from measurements.

#### REQ-F-CLIENT-003.4.1: GetImpulseResponse() - Get Impulse Response

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Gets impulse response data for a measurement
/// Maps to: GET /measurements/{id}/impulse-response
/// </summary>
/// <param name="uuid">Measurement UUID</param>
/// <returns>Impulse response with time and amplitude data</returns>
public async Task<ImpulseResponse> GetImpulseResponse(string uuid)
```

**REW REST API Mapping**:

- **Endpoint**: `GET /measurements/{id}/impulse-response`
- **Request**: UUID in path
- **Response**: JSON with time/amplitude arrays (Base64 big-endian floats)
- **Status Codes**: 200 (success), 404 (not found)

**JSON Schema** (from REW Swagger):

```json
{
  "startTime": 0.0,
  "endTime": 0.5,
  "sampleRate": 48000,
  "times": "Base64EncodedBigEndianFloatArray",
  "amplitudes": "Base64EncodedBigEndianFloatArray"
}
```

**Acceptance Criteria**:

- ❌ Method does not exist yet
- ❌ No unit tests
- ❌ ArrayCodec integration for Base64 decoding

**Traces To**:

- **REW API**: `GET /measurements/{id}/impulse-response`
- **Tests**: `TEST-MEASUREMENTS-IMPULSE-001` (TO BE CREATED)

---

### REQ-F-CLIENT-003.5: RT60 Reverberation Time Analysis

**ID**: REQ-F-CLIENT-003.5  
**Priority**: Medium  
**Type**: Functional

**Requirement**: The `MeasurementsService` SHALL provide methods to retrieve RT60 reverberation time data.

#### REQ-F-CLIENT-003.5.1: GetRT60() - Get Reverberation Time

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):

```csharp
/// <summary>
/// Gets RT60 reverberation time data for a measurement
/// Maps to: GET /measurements/{id}/rt60
/// </summary>
/// <param name="uuid">Measurement UUID</param>
/// <returns>RT60 data by frequency band</returns>
public async Task<RT60Data> GetRT60(string uuid)
```

**REW REST API Mapping**:

- **Endpoint**: `GET /measurements/{id}/rt60`
- **Response**: JSON with RT60, T20, T30 values by frequency
- **Status Codes**: 200 (success), 404 (not found)

**JSON Schema** (from REW Swagger):

```json
{
  "frequencies": [31.5, 63, 125, 250, 500, 1000, 2000, 4000, 8000],
  "EDT": [0.45, 0.52, 0.48, ...],
  "T20": [0.50, 0.55, 0.53, ...],
  "T30": [0.52, 0.58, 0.55, ...],
  "T60M": [0.53, 0.59, 0.56, ...]
}
```

**Acceptance Criteria**:

- ❌ Method does not exist yet
- ❌ No domain objects for RT60 data
- ❌ No unit tests

**Traces To**:

- **REW API**: `GET /measurements/{id}/rt60`
- **Tests**: `TEST-MEASUREMENTS-RT60-001` (TO BE CREATED)

---

## 4. Summary: Measurements API Coverage

| Category | Total Endpoints | Implemented | Status |
|---|---|---|---|
| **Basic CRUD** | 5 | 2 | ⚠️ 40% |
| **Data Retrieval** | 8 | 1 (broken) | ❌ 12.5% |
| **EQ/Filters** | 8 | 0 | ❌ 0% |
| **Processing** | 6 | 0 | ❌ 0% |
| **Subscriptions** | 3 | 0 | ❌ 0% |
| **Advanced Analysis** | 8 | 0 | ❌ 0% |
| **TOTAL** | 38 | 3 | ⚠️ **8%** |

**Critical Gaps**:

1. ❌ 35 endpoints not exposed via OOP interface
2. ⚠️ GetFrequencyResponse broken (empty Magnitudes)
3. ❌ No EQ/filter management
4. ❌ No RT60, waterfall, spectrogram
5. ❌ No subscription model implementation

---

## 5. Test Requirements

### REQ-F-CLIENT-003.6: Test Coverage

**Requirement**: SHALL have comprehensive automated tests for all functionality.

**Current Status**:

- ✅ Unit Tests: 3 tests (all GREEN)
- ⚠️ Feature Tests: 3 tests (all SKIPPED)
- ❌ Integration Tests: None
- **Coverage**: ~8% of Measurements API functionality

**Required**:

- 38 unit tests (one per endpoint)
- 10+ integration tests (workflows)
- >80% code coverage

**Traces To**:

- **Tests**: `tests/csharp/UnitTest1.cs` - REWMeasurementsModernApiTests
- **Tests**: `tests/csharp/REWMeasurementsFeatureTests.cs`

---

## 6. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ All 38 REW `/measurements/*` endpoints accessible via OOP interface
- ✅ `MeasurementsService` class implements ADR-004 architecture
- ✅ Domain objects encapsulate REW concepts (no REST exposure)
- ✅ GetFrequencyResponse bug fixed (Magnitudes not empty)
- ✅ >80% unit test coverage
- ✅ Integration tests pass against real REW
- ✅ Cross-language parity verified (C#, C++, JS, TS)
- ✅ Documentation complete (XML comments + examples)

**Current Status**: ⚠️ **8% Complete** (3/38 endpoints)

---

## 7. References

- **REW REST API**: <http://localhost:4735/> (Swagger documentation)
- **Architecture**: `03-architecture/views/object-model.md`
- **ADR-004**: `03-architecture/decisions/ADR-004-simple-object-model-pattern.md`
- **Tests**: `tests/csharp/UnitTest1.cs`, `tests/csharp/REWMeasurementsFeatureTests.cs`
- **Implementation**: `src/csharp/Domain/Services/MeasurementsService.cs`
- **Gap Analysis**: `02-requirements/functional/API_COMPLETENESS_GAP_ANALYSIS.md`

---

**Document Owner**: Requirements Team  
**Status**: Active (In Progress - 8% complete)  
**Next Review**: After each endpoint implementation milestone
