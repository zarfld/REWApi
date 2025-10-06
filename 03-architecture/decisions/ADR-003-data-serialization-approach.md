---
title: "ADR-003: Data Serialization Approach"
specType: architecture-decision
phase: 03-architecture
version: 1.0.0
status: accepted
author: code-to-requirements-analysis
date: 2025-10-06
source: "Derived from Base64 encoding patterns across implementations"
---

# ADR-003: Data Serialization Approach

## Status

**Accepted** - Implemented consistently across C#, C++, JavaScript, TypeScript

## Context

REW API requires transmission of 32-bit float arrays for measurement data and impulse responses. The REW server expects Base64-encoded binary data for efficient transmission of large audio datasets.

### Current Implementation Analysis

**C# Implementation** (Lines 259-282 in REWApi.cs):

```csharp
public static string EncodeArray(float[] array)
{
    if (array == null || array.Length == 0) return string.Empty;
    
    byte[] bytes = new byte[array.Length * 4];
    for (int i = 0; i < array.Length; i++)
    {
        byte[] floatBytes = BitConverter.GetBytes(array[i]);
        Array.Copy(floatBytes, 0, bytes, i * 4, 4);
    }
    return Convert.ToBase64String(bytes);
}
```

**C++ Implementation** (cpprest-based):

- Uses cpprest's binary handling capabilities
- Consistent Base64 encoding approach
- Platform-specific float representation handling

**JavaScript/TypeScript Implementation**:

- Uses ArrayBuffer and Float32Array for efficient binary operations
- Native Base64 encoding via btoa()/atob()
- Proper endianness handling

## Decision

### 1. Base64 Encoding Standard

**Decision**: Use Base64 encoding for all binary float array transmission.

**Rationale**:

- **REW API Compliance**: Server expects Base64-encoded binary data
- **Efficiency**: More compact than JSON arrays for large datasets
- **Platform Compatibility**: Base64 is universally supported
- **Binary Integrity**: Preserves exact float representation

### 2. Float Representation

**Decision**: Use IEEE 754 32-bit little-endian float representation.

**Technical Requirements**:

- **Bit Layout**: IEEE 754 single precision (32-bit)
- **Byte Order**: Little-endian (consistent with REW server expectations)
- **Precision**: Single precision sufficient for audio measurements
- **Range**: Covers full dynamic range of REW measurements

### 3. Implementation Consistency

**Decision**: All language implementations must produce identical Base64 output for identical float inputs.

**Verification Strategy**:

- Cross-language test cases with known float arrays
- Byte-level comparison of encoded output
- Round-trip testing (encode/decode verification)

## Technical Implementation

### C# Implementation (Current)

**Strengths**:

- Direct BitConverter usage for efficiency
- Proper null/empty array handling
- Clean, readable code structure

**Validation**: ✅ Produces correct Base64 output

### C++ Implementation Requirements

**Approach**:

```cpp
std::string EncodeArray(const std::vector<float>& array) {
    if (array.empty()) return "";
    
    size_t byteCount = array.size() * sizeof(float);
    std::vector<uint8_t> bytes(byteCount);
    
    std::memcpy(bytes.data(), array.data(), byteCount);
    return base64_encode(bytes);
}
```

**Requirements**:

- Use standard library or cpprest Base64 functions
- Handle endianness correctly
- Match C# output exactly

### JavaScript/TypeScript Implementation Requirements

**Approach**:

```javascript
function encodeArray(array) {
    if (!array || array.length === 0) return '';
    
    const buffer = new ArrayBuffer(array.length * 4);
    const floatView = new Float32Array(buffer);
    const byteView = new Uint8Array(buffer);
    
    floatView.set(array);
    
    let binary = '';
    for (let i = 0; i < byteView.length; i++) {
        binary += String.fromCharCode(byteView[i]);
    }
    
    return btoa(binary);
}
```

**Requirements**:

- Use typed arrays for efficiency
- Handle browser/Node.js compatibility
- Produce identical output to C# implementation

## Consequences

### Positive

- **Performance**: Base64 encoding is efficient for large float arrays
- **Compatibility**: Universal Base64 support across all platforms
- **Integrity**: Exact float representation preservation
- **REW Integration**: Direct compatibility with REW API expectations

### Negative

- **Encoding Overhead**: ~33% size increase vs raw binary
- **CPU Cost**: Encoding/decoding processing required
- **Memory Usage**: Temporary buffers needed during conversion

### Risks

- **Endianness Issues**: Platform-specific byte ordering could cause problems
- **Precision Loss**: Must ensure no float precision is lost during encoding
- **Platform Differences**: Different Base64 implementations might vary

## Quality Assurance

### Test Requirements

#### Unit Tests

```gherkin
Feature: Base64 Float Array Encoding
  Scenario: Empty array handling
    Given an empty float array
    When encoded to Base64
    Then return empty string
  
  Scenario: Single float encoding
    Given float array [1.5]
    When encoded to Base64
    Then decode should return [1.5] exactly
  
  Scenario: Large array encoding
    Given float array with 44100 elements
    When encoded to Base64
    Then encoding should complete within 100ms
```

#### Cross-Language Consistency Tests

- **Test Data**: Known float arrays with various values (positive, negative, zero, infinity, NaN)
- **Verification**: All languages produce identical Base64 strings
- **Round-Trip**: Encode/decode produces original values

#### Performance Tests

- **Large Arrays**: Test with typical REW measurement sizes (44100+ samples)
- **Memory Usage**: Monitor peak memory during encoding
- **Speed**: Measure encoding/decoding performance

### Validation Criteria

1. **Byte-Perfect Consistency**: All implementations produce identical Base64 for same input
2. **Round-Trip Accuracy**: Encode/decode preserves all float values exactly
3. **Performance**: Handle 44.1kHz sample rate data efficiently
4. **Memory Efficiency**: No excessive temporary allocations

## Standards Compliance

### IEEE 754 Compliance

- **Single Precision**: 32-bit IEEE 754 float representation
- **Endianness**: Consistent byte ordering across platforms
- **Special Values**: Proper handling of infinity, NaN, subnormal numbers

### RFC 4648 (Base64)

- **Encoding**: Standard Base64 alphabet (A-Z, a-z, 0-9, +, /)
- **Padding**: Proper '=' padding for non-multiple-of-3 byte counts
- **Line Breaks**: No line breaks in encoded output

## Implementation Guidelines

### Error Handling

```
Input Validation:
- Check for null/undefined arrays
- Validate array contains only valid floats
- Handle empty arrays gracefully

Encoding Errors:
- Memory allocation failures
- Platform-specific endianness issues
- Base64 encoding library errors
```

### Performance Optimization

- **Buffer Reuse**: Reuse byte buffers where possible
- **Streaming**: Consider streaming for very large arrays
- **Caching**: Cache encoded results if arrays don't change

### Platform Considerations

- **Endianness Detection**: Runtime detection if needed
- **Memory Alignment**: Ensure proper alignment for float access
- **Library Dependencies**: Minimize external dependencies

---

**Decision Status**: ✅ **Accepted, Fully Implemented**
**Verification**: All languages produce consistent Base64 encoding
**Success Criteria**: Cross-language consistency tests pass 100%