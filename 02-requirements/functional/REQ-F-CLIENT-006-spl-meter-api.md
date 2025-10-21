---
title: "REW Client Library Requirements - SPL Meter API"
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
  verification:
    - TEST-SPLMETER-XXX
description: "Functional requirements for SPLMeterService exposing REW REST API /spl-meter/* endpoints"
---

# REW Client Library - SPL Meter API Requirements

**Document ID**: REQ-F-CLIENT-006  
**REW REST API Resource**: `/spl-meter/*`  
**Client Library Component**: `SPLMeterService.cs` (Domain Service - NEW)  
**Standards**: ISO/IEC/IEEE 29148:2018  
**REW Swagger**: <http://localhost:4735/>  
**Status**: ❌ **0% Coverage - Service Does Not Exist**

---

## 1. Purpose

Define functional requirements for the **SPLMeterService** domain service class that provides object-oriented access to REW REST API `/spl-meter/*` endpoints for sound pressure level monitoring.

**What We're Building**:

- **Client Library Component**: `SPLMeterService` class (NEW - create from scratch)
- **Consumes**: REW REST API `/spl-meter/*` endpoints (~10 endpoints)
- **Provides**: OOP interface for SPL meter with live level streaming
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints

**Total Endpoints**: ~10 endpoints in `/spl-meter/*` group  
**Currently Implemented**: 0 endpoints (0% coverage)  
**Target**: 100% coverage

### Key Endpoint Categories

1. **SPL Meter Control** (~3 endpoints)
   - POST `/spl-meter/start` - Start SPL meter
   - POST `/spl-meter/stop` - Stop SPL meter
   - GET `/spl-meter/running` - Check if running

2. **Configuration** (~5 endpoints)
   - GET/POST `/spl-meter/mode` - Mode (SPL, LEQ, SEL)
   - GET/POST `/spl-meter/weighting` - Weighting (A, C, Z)
   - GET/POST `/spl-meter/time-weighting` - Time weighting (Fast, Slow, Impulse)
   - GET/POST `/spl-meter/reference-level` - Reference level (dBFS to dB SPL offset)

3. **Data Streaming** (~3 endpoints)
   - POST `/spl-meter/levels/subscribe` - Subscribe to live levels
   - GET `/spl-meter/levels/subscribers` - List subscribers
   - POST `/spl-meter/levels/unsubscribe` - Unsubscribe

4. **Data Retrieval** (~2 endpoints)
   - GET `/spl-meter/current-level` - Get current SPL reading
   - GET `/spl-meter/peak-level` - Get peak SPL

---

## 3. Functional Requirements

### REQ-F-CLIENT-006.1: SPLMeterService Domain Service

**Requirement**: The client library SHALL provide an `SPLMeterService` domain service class exposing all REW REST API `/spl-meter/*` functionality with real-time level streaming.

**Current Status**: ❌ **Service does not exist**

**Implementation Required**:

```csharp
namespace REWApi.Domain.Services
{
    /// <summary>
    /// Provides access to REW SPL Meter functionality
    /// Maps to REW REST API /spl-meter/* endpoints
    /// </summary>
    public class SPLMeterService
    {
        // Control
        Task<bool> Start()
        Task<bool> Stop()
        Task<bool> IsRunning()
        
        // Configuration
        Task<SPLMode> GetMode()
        Task<bool> SetMode(SPLMode mode)
        Task<Weighting> GetWeighting()
        Task<bool> SetWeighting(Weighting weighting)
        Task<TimeWeighting> GetTimeWeighting()
        Task<bool> SetTimeWeighting(TimeWeighting timeWeighting)
        Task<double> GetReferenceLevel()
        Task<bool> SetReferenceLevel(double dBFStoSPLOffset)
        
        // Data Retrieval (Polling)
        Task<SPLReading> GetCurrentLevel()
        Task<SPLReading> GetPeakLevel()
        
        // Data Streaming (Subscriptions) - requires SubscriptionManager
        Task<string> SubscribeToLevels(Action<SPLReading> handler)
        Task<SubscriptionInfo[]> GetLevelSubscribers()
        Task<bool> UnsubscribeFromLevels(string subscriptionId)
    }
}
```

---

## 4. Domain Objects

### SPLMode Enum

```csharp
public enum SPLMode
{
    SPL,        // Instantaneous SPL
    LEQ,        // Equivalent continuous sound level
    SEL         // Sound exposure level
}
```

### Weighting Enum

```csharp
public enum Weighting
{
    A,          // A-weighting (most common)
    C,          // C-weighting
    Z           // Z-weighting (flat/unweighted)
}
```

### TimeWeighting Enum

```csharp
public enum TimeWeighting
{
    Fast,       // 125ms time constant
    Slow,       // 1000ms time constant
    Impulse     // 35ms rise, 1500ms decay
}
```

### SPLReading Class

```csharp
/// <summary>
/// Represents an SPL meter reading
/// </summary>
public class SPLReading
{
    public DateTime Timestamp { get; set; }
    public double LeveldB { get; set; }
    public SPLMode Mode { get; set; }
    public Weighting Weighting { get; set; }
    public TimeWeighting TimeWeighting { get; set; }
}
```

### SPLMeterConfiguration Class

```csharp
/// <summary>
/// Complete SPL meter configuration
/// </summary>
public class SPLMeterConfiguration
{
    public SPLMode Mode { get; set; }
    public Weighting Weighting { get; set; }
    public TimeWeighting TimeWeighting { get; set; }
    public double ReferenceLeveldB { get; set; }
}
```

---

## 5. Subscription Requirements

**Critical Dependency**: SPL meter streaming requires **SubscriptionManager** infrastructure (TODO #20).

**REW Subscription Pattern**:

1. Client calls `POST /spl-meter/levels/subscribe` with callback URL
2. REW starts HTTP POST callbacks with live level data
3. Client's embedded HTTP listener receives readings
4. Client triggers user's handler function with SPLReading

---

## 6. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ `SPLMeterService` class exists in `src/csharp/Domain/Services/`
- ✅ All ~10 REW `/spl-meter/*` endpoints accessible
- ✅ Domain objects (SPLMode, Weighting, TimeWeighting, SPLReading) implemented
- ✅ Subscription infrastructure working (SubscriptionManager)
- ✅ Real-time level streaming validated
- ✅ Follows ADR-004 architecture
- ✅ >80% unit test coverage (~10 tests)
- ✅ Integration tests with live SPL monitoring
- ✅ Cross-language parity (C#, C++, JS, TS)

**Current Status**: ❌ **0% Complete** (service does not exist, blocked by TODO #20)

---

## 7. References

- **REW REST API**: <http://localhost:4735/>
- **Architecture**: ADR-004
- **Gap Analysis**: `API_COMPLETENESS_GAP_ANALYSIS.md`
- **Dependency**: TODO #20 (SubscriptionManager Infrastructure)
- **Implementation**: `src/csharp/Domain/Services/SPLMeterService.cs` (TO BE CREATED)
- **Tests**: `tests/csharp/SPLMeterServiceTests.cs` (TO BE CREATED)

---

**Document Owner**: Requirements Team  
**Status**: Draft (0% implemented - new service required, blocked by SubscriptionManager)
