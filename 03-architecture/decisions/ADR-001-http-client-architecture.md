---
title: "ADR-001: HTTP Client Architecture"
specType: architecture-decision
phase: 03-architecture
version: 1.0.0
status: accepted
author: code-to-requirements-analysis
date: 2025-10-06
source: "Derived from existing implementation patterns"
---

# ADR-001: HTTP Client Architecture for REW API Communication

## Status
**Accepted** - Implemented across all language versions

## Context

The REW API client library needs to communicate with REW's HTTP API server running on localhost:4735. Each language implementation (C#, C++, JavaScript, TypeScript) requires HTTP communication capabilities with consistent behavior and error handling.

### Technical Requirements
- HTTP GET, PUT, POST operations
- JSON content handling
- Error response processing
- Connection management
- Asynchronous operation support

### Implementation Analysis
Current implementations show different approaches:

**C# Implementation**:
```csharp
private static readonly HttpClient client = new HttpClient();

public async Task<string> GetAsync(string endpoint)
{
    if (graphUpdateInhibited) return null;
    HttpResponseMessage response = await client.GetAsync(endpoint);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}
```

**JavaScript/TypeScript Implementation**:
```javascript
async get(endpoint) {
    if (this.graphUpdateInhibited) return null;
    const response = await fetch(endpoint);
    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }
    return await response.json();
}
```

## Decision

### Core Architecture Decisions

#### 1. Static HTTP Client Pattern (C#)
**Decision**: Use a single static HttpClient instance for connection reuse.

**Rationale**:
- **Performance**: Avoids connection setup overhead
- **Resource Management**: Prevents socket exhaustion
- **Microsoft Recommendation**: Aligns with .NET best practices

**Implementation Evidence**: `private static readonly HttpClient client = new HttpClient();`

#### 2. Fetch API Pattern (JavaScript/TypeScript)
**Decision**: Use native fetch() API with custom error handling.

**Rationale**:
- **Standard Compliance**: Uses web standard API
- **Simplicity**: No additional dependencies required
- **Modern Support**: Available in Node.js 18+ and all modern browsers

#### 3. CPPREST SDK Pattern (C++)
**Decision**: Use Microsoft's C++ REST SDK (cpprest).

**Rationale**:
- **Async Support**: Provides pplx::task-based asynchronous operations
- **JSON Integration**: Built-in JSON handling
- **Cross-Platform**: Works on Windows, Linux, macOS

## Consequences

### Positive
- **Consistent Behavior**: All implementations provide async HTTP operations
- **Error Handling**: Each language uses appropriate error mechanisms
- **Performance**: Connection reuse where applicable
- **Maintainability**: Language-idiomatic approaches

### Negative
- **Different APIs**: Each language has different method signatures
- **Error Types**: Different exception/error types across languages
- **Dependency Management**: C++ requires additional SDK installation

### Neutral
- **Memory Usage**: Minimal impact, appropriate for single REW instance communication
- **Scalability**: Adequate for REW's single-instance architecture

## Implementation Guidelines

### Error Handling Standards
All implementations must:
1. Check graph update inhibition before making requests
2. Validate HTTP response status
3. Throw appropriate language-specific exceptions
4. Include HTTP status codes in error messages

### Connection Management
- **C#**: Reuse static HttpClient instance
- **C++**: Manage http_client lifecycle appropriately
- **JS/TS**: Rely on fetch() connection management

### JSON Handling
- **C#**: Use StringContent with UTF-8 encoding
- **C++**: Use web::json::value for structured data
- **JS/TS**: Use JSON.stringify/parse with native objects

## Compliance and Standards

### ISO/IEC/IEEE 42010:2011 Compliance
- **Architecture Viewpoint**: Implementation view
- **Stakeholder Concerns**: Performance, maintainability, consistency
- **Architecture Decision**: Documented with rationale and consequences

### REW API Compliance
- **Endpoint Support**: All implementations support GET, PUT, POST
- **Content-Type**: application/json for structured data
- **Error Handling**: Appropriate response to REW API error codes

---

**Decision Status**: ✅ **Implemented and Validated**  
**Review Date**: 2025-12-06 (6 months)  
**Related ADRs**: ADR-002 (Parameter Validation), ADR-003 (Data Serialization)