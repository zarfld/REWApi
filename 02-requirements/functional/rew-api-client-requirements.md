---
title: "REW API Client Library - Functional Requirements"
specType: requirements
standard: "ISO/IEC/IEEE 29148:2018"
phase: 02-requirements  
version: 1.0.0
status: approved
author: code-to-requirements-analysis
date: "2025-10-06"
source: "Reverse engineered from existing C#, C++, JavaScript, and TypeScript implementations"
traceability:
  stakeholderRequirements:
    - StR-001
    - StR-002
    - StR-003
    - StR-004
    - StR-005
  source_implementations:
    - src/csharp/REWApi.cs
    - src/cpp/REWApi.cpp
    - src/js/rewApi.js
    - src/ts/rewApi.ts
  external_api: https://www.roomeqwizard.com/help/help_en-GB/html/api.html
description: "Functional requirements specification for REW API client library extracted from existing code implementations"
---

# REW API Client Library - Functional Requirements

**Document ID**: REQ-FUNC-REW-CLIENT-001  
**Standards**: ISO/IEC/IEEE 29148:2018 (System Requirements)  
**Source**: Code-to-Requirements Analysis  
**Implementation Status**: ✅ Implemented across multiple languages

## 🎯 System Overview

The REW API Client Library provides a standards-compliant software interface for interacting with Room EQ Wizard's REST API. The system enables programmatic control of REW's audio measurement, analysis, and processing capabilities through HTTP-based communication.

**Business Context**: Room EQ Wizard (REW) is professional acoustic measurement software that provides an HTTP API for automation. This client library abstracts the HTTP communication complexity and provides language-specific interfaces for developers.

## 📋 Functional Requirements

### REQ-F-001: HTTP Communication Core

**ID**: REQ-F-001  
**Priority**: Critical (P0)  
**Source**: Derived from HTTP client implementations across all languages  
**Implementation**: REWApi.cs:16-40, REWApi.cpp:12-45, rewApi.js:18-65, rewApi.ts:18-65

#### Description
The system shall provide HTTP communication capabilities to interact with REW's REST API server.

#### Sub-Requirements

##### REQ-F-001.1: GET Operations
**Implementation**: C# GetAsync(), C++ GetAsync(), JS get(), TS get()  
The system shall support HTTP GET operations for retrieving data from REW API endpoints.

**Acceptance Criteria**:
```gherkin
Scenario: Successful GET request
  Given REW API server is running on localhost:4735
  When a GET request is made to a valid endpoint
  Then the response status should be 200 OK
  And the response body should contain valid JSON data

Scenario: GET request with graph update inhibition
  Given graph update inhibition is enabled  
  When a GET request is made
  Then the request should return null without contacting the server
```

##### REQ-F-001.2: PUT Operations  
**Implementation**: C# PutAsync(), C++ PutAsync(), JS put(), TS put()  
The system shall support HTTP PUT operations for updating REW API resources.

**Acceptance Criteria**:
```gherkin
Scenario: Successful PUT request
  Given REW API server is running
  And valid JSON content is provided
  When a PUT request is made to an updatable endpoint
  Then the response status should be 200 OK
  And the resource should be updated with the provided data
```

##### REQ-F-001.3: POST Operations
**Implementation**: C# PostAsync(), C++ PostAsync(), JS post(), TS post()  
The system shall support HTTP POST operations for creating resources and issuing commands.

**Acceptance Criteria**:
```gherkin
Scenario: Successful POST request
  Given REW API server is running
  And valid JSON content is provided  
  When a POST request is made to a command endpoint
  Then the response status should be 200 OK or 202 Accepted
  And the command should be executed by REW

Scenario: POST with blocking mode enabled
  Given blocking mode is enabled
  When a POST request is made to a long-running command
  Then the response should not return until command completion
  And the response should include ProcessResult data
```

##### REQ-F-001.4: Error Handling
**Implementation**: response.EnsureSuccessStatusCode() patterns  
The system shall handle HTTP errors gracefully and provide meaningful error messages.

**Acceptance Criteria**:
```gherkin
Scenario: HTTP error response
  Given REW API server returns an error status
  When an HTTP request is made
  Then an appropriate exception should be thrown
  And the error message should include HTTP status information
```

### REQ-F-002: Base64 Array Encoding/Decoding

**ID**: REQ-F-002  
**Priority**: High (P1)  
**Source**: REW API documentation requirement for array data transfer  
**Implementation**: REWApi.cs:384-396, rewApi.js:encodeArray/decodeArray, rewApi.ts:encodeArray/decodeArray

#### Description
The system shall provide Base64 encoding and decoding capabilities for transferring 32-bit float arrays as required by REW API protocol.

#### Sub-Requirements

##### REQ-F-002.1: Array Encoding
**Implementation**: C# EncodeArray(), JS encodeArray(), TS encodeArray()  
The system shall encode float arrays to Base64 strings for transmission to REW API.

**Technical Specification**:
- Input: 32-bit IEEE 754 float array
- Process: Convert to big-endian byte representation → Base64 encode
- Output: Base64-encoded string according to RFC 4648
- Byte order: **Big-endian** (as specified by REW API documentation)
- Float format: IEEE 754 single precision (32-bit)

**Implementation Details**:
- **C#**: Requires explicit byte order conversion to big-endian before Base64 encoding
- **C++**: Must ensure big-endian byte order using network byte order functions or explicit conversion
- **JavaScript/TypeScript**: Uses `DataView` with explicit big-endian flag for byte order control
- **Cross-platform compatibility**: All implementations must produce identical Base64 output using big-endian byte order

**Verification Requirement**:
All language implementations shall produce byte-identical Base64 strings for identical float input arrays using big-endian byte order to ensure REW API compatibility.

**Acceptance Criteria**:
```gherkin
Scenario: Encode float array to Base64
  Given a float array [0.125f, 0.25f, 0.375f, 0.5f]
  When the array is encoded to Base64
  Then the result should be "PgAAAD6AAAA+wAAAPwAAAA=="
  
Scenario: Encode empty array
  Given an empty float array
  When the array is encoded
  Then the result should be an empty Base64 string
```

##### REQ-F-002.2: Array Decoding  
**Implementation**: C# DecodeArray(), JS decodeArray(), TS decodeArray()  
The system shall decode Base64 strings received from REW API back to float arrays.

**Technical Specification**:
- Input: Base64-encoded string from REW API response
- Process: Base64 decode → Convert from big-endian byte representation to float array
- Output: 32-bit IEEE 754 float array
- Byte order: **Big-endian** (as specified by REW API documentation)
- Float format: IEEE 754 single precision (32-bit)

**Implementation Details**:
- **C#**: Requires explicit big-endian to native byte order conversion after Base64 decoding
- **C++**: Must handle big-endian byte order conversion using network byte order functions or explicit conversion
- **JavaScript/TypeScript**: Uses `DataView` with explicit big-endian flag for byte order control
- **Cross-platform compatibility**: All implementations must correctly interpret big-endian encoded data from REW API

**Verification Requirement**:
All language implementations shall produce identical float arrays from identical Base64 input strings using big-endian byte order interpretation.

**Acceptance Criteria**:
```gherkin
Scenario: Decode Base64 to float array
  Given Base64 string "PgAAAD6AAAA+wAAAPwAAAA=="
  When the string is decoded
  Then the result should be float array [0.125f, 0.25f, 0.375f, 0.5f]

Scenario: Decode invalid Base64
  Given an invalid Base64 string
  When decoding is attempted
  Then an appropriate exception should be thrown
```

**Architecture Traceability**:
- **Architecture Components**: ARC-C-001 - REW Client Service Component
- **Design Elements**: Base64 encoding/decoding utility classes
- **Implementation**: Cross-platform data serialization layer

### REQ-F-003: Audio System Control

**ID**: REQ-F-003  
**Priority**: High (P1)  
**Source**: Audio settings methods across implementations  
**Implementation**: GetAudioSettingsAsync/UpdateAudioSettingsAsync patterns

#### Description
The system shall provide interfaces for configuring REW's audio input/output settings.

#### Sub-Requirements

##### REQ-F-003.1: Audio Settings Retrieval
The system shall retrieve current audio configuration from REW.

**API Endpoints**: `/audio/settings`, `/audio/driver`, `/audio/samplerate`

**Acceptance Criteria**:
```gherkin
Scenario: Get audio settings
  Given REW is running with audio enabled
  When audio settings are requested
  Then current driver, sample rate, and device information should be returned
```

##### REQ-F-003.2: Audio Settings Update
The system shall update REW's audio configuration settings.

**Acceptance Criteria**:
```gherkin
Scenario: Update audio driver
  Given REW supports multiple audio drivers
  When a driver change is requested
  Then REW should switch to the specified driver
  And available devices should be updated accordingly
```

**Architecture Traceability**:
- **Architecture Components**: ARC-C-001 - REW Client Service Component
- **Design Elements**: Audio system configuration interfaces
- **Implementation**: Audio driver management and device enumeration

### REQ-F-004: Measurement Operations

**ID**: REQ-F-004  
**Priority**: Critical (P0)  
**Source**: Extensive measurement-related methods in all implementations  
**Implementation**: REWApi.cs:GetMeasurementsAsync, measurement command patterns

#### Description
The system shall provide comprehensive measurement management capabilities including retrieval, creation, modification, and processing of acoustic measurements.

#### Sub-Requirements

##### REQ-F-004.1: Measurement Retrieval
**Implementation**: GetMeasurementsAsync(), GetMeasurement(id) patterns  
The system shall retrieve measurement data and metadata from REW.

**API Endpoints**: `/measurements`, `/measurements/{id}`

**Acceptance Criteria**:
```gherkin
Scenario: Get all measurements
  Given REW has loaded measurements
  When all measurements are requested
  Then a list of measurement summaries should be returned
  And each summary should include ID, name, and basic metadata

Scenario: Get specific measurement
  Given a measurement exists with ID "123"
  When measurement "123" is requested
  Then detailed measurement data should be returned
  Including frequency response, impulse response, and settings
```

##### REQ-F-004.2: Measurement Commands
**Implementation**: IssueMeasurementCommandWithParamsAsync() with extensive validation  
The system shall execute measurement processing commands with parameter validation.

**Supported Commands** (from code analysis):
- Load
- Save all  
- Save
- Generate minimum phase version
- Generate excess phase version
- Generate Mic in box correction
- Make response copy
- Merge cal data to IR
- Trim IR to windows
- Smooth
- Generate waterfalls
- Generate spectrograms
- Estimate IR delay
- Offset t=0
- Add SPL offset
- Invert
- Generate RT60 data

**Parameter Validation Requirements**:
- Boolean parameters: "include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data"
- Numeric constraints: lf tail slope ≥ 0, hf tail slope ≤ 0
- Required parameter dependencies based on boolean flags

**Acceptance Criteria**:
```gherkin
Scenario: Execute smooth command
  Given a measurement exists
  And valid smoothing parameter is provided ("1/3", "1/6", etc.)
  When smooth command is executed
  Then the measurement should be smoothed according to the parameter
  And the operation should complete successfully

Scenario: Execute command with invalid parameters
  Given a measurement command requires specific parameters
  When the command is executed with missing or invalid parameters
  Then an error message should be returned
  And the command should not be executed

Scenario: Generate minimum phase version
  Given a measurement exists
  And parameters include cal=true, append_lf_tail=true, lf_tail_start=27, lf_tail_slope=12.0
  When "Generate minimum phase version" command is executed
  Then a new measurement should be created with minimum phase characteristics
```

##### REQ-F-004.3: Frequency Response Data
**Implementation**: GetFrequencyResponse() methods with unit/smoothing options  
The system shall retrieve frequency response data with configurable units and smoothing.

**API Endpoints**: `/measurements/{id}/frequency-response`

**Supported Units**: SPL, dBFS, and others as per REW API  
**Supported Smoothing**: 1/12, 1/6, 1/3, etc.

**Acceptance Criteria**:
```gherkin
Scenario: Get frequency response with specific units
  Given a measurement exists
  When frequency response is requested with unit="dBFS"
  Then response data should be in dBFS units
  And magnitude and phase arrays should be Base64 encoded

Scenario: Get frequency response with smoothing
  Given a measurement exists  
  When frequency response is requested with smoothing="1/3"
  Then the response should be smoothed to 1/3 octave resolution
```

##### REQ-F-004.4: Impulse Response Data
**Implementation**: GetImpulseResponse() methods  
The system shall retrieve impulse response data with configurable options.

**Options**: unit, windowed, normalised

**Acceptance Criteria**:
```gherkin
Scenario: Get windowed impulse response
  Given a measurement has impulse response data
  When impulse response is requested with windowed=true
  Then only the windowed portion should be returned

Scenario: Get non-normalized impulse response  
  Given a measurement exists
  When impulse response is requested with normalised=false
  Then raw impulse response data should be returned without normalization
```

### REQ-F-005: Filter and EQ Management

**ID**: REQ-F-FILTER-001  
**Priority**: High (P1)  
**Source**: EQ/Filter methods and FilterService implementations  
**Implementation**: FilterService.cs, GetEQSettingsAsync/UpdateEQSettingsAsync patterns

#### Description
The system shall provide equalization and filtering capabilities for measurement processing.

#### Sub-Requirements

##### REQ-F-FILTER-001.1: Filter Configuration
**Implementation**: FilterService.GetMeasurementFiltersAsync(), UpdateMeasurementFilterAsync()  
The system shall manage filter settings for measurements.

**API Endpoints**: `/measurements/{id}/filters`, `/measurements/{id}/filters/{filterId}`

**Acceptance Criteria**:
```gherkin
Scenario: Get measurement filters
  Given a measurement has configured filters
  When filters are requested
  Then current filter settings should be returned
  Including filter type, frequency, Q, and gain parameters

Scenario: Update filter settings
  Given a measurement exists
  When filter parameters are updated
  Then the filter should be reconfigured
  And the change should be reflected in measurement processing
```

##### REQ-F-FILTER-001.2: EQ Settings Management
**Implementation**: GetEQSettingsAsync(), UpdateEQSettingsAsync()  
The system shall manage equalizer configurations.

**Acceptance Criteria**:
```gherkin
Scenario: Update EQ settings
  Given REW supports multiple equalizer types
  When EQ settings are updated
  Then the measurement should use the new equalizer configuration
  And predicted response should be recalculated
```

### REQ-F-SPL-001: SPL Meter Operations

**ID**: REQ-F-SPL-001  
**Priority**: Medium (P2)  
**Source**: SPL meter methods across implementations  
**Implementation**: GetSPLMeterAsync(), PostSPLMeterCommandAsync() patterns

#### Description
The system shall provide Sound Pressure Level measurement and monitoring capabilities.

#### Sub-Requirements

##### REQ-F-SPL-001.1: SPL Meter Control
**Implementation**: PostSPLMeterCommandAsync(), SPL meter configuration methods  
The system shall control SPL meter operation and configuration.

**API Endpoints**: `/spl-meter/{id}/command`, `/spl-meter/{id}/configuration`

**Supported Commands**: start, stop, reset (derived from code patterns)  
**Configuration Options**: weighting (A, C), filter (Fast, Slow), display mode

**Acceptance Criteria**:
```gherkin
Scenario: Start SPL monitoring
  Given SPL meter is configured
  When start command is issued
  Then SPL measurements should begin
  And level data should be available via subscription

Scenario: Configure SPL meter weighting
  Given SPL meter exists
  When weighting is set to "C"
  Then subsequent measurements should use C-weighting
```

##### REQ-F-SPL-001.2: SPL Data Retrieval
**Implementation**: GetSPLMeterLevelsAsync()  
The system shall retrieve current SPL measurements.

**Acceptance Criteria**:
```gherkin
Scenario: Get current SPL levels
  Given SPL meter is running
  When levels are requested
  Then current SPL, Leq, and SEL values should be returned
  With timestamp and measurement duration
```

### REQ-F-RTA-001: Real-Time Analyzer Operations

**ID**: REQ-F-RTA-001  
**Priority**: Medium (P2)  
**Source**: RTA methods in implementations  
**Implementation**: GetRTAAsync(), PostRTACommandAsync(), RTA configuration methods

#### Description
The system shall provide real-time frequency analysis capabilities.

#### Sub-Requirements

##### REQ-F-RTA-001.1: RTA Control
**Implementation**: PostRTACommandAsync(), RTA configuration methods  
The system shall control RTA operation and settings.

**API Endpoints**: `/rta/command`, `/rta/configuration`

**Acceptance Criteria**:
```gherkin
Scenario: Start RTA analysis
  Given RTA is configured
  When start command is issued
  Then real-time frequency analysis should begin
  And spectrum data should be available

Scenario: Configure RTA settings
  Given RTA supports multiple analysis modes
  When configuration is updated
  Then analysis should use new settings
  Including FFT size, windowing, and averaging
```

### REQ-F-CTRL-001: Application Control

**ID**: REQ-F-CTRL-001  
**Priority**: Medium (P2)  
**Source**: Application control methods  
**Implementation**: ShutdownAsync(), graph inhibition, blocking mode

#### Description
The system shall provide application-level control of REW operation.

#### Sub-Requirements

##### REQ-F-CTRL-001.1: Graph Update Inhibition
**Implementation**: SetGraphUpdateInhibition(), graphUpdateInhibited checks  
The system shall control REW's graph update behavior to prevent display updates during batch operations.

**Acceptance Criteria**:
```gherkin
Scenario: Enable graph update inhibition
  Given graph updates are enabled
  When graph update inhibition is enabled
  Then subsequent API calls should not trigger graph updates
  And API operations should return null responses

Scenario: Disable graph update inhibition
  Given graph update inhibition is enabled
  When inhibition is disabled
  Then normal API operations should resume
  And graph updates should occur normally
```

##### REQ-F-CTRL-001.2: Blocking Mode Control
**Implementation**: blockingModeEnabled flag in POST operations  
The system shall support blocking mode for synchronous operation with long-running commands.

**Acceptance Criteria**:
```gherkin
Scenario: Execute command in blocking mode
  Given blocking mode is enabled
  When a long-running command is issued
  Then the API call should not return until completion
  And the response should include the complete result

Scenario: Execute command in non-blocking mode
  Given blocking mode is disabled
  When a long-running command is issued
  Then the API call should return immediately with 202 Accepted
  And result should be available via polling or callback
```

##### REQ-F-CTRL-001.3: Application Shutdown
**Implementation**: ShutdownAsync()  
The system shall provide graceful REW application shutdown capability.

**API Endpoints**: `/application/command` with shutdown command

**Acceptance Criteria**:
```gherkin
Scenario: Shutdown REW application
  Given REW is running without GUI
  When shutdown command is issued
  Then REW should close gracefully
  And all resources should be properly released
```

### REQ-F-SUB-001: Subscription Management

**ID**: REQ-F-SUB-001  
**Priority**: Medium (P2)  
**Source**: Subscription methods and notification patterns  
**Implementation**: SubscribeAsync(), UnsubscribeAsync(), _notifySubscribers()

#### Description
The system shall provide event subscription capabilities for receiving notifications of REW state changes.

#### Sub-Requirements

##### REQ-F-SUB-001.1: Subscription Registration
**Implementation**: SubscribeAsync() with URL-based subscriptions  
The system shall register client URLs to receive notifications.

**API Endpoints**: `/subscribe`, `/measurements/subscribe`, `/spl-meter/{id}/subscribe`

**Acceptance Criteria**:
```gherkin
Scenario: Subscribe to measurement changes
  Given a client URL is available to receive notifications
  When subscription is registered for measurement changes
  Then the client should receive notifications when measurements are added, modified, or deleted

Scenario: Subscribe to SPL meter updates
  Given SPL meter is configured
  When subscription is registered for SPL updates
  Then the client should receive level updates at configured intervals
```

##### REQ-F-SUB-001.2: Subscription Removal
**Implementation**: UnsubscribeAsync()  
The system shall remove subscriptions when no longer needed.

**Acceptance Criteria**:
```gherkin
Scenario: Unsubscribe from notifications
  Given an active subscription exists
  When unsubscribe is requested with the same URL
  Then notifications should stop being sent to that URL
```

## 🔗 Traceability Matrix

| Requirement ID | REW API Endpoint | C# Implementation | C++ Implementation | JS Implementation | TS Implementation |
|---|---|---|---|---|---|
| REQ-F-001.1 | Various GET endpoints | GetAsync() | GetAsync() | get() | get() |
| REQ-F-001.3 | Various POST endpoints | PostAsync() | PostAsync() | post() | post() |
| REQ-F-002.1 | Array transfer | EncodeArray() | EncodeArray() | encodeArray() | encodeArray() |
| REQ-F-004.1 | /measurements | GetMeasurementsAsync() | GetMeasurementsAsync() | getMeasurements() | getMeasurements() |
| REQ-F-004.2 | /measurements/{id}/command | IssueMeasurementCommandWithParamsAsync() | IssueMeasurementCommandAsync() | issueMeasurementCommand() | issueMeasurementCommand() |
| REQ-F-005.1 | /measurements/{id}/filters | FilterService methods | FilterService methods | EQ methods | EQ methods |

## 🧪 Test Requirements

### Unit Test Coverage Requirements
- **HTTP Operations**: 100% coverage of GET, PUT, POST methods
- **Parameter Validation**: 100% coverage of all validation logic
- **Data Encoding**: 100% coverage of Base64 encode/decode operations
- **Error Handling**: 100% coverage of error scenarios

### Integration Test Requirements
- **REW API Compatibility**: Tests against actual REW instance
- **Cross-Language Consistency**: Verify identical behavior across C#, C++, JS, TS implementations
- **Subscription Functionality**: End-to-end subscription tests
- **Long-Running Operations**: Blocking vs non-blocking mode tests

### Performance Requirements
- **Response Time**: API calls should complete within 5 seconds under normal conditions
- **Throughput**: Support concurrent operations (limited by REW server capabilities)
- **Memory Usage**: Efficient handling of large measurement datasets

## 📊 Implementation Status

| Language | Core HTTP | Data Encoding | Measurements | Filters | SPL Meter | RTA | Subscriptions |
|---|---|---|---|---|---|---|---|
| **C#** | ✅ Complete | ✅ Complete | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Basic | ✅ Complete |
| **C++** | ✅ Complete | ✅ Complete | ✅ Complete | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Basic |
| **JavaScript** | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Basic | ✅ Complete | ✅ Complete | ✅ Complete |
| **TypeScript** | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Basic | ✅ Complete | ✅ Complete | ✅ Complete |

**Legend**: ✅ Complete, ⚠️ Partial, ❌ Missing

## 🎯 Success Criteria

The REW API Client Library implementation shall be considered successful when:

1. ✅ **Standards Compliance**: All requirements are traceable to ISO/IEC/IEEE 29148:2018
2. ✅ **Cross-Language Consistency**: Identical functionality across C#, C++, JavaScript, TypeScript
3. ✅ **REW API Coverage**: Support for all major REW API endpoint categories
4. ✅ **Parameter Validation**: Comprehensive validation matching REW API specifications
5. ✅ **Error Handling**: Graceful handling of all HTTP and validation errors
6. ✅ **Documentation**: Complete traceability from requirements to implementation
7. ⚠️ **Test Coverage**: >80% unit test coverage (target for improvement)
8. ✅ **Real-World Usage**: Successfully integrate with actual REW instances

---

**Document Status**: ✅ **Requirements Successfully Extracted from Code**  
**Next Phase**: 03-Architecture - Document architecture decisions and design patterns  
**Validation Required**: Cross-reference with official REW API documentation for completeness