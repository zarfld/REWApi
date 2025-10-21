---
title: "REW Client Library Requirements - Application API"
specType: requirements
phase: 02-requirements
version: 1.0.0
status: draft
author: requirements-analysis
date: 2025-10-21
traceability:
  stakeholderRequirements:
    - STR-001 (Audio measurement automation)
    - STR-002 (Test automation support)
  architecture:
    - ADR-004 (Domain Service Architecture)
  verification:
    - TEST-APPLICATION-XXX
description: "Functional requirements for ApplicationService exposing REW REST API /application/* endpoints through OOP interface"
---

# REW Client Library - Application API Requirements

**Document ID**: REQ-F-CLIENT-001  
**REW REST API Resource**: `/application/*`  
**Client Library Component**: `ApplicationService.cs` (Domain Service)  
**Standards**: ISO/IEC/IEEE 29148:2018 (System Requirements)  
**REW Swagger**: <http://localhost:4735/>

---

## 1. Purpose

Define functional requirements for the **ApplicationService** domain service class that provides object-oriented access to REW REST API `/application/*` endpoints.

**What We're Building**:
- **Client Library Component**: `ApplicationService` class
- **Consumes**: REW REST API `/application/*` endpoints (14 endpoints)
- **Provides**: Clean OOP interface following ADR-004 Domain Service Architecture
- **Languages**: C#, C++, JavaScript, TypeScript

---

## 2. REW REST API Endpoints (Source of Truth)

**REW Swagger Documentation**: <http://localhost:4735/>

### Endpoints Covered by This Requirement

| HTTP Method | REW Endpoint | Purpose | Response | Status |
|---|---|---|---|---|
| GET | `/application/blocking` | Get blocking status | boolean | ✅ Implemented |
| POST | `/application/blocking` | Enable/disable blocking | 200 OK | ✅ Implemented |
| POST | `/application/command` | Execute application command | 200/202 | ⚠️ Partial (legacy) |
| GET | `/application/commands` | List available commands | JSON array | ❌ Missing |
| GET | `/application/errors` | Get error history | JSON array | ⚠️ Partial |
| POST | `/application/errors/subscribe` | Subscribe to error notifications | 200 OK | ⚠️ Not REW-compliant |
| GET | `/application/errors/subscribers` | List error subscribers | JSON array | ❌ Missing |
| POST | `/application/errors/unsubscribe` | Unsubscribe from errors | 200 OK | ❌ Missing |
| GET | `/application/inhibit-graph-updates` | Get graph update inhibit status | boolean | ❌ Missing |
| POST | `/application/inhibit-graph-updates` | Set graph update inhibit | 200 OK | ❌ Missing |
| GET | `/application/last-error` | Get most recent error | JSON | ❌ Missing |
| GET | `/application/last-warning` | Get most recent warning | JSON | ❌ Missing |
| GET | `/application/logging` | Get logging configuration | JSON | ❌ Missing |
| POST | `/application/logging` | Configure logging | 200 OK | ❌ Missing |
| GET | `/application/warnings` | Get warning history | JSON array | ❌ Missing |
| POST | `/application/warnings/subscribe` | Subscribe to warning notifications | 200 OK | ❌ Missing |
| GET | `/application/warnings/subscribers` | List warning subscribers | JSON array | ❌ Missing |
| POST | `/application/warnings/unsubscribe` | Unsubscribe from warnings | 200 OK | ❌ Missing |

**Total Endpoints**: 18 endpoints in `/application/*` group  
**Currently Implemented**: ~3 endpoints (17% coverage)  
**Target**: 100% coverage

---

## 3. Functional Requirements

### REQ-F-CLIENT-001.1: ApplicationService Domain Service

**ID**: REQ-F-CLIENT-001.1  
**Priority**: Critical  
**Type**: Functional

**Requirement**: The client library SHALL provide an `ApplicationService` domain service class that exposes all REW REST API `/application/*` functionality through an object-oriented interface.

**Acceptance Criteria**:
- ✅ `ApplicationService` class exists in `src/csharp/Domain/Services/`
- ⚠️ Currently 3/18 REW endpoints accessible (need 15 more methods)
- ✅ Follows ADR-004 layered architecture (Domain → Gateway → Transport)
- ✅ No HTTP/JSON exposure in public API
- ⚠️ Cross-language consistency (C# exists, JS/TS/C++ pending)

**Current Status**: Partially implemented (17%)

**Rationale**: Users need OOP access to application control functionality without dealing with REST details.

**Traces To**:
- **Architecture**: ADR-004 (Domain Service Architecture)
- **REW API**: `/application/*` endpoints
- **Tests**: `TEST-APPLICATION-001` through `TEST-APPLICATION-018`

---

### REQ-F-CLIENT-001.2: Blocking Control

**ID**: REQ-F-CLIENT-001.2  
**Priority**: High  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to control REW's blocking/non-blocking execution mode.

#### REQ-F-CLIENT-001.2.1: GetBlockingStatus() - Check Blocking Mode

**Status**: ✅ **IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets the current blocking status of REW
/// Maps to: GET /application/blocking
/// </summary>
/// <returns>True if blocking mode is enabled</returns>
public async Task<bool> GetBlockingStatus()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/blocking`
- **Response**: JSON `{"blocking": true}`
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ✅ Method exists in `ApplicationService` class
- ✅ Returns boolean blocking status
- ✅ Unit test coverage

**Traces To**:
- **REW API**: `GET /application/blocking`
- **Tests**: `TEST-APPLICATION-BLOCKING-001`
- **Implementation**: `src/csharp/Domain/Services/ApplicationService.cs`

---

#### REQ-F-CLIENT-001.2.2: SetBlocking() - Enable/Disable Blocking Mode

**Status**: ✅ **IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Sets the blocking status of REW
/// Maps to: POST /application/blocking
/// </summary>
/// <param name="enabled">True to enable blocking mode</param>
/// <returns>True if successfully set</returns>
public async Task<bool> SetBlocking(bool enabled)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/blocking`
- **Request Body**: JSON `{"blocking": true}`
- **Response**: 200 OK
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ✅ Method exists in `ApplicationService` class
- ✅ Accepts boolean parameter
- ✅ Unit test coverage

**Traces To**:
- **REW API**: `POST /application/blocking`
- **Tests**: `TEST-APPLICATION-BLOCKING-002`

---

### REQ-F-CLIENT-001.3: Command Execution

**ID**: REQ-F-CLIENT-001.3  
**Priority**: High  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to execute application-level commands.

#### REQ-F-CLIENT-001.3.1: ExecuteCommand() - Execute Application Command

**Status**: ⚠️ **PARTIALLY IMPLEMENTED** (via legacy API)

**Method Signature** (C#):
```csharp
/// <summary>
/// Executes an application-level command
/// Maps to: POST /application/command
/// </summary>
/// <param name="command">Command name</param>
/// <param name="parameters">Optional command parameters</param>
/// <returns>Command result</returns>
public async Task<CommandResult> ExecuteCommand(string command, Dictionary<string, object> parameters = null)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/command`
- **Request Body**: JSON `{"command": "name", "parameters": {...}}`
- **Response**: 200 OK (immediate) or 202 Accepted (long-running)
- **Status Codes**: 200 (success), 202 (accepted), 400 (invalid command)

**JSON Schema** (from REW Swagger):
```json
{
  "request": {
    "command": "string",
    "parameters": {
      "param1": "value1",
      "param2": "value2"
    }
  },
  "response": {
    "result": "success",
    "data": {}
  }
}
```

**Acceptance Criteria**:
- ⚠️ Currently uses legacy API pattern
- ❌ Should use CommandGate for 200/202 handling
- ❌ Needs proper domain objects (CommandResult)
- ❌ Unit tests incomplete

**Traces To**:
- **REW API**: `POST /application/command`
- **Tests**: `TEST-APPLICATION-COMMAND-001`
- **Architecture**: `CommandGate`, `LongRunningJob<T>`

---

#### REQ-F-CLIENT-001.3.2: GetAvailableCommands() - List Available Commands

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets list of available application commands
/// Maps to: GET /application/commands
/// </summary>
/// <returns>Array of available command names</returns>
public async Task<string[]> GetAvailableCommands()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/commands`
- **Response**: JSON array `["command1", "command2", ...]`
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests
- ❌ Not accessible via OOP interface

**Traces To**:
- **REW API**: `GET /application/commands`
- **Tests**: `TEST-APPLICATION-COMMANDS-001` (TO BE CREATED)

---

### REQ-F-CLIENT-001.4: Error Monitoring

**ID**: REQ-F-CLIENT-001.4  
**Priority**: High  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to monitor and subscribe to REW errors.

#### REQ-F-CLIENT-001.4.1: GetErrors() - Get Error History

**Status**: ⚠️ **PARTIALLY IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets error history from REW
/// Maps to: GET /application/errors
/// </summary>
/// <param name="limit">Maximum number of errors to retrieve</param>
/// <returns>Array of error messages</returns>
public async Task<ErrorMessage[]> GetErrors(int limit = 100)
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/errors?limit={limit}`
- **Response**: JSON array of error objects
- **Status Codes**: 200 (success)

**JSON Schema** (from REW Swagger):
```json
[
  {
    "timestamp": "2025-10-21T10:30:00Z",
    "level": "ERROR",
    "message": "Error description",
    "source": "ComponentName"
  }
]
```

**Acceptance Criteria**:
- ⚠️ Currently partially implemented
- ❌ Needs ErrorMessage domain object
- ❌ Unit test coverage incomplete

**Traces To**:
- **REW API**: `GET /application/errors`
- **Tests**: `TEST-APPLICATION-ERRORS-001`

---

#### REQ-F-CLIENT-001.4.2: SubscribeToErrors() - Subscribe to Error Notifications

**Status**: ⚠️ **IMPLEMENTED BUT NOT REW-COMPLIANT**

**Method Signature** (C#):
```csharp
/// <summary>
/// Subscribes to real-time error notifications from REW
/// Maps to: POST /application/errors/subscribe
/// </summary>
/// <param name="handler">Callback function to receive errors</param>
/// <returns>Subscription ID for unsubscribing</returns>
public async Task<string> SubscribeToErrors(Action<ErrorMessage> handler)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/errors/subscribe`
- **Request Body**: JSON `{"callbackUrl": "http://client:port/callback"}`
- **Response**: JSON `{"subscriptionId": "uuid"}`
- **Status Codes**: 200 (success)

**Current Issues**:
- ⚠️ Does NOT use REW's callback URL pattern
- ❌ Should use SubscriptionManager infrastructure
- ❌ Needs embedded HTTP listener for REW callbacks

**Acceptance Criteria**:
- ❌ Must use REW subscription model (callback URLs)
- ❌ Requires SubscriptionManager class
- ❌ Unit and integration tests

**Traces To**:
- **REW API**: `POST /application/errors/subscribe`
- **Tests**: `TEST-APPLICATION-ERROR-SUBSCRIPTION-001`
- **Infrastructure**: TODO #20 (SubscriptionManager)

---

#### REQ-F-CLIENT-001.4.3: GetErrorSubscribers() - List Error Subscribers

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets list of current error subscribers
/// Maps to: GET /application/errors/subscribers
/// </summary>
/// <returns>Array of subscription information</returns>
public async Task<SubscriptionInfo[]> GetErrorSubscribers()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/errors/subscribers`
- **Response**: JSON array `[{"subscriptionId": "uuid", "callbackUrl": "..."}]`
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ Needs SubscriptionInfo domain object
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/errors/subscribers`
- **Tests**: `TEST-APPLICATION-ERROR-SUBSCRIBERS-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.4.4: UnsubscribeFromErrors() - Unsubscribe from Errors

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Unsubscribes from error notifications
/// Maps to: POST /application/errors/unsubscribe
/// </summary>
/// <param name="subscriptionId">Subscription ID to cancel</param>
/// <returns>True if successfully unsubscribed</returns>
public async Task<bool> UnsubscribeFromErrors(string subscriptionId)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/errors/unsubscribe`
- **Request Body**: JSON `{"subscriptionId": "uuid"}`
- **Response**: 200 OK
- **Status Codes**: 200 (success), 404 (subscription not found)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `POST /application/errors/unsubscribe`
- **Tests**: `TEST-APPLICATION-ERROR-UNSUBSCRIBE-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.4.5: GetLastError() - Get Most Recent Error

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets the most recent error from REW
/// Maps to: GET /application/last-error
/// </summary>
/// <returns>Last error message or null if none</returns>
public async Task<ErrorMessage?> GetLastError()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/last-error`
- **Response**: JSON error object or null
- **Status Codes**: 200 (success), 204 (no error)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ Needs ErrorMessage domain object
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/last-error`
- **Tests**: `TEST-APPLICATION-LAST-ERROR-001` (TO BE CREATED)

---

### REQ-F-CLIENT-001.5: Warning Monitoring

**ID**: REQ-F-CLIENT-001.5  
**Priority**: Medium  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to monitor and subscribe to REW warnings.

#### REQ-F-CLIENT-001.5.1: GetWarnings() - Get Warning History

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets warning history from REW
/// Maps to: GET /application/warnings
/// </summary>
/// <param name="limit">Maximum number of warnings to retrieve</param>
/// <returns>Array of warning messages</returns>
public async Task<WarningMessage[]> GetWarnings(int limit = 100)
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/warnings?limit={limit}`
- **Response**: JSON array of warning objects
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ Needs WarningMessage domain object
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/warnings`
- **Tests**: `TEST-APPLICATION-WARNINGS-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.5.2: SubscribeToWarnings() - Subscribe to Warning Notifications

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Subscribes to real-time warning notifications from REW
/// Maps to: POST /application/warnings/subscribe
/// </summary>
/// <param name="handler">Callback function to receive warnings</param>
/// <returns>Subscription ID for unsubscribing</returns>
public async Task<string> SubscribeToWarnings(Action<WarningMessage> handler)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/warnings/subscribe`
- **Request Body**: JSON `{"callbackUrl": "http://client:port/callback"}`
- **Response**: JSON `{"subscriptionId": "uuid"}`
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ Must use SubscriptionManager infrastructure
- ❌ Needs embedded HTTP listener for REW callbacks
- ❌ No unit tests

**Traces To**:
- **REW API**: `POST /application/warnings/subscribe`
- **Tests**: `TEST-APPLICATION-WARNING-SUBSCRIPTION-001` (TO BE CREATED)
- **Infrastructure**: TODO #20 (SubscriptionManager)

---

#### REQ-F-CLIENT-001.5.3: GetWarningSubscribers() - List Warning Subscribers

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets list of current warning subscribers
/// Maps to: GET /application/warnings/subscribers
/// </summary>
/// <returns>Array of subscription information</returns>
public async Task<SubscriptionInfo[]> GetWarningSubscribers()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/warnings/subscribers`
- **Response**: JSON array
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/warnings/subscribers`
- **Tests**: `TEST-APPLICATION-WARNING-SUBSCRIBERS-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.5.4: UnsubscribeFromWarnings() - Unsubscribe from Warnings

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Unsubscribes from warning notifications
/// Maps to: POST /application/warnings/unsubscribe
/// </summary>
/// <param name="subscriptionId">Subscription ID to cancel</param>
/// <returns>True if successfully unsubscribed</returns>
public async Task<bool> UnsubscribeFromWarnings(string subscriptionId)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/warnings/unsubscribe`
- **Request Body**: JSON `{"subscriptionId": "uuid"}`
- **Response**: 200 OK
- **Status Codes**: 200 (success), 404 (not found)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `POST /application/warnings/unsubscribe`
- **Tests**: `TEST-APPLICATION-WARNING-UNSUBSCRIBE-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.5.5: GetLastWarning() - Get Most Recent Warning

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets the most recent warning from REW
/// Maps to: GET /application/last-warning
/// </summary>
/// <returns>Last warning message or null if none</returns>
public async Task<WarningMessage?> GetLastWarning()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/last-warning`
- **Response**: JSON warning object or null
- **Status Codes**: 200 (success), 204 (no warning)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/last-warning`
- **Tests**: `TEST-APPLICATION-LAST-WARNING-001` (TO BE CREATED)

---

### REQ-F-CLIENT-001.6: Graph Update Control

**ID**: REQ-F-CLIENT-001.6  
**Priority**: Medium  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to control REW's graph update behavior.

#### REQ-F-CLIENT-001.6.1: GetGraphUpdateInhibit() - Get Inhibit Status

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets whether graph updates are currently inhibited
/// Maps to: GET /application/inhibit-graph-updates
/// </summary>
/// <returns>True if graph updates are inhibited</returns>
public async Task<bool> GetGraphUpdateInhibit()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/inhibit-graph-updates`
- **Response**: JSON `{"inhibit": true}`
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/inhibit-graph-updates`
- **Tests**: `TEST-APPLICATION-GRAPH-INHIBIT-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.6.2: SetGraphUpdateInhibit() - Set Inhibit Status

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Sets whether to inhibit graph updates
/// Maps to: POST /application/inhibit-graph-updates
/// </summary>
/// <param name="inhibit">True to inhibit updates</param>
/// <returns>True if successfully set</returns>
public async Task<bool> SetGraphUpdateInhibit(bool inhibit)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/inhibit-graph-updates`
- **Request Body**: JSON `{"inhibit": true}`
- **Response**: 200 OK
- **Status Codes**: 200 (success)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `POST /application/inhibit-graph-updates`
- **Tests**: `TEST-APPLICATION-GRAPH-INHIBIT-002` (TO BE CREATED)

---

### REQ-F-CLIENT-001.7: Logging Configuration

**ID**: REQ-F-CLIENT-001.7  
**Priority**: Low  
**Type**: Functional

**Requirement**: The `ApplicationService` SHALL provide methods to configure REW logging.

#### REQ-F-CLIENT-001.7.1: GetLoggingConfiguration() - Get Logging Config

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Gets current logging configuration
/// Maps to: GET /application/logging
/// </summary>
/// <returns>Logging configuration settings</returns>
public async Task<LoggingConfiguration> GetLoggingConfiguration()
```

**REW REST API Mapping**:
- **Endpoint**: `GET /application/logging`
- **Response**: JSON logging config object
- **Status Codes**: 200 (success)

**JSON Schema** (from REW Swagger):
```json
{
  "level": "INFO",
  "enableFileLogging": true,
  "logFilePath": "/path/to/log.txt",
  "maxFileSize": 10485760
}
```

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ Needs LoggingConfiguration domain object
- ❌ No unit tests

**Traces To**:
- **REW API**: `GET /application/logging`
- **Tests**: `TEST-APPLICATION-LOGGING-001` (TO BE CREATED)

---

#### REQ-F-CLIENT-001.7.2: SetLoggingConfiguration() - Configure Logging

**Status**: ❌ **NOT IMPLEMENTED**

**Method Signature** (C#):
```csharp
/// <summary>
/// Sets logging configuration
/// Maps to: POST /application/logging
/// </summary>
/// <param name="configuration">Logging configuration to apply</param>
/// <returns>True if successfully configured</returns>
public async Task<bool> SetLoggingConfiguration(LoggingConfiguration configuration)
```

**REW REST API Mapping**:
- **Endpoint**: `POST /application/logging`
- **Request Body**: JSON logging config object
- **Response**: 200 OK
- **Status Codes**: 200 (success), 400 (invalid config)

**Acceptance Criteria**:
- ❌ Method does not exist yet
- ❌ No unit tests

**Traces To**:
- **REW API**: `POST /application/logging`
- **Tests**: `TEST-APPLICATION-LOGGING-002` (TO BE CREATED)

---

## 4. Domain Object Requirements

### REQ-F-CLIENT-001.8: Domain Objects

**ID**: REQ-F-CLIENT-001.8  
**Priority**: High  
**Type**: Functional

**Requirement**: The client library SHALL provide domain objects that represent REW application concepts.

#### ErrorMessage Class

```csharp
/// <summary>
/// Represents an error message from REW
/// </summary>
public class ErrorMessage
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }  // "ERROR", "SEVERE", etc.
    public string Message { get; set; }
    public string Source { get; set; }
}
```

#### WarningMessage Class

```csharp
/// <summary>
/// Represents a warning message from REW
/// </summary>
public class WarningMessage
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }  // "WARNING", "INFO", etc.
    public string Message { get; set; }
    public string Source { get; set; }
}
```

#### SubscriptionInfo Class

```csharp
/// <summary>
/// Represents subscription information
/// </summary>
public class SubscriptionInfo
{
    public string SubscriptionId { get; set; }
    public string CallbackUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### LoggingConfiguration Class

```csharp
/// <summary>
/// Represents REW logging configuration
/// </summary>
public class LoggingConfiguration
{
    public string Level { get; set; }  // "DEBUG", "INFO", "WARNING", "ERROR"
    public bool EnableFileLogging { get; set; }
    public string LogFilePath { get; set; }
    public long MaxFileSize { get; set; }
}
```

#### CommandResult Class

```csharp
/// <summary>
/// Represents the result of a command execution
/// </summary>
public class CommandResult
{
    public string Result { get; set; }  // "success", "error", etc.
    public Dictionary<string, object> Data { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 5. Summary: Application API Coverage

| Category | Total Endpoints | Implemented | Status |
|---|---|---|---|
| **Blocking Control** | 2 | 2 | ✅ 100% |
| **Command Execution** | 2 | 1 | ⚠️ 50% |
| **Error Monitoring** | 5 | 1 | ❌ 20% |
| **Warning Monitoring** | 5 | 0 | ❌ 0% |
| **Graph Control** | 2 | 0 | ❌ 0% |
| **Logging** | 2 | 0 | ❌ 0% |
| **TOTAL** | 18 | 3 | ⚠️ **17%** |

**Critical Gaps**:
1. ❌ 15 endpoints not exposed via OOP interface
2. ⚠️ Subscription model not REW-compliant (needs SubscriptionManager)
3. ❌ No warning monitoring functionality
4. ❌ No graph update control
5. ❌ No logging configuration

---

## 6. Test Requirements

### REQ-F-CLIENT-001.9: Test Coverage

**Requirement**: SHALL have comprehensive automated tests for all functionality.

**Current Status**:
- ⚠️ Partial unit tests for blocking control
- ❌ Missing tests for 15 endpoints
- ❌ No integration tests

**Required**:
- 18 unit tests (one per endpoint)
- 5+ integration tests (workflows)
- >80% code coverage

**Traces To**:
- **Tests**: `tests/csharp/ApplicationServiceTests.cs` (TO BE CREATED)
- **Integration**: `tests/csharp/ApplicationIntegrationTests.cs` (TO BE CREATED)

---

## 7. Non-Functional Requirements

### Performance
- Method calls SHALL have <100ms overhead beyond REW REST API latency

### Error Handling
- SHALL provide `REWApplicationException` with actionable error messages
- No raw HTTP exceptions exposed

### Cross-Language Consistency
- SHALL provide identical functionality across C#, C++, JavaScript, TypeScript

---

## 8. Acceptance Criteria Summary

**This requirement is COMPLETE when**:

- ✅ All 18 REW `/application/*` endpoints accessible via OOP interface
- ✅ `ApplicationService` class implements ADR-004 architecture
- ✅ Domain objects encapsulate REW concepts (no REST exposure)
- ✅ SubscriptionManager infrastructure implemented for REW callbacks
- ✅ >80% unit test coverage
- ✅ Integration tests pass against real REW
- ✅ Cross-language parity verified (C#, C++, JS, TS)
- ✅ Documentation complete (XML comments + examples)

**Current Status**: ⚠️ **17% Complete** (3/18 endpoints)

---

## 9. References

- **REW REST API**: <http://localhost:4735/> (Swagger documentation)
- **Architecture**: `03-architecture/views/object-model.md`
- **ADR-004**: `03-architecture/decisions/ADR-004-simple-object-model-pattern.md`
- **Tests**: `tests/csharp/ApplicationServiceTests.cs` (TO BE CREATED)
- **Implementation**: `src/csharp/Domain/Services/ApplicationService.cs`
- **Gap Analysis**: `02-requirements/functional/API_COMPLETENESS_GAP_ANALYSIS.md`

---

**Document Owner**: Requirements Team  
**Status**: Draft (17% implemented)  
**Next Review**: After each endpoint implementation milestone
