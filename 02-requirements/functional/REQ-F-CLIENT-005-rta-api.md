---
title: "REW Client Library Requirements - RTA API"
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
    - TEST-RTA-XXX
description: "Functional requirements for RTAService exposing REW REST API /rta/* endpoints"
---

# REW Client Library - RTA (Real-Time Analyzer) API Requirements

**Document ID**: REQ-F-CLIENT-005  
**REW REST API Resource**: `/rta/*`  
**Client Library Component**: `RTAService.cs` (Domain Service - NEW)  
**Standards**: ISO/IEC/IEEE 29148:2018  
**REW Swagger**: <http://localhost:4735/>  
**Status**: ❌ **0% Coverage - Service Does Not Exist**

---

## 1. Purpose

Define functional requirements for the **RTAService** domain service class that provides object-oriented access to REW REST API `/rta/*` endpoints for real-time frequency analysis.

**What We're Building**:

- **Client Library Component**: `RTAService` class (NEW - create from scratch)
- **Consumes**: REW REST API `/rta/*` endpoints (~40 endpoints)
- **Provides**: OOP interface for real-time analyzer with live data streaming
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints

**Total Endpoints**: ~40 endpoints in `/rta/*` group  
**Currently Implemented**: 0 endpoints (0% coverage)  
**Target**: 100% coverage

### Key Endpoint Categories

1. **RTA Control** (~5 endpoints)
   - POST `/rta/start` - Start RTA
   - POST `/rta/stop` - Stop RTA
   - GET `/rta/running` - Check if RTA is running
   - POST `/rta/pause` - Pause RTA
   - POST `/rta/resume` - Resume RTA

2. **Configuration** (~15 endpoints)
   - GET/POST `/rta/fft-length` - FFT size
   - GET/POST `/rta/window-type` - Window function
   - GET/POST `/rta/overlap` - Window overlap percentage
   - GET/POST `/rta/averaging` - Averaging type (exponential, linear, peak hold)
   - GET/POST `/rta/averaging-time` - Averaging time constant
   - GET/POST `/rta/smoothing` - Fractional octave smoothing
   - GET/POST `/rta/frequency-range` - Min/max frequency display

3. **Data Subscriptions** (~8 endpoints)
   - POST `/rta/captured-data/subscribe` - Subscribe to live RTA data
   - GET `/rta/captured-data/subscribers` - List subscribers
   - POST `/rta/captured-data/unsubscribe` - Unsubscribe
   - POST `/rta/peak-data/subscribe` - Subscribe to peak data
   - GET `/rta/peak-data/subscribers` - List peak subscribers
   - POST `/rta/peak-data/unsubscribe` - Unsubscribe from peaks

4. **Data Retrieval** (~6 endpoints)
   - GET `/rta/captured-data` - Get current RTA spectrum
   - GET `/rta/peak-data` - Get peak hold spectrum
   - GET `/rta/average-data` - Get averaged spectrum
   - GET `/rta/instantaneous-data` - Get instantaneous spectrum

5. **Appearance/Display** (~6 endpoints)
   - GET/POST `/rta/show-grid` - Grid visibility
   - GET/POST `/rta/show-legend` - Legend visibility
   - GET/POST `/rta/background-color` - Background color
   - GET/POST `/rta/trace-color` - Trace color

---

## 3. Functional Requirements

### REQ-F-CLIENT-005.1: RTAService Domain Service

**Requirement**: The client library SHALL provide an `RTAService` domain service class exposing all REW REST API `/rta/*` functionality with real-time data streaming support.

**Current Status**: ❌ **Service does not exist**

**Implementation Required**:

```csharp
namespace REWApi.Domain.Services
{
    /// <summary>
    /// Provides access to REW Real-Time Analyzer functionality
    /// Maps to REW REST API /rta/* endpoints
    /// </summary>
    public class RTAService
    {
        // RTA Control
        Task<bool> Start()
        Task<bool> Stop()
        Task<bool> IsRunning()
        Task<bool> Pause()
        Task<bool> Resume()
        
        // Configuration
        Task<int> GetFFTLength()
        Task<bool> SetFFTLength(int fftSize)
        Task<WindowType> GetWindowType()
        Task<bool> SetWindowType(WindowType windowType)
        Task<double> GetOverlap()
        Task<bool> SetOverlap(double overlapPercent)
        Task<AveragingType> GetAveraging()
        Task<bool> SetAveraging(AveragingType averagingType)
        Task<double> GetAveragingTime()
        Task<bool> SetAveragingTime(double seconds)
        Task<string> GetSmoothing()
        Task<bool> SetSmoothing(string smoothing)
        Task<FrequencyRange> GetFrequencyRange()
        Task<bool> SetFrequencyRange(double minHz, double maxHz)
        
        // Data Retrieval (Polling)
        Task<RTASpectrum> GetCapturedData()
        Task<RTASpectrum> GetPeakData()
        Task<RTASpectrum> GetAverageData()
        Task<RTASpectrum> GetInstantaneousData()
        
        // Data Streaming (Subscriptions) - requires SubscriptionManager
        Task<string> SubscribeToCapturedData(Action<RTASpectrum> handler)
        Task<SubscriptionInfo[]> GetCapturedDataSubscribers()
        Task<bool> UnsubscribeFromCapturedData(string subscriptionId)
        
        Task<string> SubscribeToPeakData(Action<RTASpectrum> handler)
        Task<SubscriptionInfo[]> GetPeakDataSubscribers()
        Task<bool> UnsubscribeFromPeakData(string subscriptionId)
        
        // Appearance
        Task<bool> GetShowGrid()
        Task<bool> SetShowGrid(bool show)
        Task<bool> GetShowLegend()
        Task<bool> SetShowLegend(bool show)
        Task<string> GetBackgroundColor()
        Task<bool> SetBackgroundColor(string color)
        Task<string> GetTraceColor()
        Task<bool> SetTraceColor(string color)
    }
}
```

---

## 4. Domain Objects

### WindowType Enum

```csharp
public enum WindowType
{
    Rectangular,
    Hamming,
    Hann,
    Blackman,
    BlackmanHarris,
    FlatTop
}
```

### AveragingType Enum

```csharp
public enum AveragingType
{
    None,
    Exponential,
    Linear,
    PeakHold
}
```

### RTASpectrum Class

```csharp
/// <summary>
/// Represents RTA frequency spectrum data
/// </summary>
public class RTASpectrum
{
    public DateTime Timestamp { get; set; }
    public double[] Frequencies { get; set; }
    public double[] MagnitudesdB { get; set; }
    public double SampleRate { get; set; }
    public int FFTLength { get; set; }
    public string Smoothing { get; set; }
}
```

### FrequencyRange Class

```csharp
public class FrequencyRange
{
    public double MinHz { get; set; }
    public double MaxHz { get; set; }
}
```

### RTAConfiguration Class

```csharp
/// <summary>
/// Complete RTA configuration
/// </summary>
public class RTAConfiguration
{
    public int FFTLength { get; set; }
    public WindowType WindowType { get; set; }
    public double OverlapPercent { get; set; }
    public AveragingType Averaging { get; set; }
    public double AveragingTimeSeconds { get; set; }
    public string Smoothing { get; set; }
    public FrequencyRange FrequencyRange { get; set; }
    public bool ShowGrid { get; set; }
    public bool ShowLegend { get; set; }
    public string BackgroundColor { get; set; }
    public string TraceColor { get; set; }
}
```

---

## 5. Subscription Requirements

**Critical Dependency**: RTA data streaming requires **SubscriptionManager** infrastructure (TODO #20).

**REW Subscription Pattern**:

1. Client calls `POST /rta/captured-data/subscribe` with callback URL
2. REW starts HTTP POST callbacks to client with live data
3. Client's embedded HTTP listener receives spectrum data
4. Client triggers user's handler function

**Implementation Notes**:

- Requires embedded HTTP server in client library
- Callback URL format: `http://client-ip:port/rta/captured-data/callback`
- REW sends JSON spectrum data at configured rate
- Must handle network errors and reconnection

---

## 6. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ `RTAService` class exists in `src/csharp/Domain/Services/`
- ✅ All ~40 REW `/rta/*` endpoints accessible
- ✅ Domain objects (WindowType, AveragingType, RTASpectrum, RTAConfiguration) implemented
- ✅ Subscription infrastructure working (SubscriptionManager)
- ✅ Real-time data streaming validated
- ✅ Follows ADR-004 architecture
- ✅ >80% unit test coverage (~40 tests)
- ✅ Integration tests with live RTA streaming
- ✅ Cross-language parity (C#, C++, JS, TS)

**Current Status**: ❌ **0% Complete** (service does not exist, blocked by TODO #20)

---

## 7. References

- **REW REST API**: <http://localhost:4735/>
- **Architecture**: ADR-004
- **Gap Analysis**: `API_COMPLETENESS_GAP_ANALYSIS.md`
- **Dependency**: TODO #20 (SubscriptionManager Infrastructure)
- **Implementation**: `src/csharp/Domain/Services/RTAService.cs` (TO BE CREATED)
- **Tests**: `tests/csharp/RTAServiceTests.cs` (TO BE CREATED)

---

**Document Owner**: Requirements Team  
**Status**: Draft (0% implemented - new service required, blocked by SubscriptionManager)
