---
title: "REW Client Library Requirements - Generator API"
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
    - TEST-GENERATOR-XXX
description: "Functional requirements for GeneratorService exposing REW REST API /generator/* endpoints"
---

# REW Client Library - Generator API Requirements

**Document ID**: REQ-F-CLIENT-004  
**REW REST API Resource**: `/generator/*`  
**Client Library Component**: `GeneratorService.cs` (Domain Service - NEW)  
**Standards**: ISO/IEC/IEEE 29148:2018  
**REW Swagger**: <http://localhost:4735/>  
**Status**: ❌ **0% Coverage - Service Does Not Exist**

---

## 1. Purpose

Define functional requirements for the **GeneratorService** domain service class that provides object-oriented access to REW REST API `/generator/*` endpoints for signal generation control.

**What We're Building**:

- **Client Library Component**: `GeneratorService` class (NEW - create from scratch)
- **Consumes**: REW REST API `/generator/*` endpoints (~40 endpoints)
- **Provides**: OOP interface for signal generator (sine, pink noise, sweeps, etc.)
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints

**Total Endpoints**: ~40 endpoints in `/generator/*` group  
**Currently Implemented**: 0 endpoints (0% coverage)  
**Target**: 100% coverage

### Key Endpoint Categories

1. **Signal Type Control** (~8 endpoints)
   - GET/POST `/generator/signal-type` - Set signal type (sine, pink noise, white noise, sweep, WAV file, etc.)
   - GET `/generator/signal-types` - List available signal types

2. **Frequency Control** (~6 endpoints)
   - GET/POST `/generator/frequency` - Set generator frequency
   - GET/POST `/generator/start-frequency` - Sweep start frequency
   - GET/POST `/generator/end-frequency` - Sweep end frequency

3. **Level Control** (~5 endpoints)
   - GET/POST `/generator/level` - Set output level
   - GET/POST `/generator/rms-level` - Set RMS level
   - GET/POST `/generator/peak-level` - Set peak level

4. **Signal Configuration** (~8 endpoints)
   - GET/POST `/generator/sweep-length` - Sweep duration
   - GET/POST `/generator/sweep-type` - Linear/log sweep
   - GET/POST `/generator/fade-in` - Fade in time
   - GET/POST `/generator/fade-out` - Fade out time

5. **Protection** (~4 endpoints)
   - GET/POST `/generator/clip-protection` - Enable/disable clip protection
   - GET/POST `/generator/clip-protection-level` - Set protection level

6. **Generator Control** (~6 endpoints)
   - POST `/generator/start` - Start signal generation
   - POST `/generator/stop` - Stop signal generation
   - GET `/generator/running` - Check if running
   - POST `/generator/command` - Execute generator command
   - GET `/generator/commands` - List available commands

7. **Other Configuration** (~3 endpoints)
   - GET/POST `/generator/channel` - Set output channel
   - GET/POST `/generator/wav-file` - Set WAV file path for playback

---

## 3. Functional Requirements

### REQ-F-CLIENT-004.1: GeneratorService Domain Service

**Requirement**: The client library SHALL provide a `GeneratorService` domain service class exposing all REW REST API `/generator/*` functionality.

**Current Status**: ❌ **Service does not exist**

**Implementation Required**:

```csharp
namespace REWApi.Domain.Services
{
    /// <summary>
    /// Provides access to REW signal generator functionality
    /// Maps to REW REST API /generator/* endpoints
    /// </summary>
    public class GeneratorService
    {
        // Signal Type
        Task<SignalType> GetSignalType()
        Task<bool> SetSignalType(SignalType signalType)
        Task<SignalType[]> GetAvailableSignalTypes()
        
        // Frequency Control
        Task<double> GetFrequency()
        Task<bool> SetFrequency(double frequencyHz)
        Task<double> GetStartFrequency()
        Task<bool> SetStartFrequency(double frequencyHz)
        Task<double> GetEndFrequency()
        Task<bool> SetEndFrequency(double frequencyHz)
        
        // Level Control
        Task<double> GetLevel()
        Task<bool> SetLevel(double leveldB)
        Task<double> GetRMSLevel()
        Task<bool> SetRMSLevel(double leveldB)
        Task<double> GetPeakLevel()
        Task<bool> SetPeakLevel(double leveldB)
        
        // Sweep Configuration
        Task<double> GetSweepLength()
        Task<bool> SetSweepLength(double seconds)
        Task<SweepType> GetSweepType()
        Task<bool> SetSweepType(SweepType sweepType)
        
        // Fade Configuration
        Task<double> GetFadeIn()
        Task<bool> SetFadeIn(double seconds)
        Task<double> GetFadeOut()
        Task<bool> SetFadeOut(double seconds)
        
        // Clip Protection
        Task<bool> GetClipProtection()
        Task<bool> SetClipProtection(bool enabled)
        Task<double> GetClipProtectionLevel()
        Task<bool> SetClipProtectionLevel(double leveldB)
        
        // Generator Control
        Task<bool> Start()
        Task<bool> Stop()
        Task<bool> IsRunning()
        
        // Commands
        Task<string[]> GetAvailableCommands()
        Task<CommandResult> ExecuteCommand(string command, Dictionary<string, object> parameters = null)
        
        // Channel/File Configuration
        Task<string> GetChannel()
        Task<bool> SetChannel(string channel)
        Task<string> GetWavFile()
        Task<bool> SetWavFile(string filePath)
    }
}
```

---

## 4. Domain Objects

### SignalType Enum

```csharp
public enum SignalType
{
    Sine,
    PinkNoise,
    WhiteNoise,
    Sweep,
    WavFile,
    Chirp,
    MLS,
    Silence
}
```

### SweepType Enum

```csharp
public enum SweepType
{
    Linear,
    Logarithmic
}
```

### GeneratorConfiguration Class

```csharp
/// <summary>
/// Complete generator configuration
/// </summary>
public class GeneratorConfiguration
{
    public SignalType SignalType { get; set; }
    public double FrequencyHz { get; set; }
    public double StartFrequencyHz { get; set; }
    public double EndFrequencyHz { get; set; }
    public double LeveldB { get; set; }
    public double SweepLengthSeconds { get; set; }
    public SweepType SweepType { get; set; }
    public double FadeInSeconds { get; set; }
    public double FadeOutSeconds { get; set; }
    public bool ClipProtectionEnabled { get; set; }
    public double ClipProtectionLeveldB { get; set; }
    public string OutputChannel { get; set; }
    public string WavFilePath { get; set; }
}
```

---

## 5. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ `GeneratorService` class exists in `src/csharp/Domain/Services/`
- ✅ All ~40 REW `/generator/*` endpoints accessible
- ✅ Domain objects (SignalType, SweepType, GeneratorConfiguration) implemented
- ✅ Follows ADR-004 architecture
- ✅ >80% unit test coverage (~40 tests)
- ✅ Integration tests pass against real REW
- ✅ Cross-language parity (C#, C++, JS, TS)

**Current Status**: ❌ **0% Complete** (service does not exist)

---

## 6. References

- **REW REST API**: <http://localhost:4735/>
- **Architecture**: ADR-004
- **Gap Analysis**: `API_COMPLETENESS_GAP_ANALYSIS.md`
- **Implementation**: `src/csharp/Domain/Services/GeneratorService.cs` (TO BE CREATED)
- **Tests**: `tests/csharp/GeneratorServiceTests.cs` (TO BE CREATED)

---

**Document Owner**: Requirements Team  
**Status**: Draft (0% implemented - new service required)
