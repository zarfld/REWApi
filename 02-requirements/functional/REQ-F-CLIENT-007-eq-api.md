---
title: "REW Client Library Requirements - EQ API"
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
    - TEST-EQ-XXX
description: "Functional requirements for EQService exposing REW REST API /eq/* endpoints"
---

# REW Client Library - EQ API Requirements

**Document ID**: REQ-F-CLIENT-007  
**REW REST API Resource**: `/eq/*`  
**Client Library Component**: `EQService.cs` (Domain Service - NEW)  
**Standards**: ISO/IEC/IEEE 29148:2018  
**REW Swagger**: <http://localhost:4735/>  
**Status**: ❌ **0% Coverage - Service Does Not Exist**

---

## 1. Purpose

Define functional requirements for the **EQService** domain service class that provides object-oriented access to REW REST API `/eq/*` endpoints for equalizer and target curve management.

**What We're Building**:

- **Client Library Component**: `EQService` class (NEW - create from scratch)
- **Consumes**: REW REST API `/eq/*` endpoints (~30 endpoints)
- **Provides**: OOP interface for EQ configuration, target curves, room correction
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints

**Total Endpoints**: ~30 endpoints in `/eq/*` group  
**Currently Implemented**: 0 endpoints (0% coverage)  
**Target**: 100% coverage

### Key Endpoint Categories

1. **Default EQ Settings** (~5 endpoints)
   - GET/POST `/eq/default/filter-tasks` - Default filter tasks
   - GET/POST `/eq/default/target-settings` - Default target settings
   - GET/POST `/eq/default/optimization-settings` - Optimization settings

2. **Room Curve Settings** (~6 endpoints)
   - GET/POST `/eq/room-curve/enabled` - Enable/disable room curve
   - GET/POST `/eq/room-curve/slope` - Room curve slope
   - GET/POST `/eq/room-curve/type` - Room curve type
   - GET/POST `/eq/room-curve/frequency-range` - Frequency range

3. **Target Settings** (~8 endpoints)
   - GET/POST `/eq/target/type` - Target type (flat, house curve, custom)
   - GET/POST `/eq/target/level` - Target level
   - GET/POST `/eq/target/slope` - Target slope
   - GET/POST `/eq/target/frequencies` - Target frequencies array
   - GET/POST `/eq/target/magnitudes` - Target magnitudes array
   - GET `/eq/target/response` - Get full target response curve

4. **House Curve Management** (~5 endpoints)
   - GET `/eq/house-curves` - List available house curves
   - GET `/eq/house-curve/{name}` - Get specific house curve
   - POST `/eq/house-curve` - Create house curve
   - PUT `/eq/house-curve/{name}` - Update house curve
   - DELETE `/eq/house-curve/{name}` - Delete house curve

5. **EQ Manufacturers/Devices** (~4 endpoints)
   - GET `/eq/manufacturers` - List EQ manufacturers
   - GET `/eq/manufacturer/{name}/devices` - List devices by manufacturer
   - GET/POST `/eq/selected-device` - Get/set selected EQ device

6. **Match Target Functions** (~2 endpoints)
   - POST `/eq/match-target` - Generate EQ to match target
   - GET `/eq/match-target/result` - Get match result

---

## 3. Functional Requirements

### REQ-F-CLIENT-007.1: EQService Domain Service

**Requirement**: The client library SHALL provide an `EQService` domain service class exposing all REW REST API `/eq/*` functionality for EQ and target management.

**Current Status**: ❌ **Service does not exist**

**Implementation Required**:

```csharp
namespace REWApi.Domain.Services
{
    /// <summary>
    /// Provides access to REW Equalizer and Target Curve functionality
    /// Maps to REW REST API /eq/* endpoints
    /// </summary>
    public class EQService
    {
        // Default EQ Settings
        Task<FilterTask[]> GetDefaultFilterTasks()
        Task<bool> SetDefaultFilterTasks(FilterTask[] tasks)
        Task<TargetSettings> GetDefaultTargetSettings()
        Task<bool> SetDefaultTargetSettings(TargetSettings settings)
        Task<OptimizationSettings> GetDefaultOptimizationSettings()
        Task<bool> SetDefaultOptimizationSettings(OptimizationSettings settings)
        
        // Room Curve
        Task<bool> GetRoomCurveEnabled()
        Task<bool> SetRoomCurveEnabled(bool enabled)
        Task<double> GetRoomCurveSlope()
        Task<bool> SetRoomCurveSlope(double slopedBPerOctave)
        Task<RoomCurveType> GetRoomCurveType()
        Task<bool> SetRoomCurveType(RoomCurveType type)
        Task<FrequencyRange> GetRoomCurveFrequencyRange()
        Task<bool> SetRoomCurveFrequencyRange(double minHz, double maxHz)
        
        // Target Settings
        Task<TargetType> GetTargetType()
        Task<bool> SetTargetType(TargetType type)
        Task<double> GetTargetLevel()
        Task<bool> SetTargetLevel(double leveldB)
        Task<double> GetTargetSlope()
        Task<bool> SetTargetSlope(double slopedBPerOctave)
        Task<double[]> GetTargetFrequencies()
        Task<bool> SetTargetFrequencies(double[] frequenciesHz)
        Task<double[]> GetTargetMagnitudes()
        Task<bool> SetTargetMagnitudes(double[] magnitudesdB)
        Task<TargetResponse> GetTargetResponse()
        
        // House Curves
        Task<HouseCurve[]> GetHouseCurves()
        Task<HouseCurve> GetHouseCurve(string name)
        Task<bool> CreateHouseCurve(HouseCurve houseCurve)
        Task<bool> UpdateHouseCurve(string name, HouseCurve houseCurve)
        Task<bool> DeleteHouseCurve(string name)
        
        // EQ Devices
        Task<string[]> GetManufacturers()
        Task<EQDevice[]> GetDevicesByManufacturer(string manufacturer)
        Task<EQDevice> GetSelectedDevice()
        Task<bool> SetSelectedDevice(string manufacturer, string deviceName)
        
        // Match Target
        Task<EQFilterSet> MatchTarget(string measurementUuid, TargetSettings targetSettings)
        Task<MatchResult> GetMatchTargetResult(string jobId)
    }
}
```

---

## 4. Domain Objects

### TargetType Enum

```csharp
public enum TargetType
{
    Flat,
    HouseCurve,
    RoomCurve,
    Custom
}
```

### RoomCurveType Enum

```csharp
public enum RoomCurveType
{
    Linear,
    BK,
    Harman
}
```

### TargetSettings Class

```csharp
/// <summary>
/// Target curve settings
/// </summary>
public class TargetSettings
{
    public TargetType Type { get; set; }
    public double LeveldB { get; set; }
    public double SlopedBPerOctave { get; set; }
    public string HouseCurveName { get; set; }
    public double[] CustomFrequencies { get; set; }
    public double[] CustomMagnitudes { get; set; }
}
```

### FilterTask Class

```csharp
public class FilterTask
{
    public string TaskName { get; set; }
    public double FrequencyHz { get; set; }
    public double GaindB { get; set; }
    public double Q { get; set; }
    public FilterType FilterType { get; set; }
}
```

### HouseCurve Class

```csharp
public class HouseCurve
{
    public string Name { get; set; }
    public double[] Frequencies { get; set; }
    public double[] Magnitudes { get; set; }
    public string Description { get; set; }
}
```

### EQDevice Class

```csharp
public class EQDevice
{
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public int MaxFilters { get; set; }
    public FilterType[] SupportedFilterTypes { get; set; }
}
```

### EQFilterSet Class

```csharp
public class EQFilterSet
{
    public EQFilter[] Filters { get; set; }
    public EQDevice TargetDevice { get; set; }
}
```

### EQFilter Class

```csharp
public class EQFilter
{
    public double FrequencyHz { get; set; }
    public double GaindB { get; set; }
    public double Q { get; set; }
    public FilterType FilterType { get; set; }
}
```

### FilterType Enum

```csharp
public enum FilterType
{
    PEQ,            // Parametric EQ
    LowShelf,
    HighShelf,
    LowPass,
    HighPass,
    Notch,
    AllPass
}
```

---

## 5. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ `EQService` class exists in `src/csharp/Domain/Services/`
- ✅ All ~30 REW `/eq/*` endpoints accessible
- ✅ Domain objects (TargetSettings, HouseCurve, EQFilter, etc.) implemented
- ✅ Follows ADR-004 architecture
- ✅ >80% unit test coverage (~30 tests)
- ✅ Integration tests for EQ matching workflow
- ✅ Cross-language parity (C#, C++, JS, TS)

**Current Status**: ❌ **0% Complete** (service does not exist)

---

## 6. References

- **REW REST API**: <http://localhost:4735/>
- **Architecture**: ADR-004
- **Gap Analysis**: `API_COMPLETENESS_GAP_ANALYSIS.md`
- **Implementation**: `src/csharp/Domain/Services/EQService.cs` (TO BE CREATED)
- **Tests**: `tests/csharp/EQServiceTests.cs` (TO BE CREATED)

---

**Document Owner**: Requirements Team  
**Status**: Draft (0% implemented - new service required)
