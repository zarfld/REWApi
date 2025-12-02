---
title: "ADR-006: Property Name Preservation"
specType: architecture-decision
standard: "ISO/IEC/IEEE 42010:2011"
phase: 03-architecture
version: "1.0.0"
status: accepted
author: architecture-team
date: "2024-12-19"
description: "Decision on preserving REW API property names exactly as-is across all language implementations"
---

# ADR-006: Property Name Preservation

## Status

**Accepted** (2024-12-19)

## Context

The REW (Room EQ Wizard) HTTP API uses JSON objects with specific property naming conventions that include spaces, specific casing, and non-standard formats. Our REW API client library must interact with these endpoints while maintaining consistency across four programming languages.

### REW API Property Examples

The REW API uses property names such as:
- `"include cal"` (with space)
- `"append lf tail"` (with spaces)
- `"append hf tail"` (with spaces)
- `"frequency warping"` (with space)
- `"lf tail start"` (with spaces)
- `"hf tail slope"` (with spaces)

### Programming Language Conventions

Different languages have different property naming conventions:
- **JavaScript**: Generally camelCase (`includeCal`, `appendLfTail`)
- **C#**: Generally PascalCase (`IncludeCal`, `AppendLfTail`)
- **C++**: Generally snake_case (`include_cal`, `append_lf_tail`)
- **TypeScript**: Generally camelCase (`includeCal`, `appendLfTail`)

### Problems

1. **Translation Complexity**: Converting between language conventions and REW API requires mapping logic
2. **Mapping Maintenance**: Property name mappings must be kept in sync across languages
3. **Documentation Confusion**: Developers see different property names in code vs. REW documentation
4. **Error Messages**: Error messages from REW API reference original property names
5. **Debugging Difficulty**: JSON payloads don't match code property names

## Decision

**Preserve REW API property names exactly in all programming languages**, including spaces, casing, and formatting.

### Core Principle

Use REW property names **as-is** across all implementations:
- ✅ `"include cal"` (exact REW API name)
- ❌ `"includeCal"` (camelCase conversion)
- ❌ `"include_cal"` (snake_case conversion)
- ❌ `"IncludeCal"` (PascalCase conversion)

### Implementation Patterns

**JavaScript (canonical)**:
```javascript
const parameters = {
    "include cal": true,
    "append lf tail": false,
    "append hf tail": true,
    "frequency warping": true,
    "lf tail start": 10000,
    "hf tail slope": -6.0
};
```

**TypeScript (canonical)**:
```typescript
interface MeasurementCommandParameters {
    "include cal"?: boolean;
    "append lf tail"?: boolean;
    "append hf tail"?: boolean;
    "frequency warping"?: boolean;
    "lf tail start"?: number;
    "hf tail slope"?: number;
}
```

**C# (following pattern)**:
```csharp
// Using anonymous objects with exact property names
var parameters = new Dictionary<string, object>
{
    ["include cal"] = true,
    ["append lf tail"] = false,
    ["append hf tail"] = true,
    ["frequency warping"] = true,
    ["lf tail start"] = 10000,
    ["hf tail slope"] = -6.0
};
```

**C++ (following pattern)**:
```cpp
json::value parameters = json::value::object();
parameters["include cal"] = json::value::boolean(true);
parameters["append lf tail"] = json::value::boolean(false);
parameters["append hf tail"] = json::value::boolean(true);
parameters["frequency warping"] = json::value::boolean(true);
parameters["lf tail start"] = json::value::number(10000);
parameters["hf tail slope"] = json::value::number(-6.0);
```

## Rationale

### Advantages

1. **Zero Translation**: No property name mapping or conversion logic required
2. **REW Compatibility**: Direct compatibility with REW API JSON expectations
3. **Documentation Alignment**: Code matches REW documentation exactly
4. **Error Message Clarity**: REW error messages reference property names developers see in code
5. **Debugging Simplicity**: JSON payloads match code property names exactly
6. **Maintenance Reduction**: No mapping tables to maintain across languages
7. **Consistency**: Same property names across all four language implementations

### Challenges

1. **Language Convention Deviation**: Property names don't follow standard language conventions
2. **IDE Support**: Some IDEs may flag unusual property names
3. **Quoted Properties**: Languages may require quoted property access

### Mitigation Strategies

1. **Documentation**: Clear explanation of REW property naming in documentation
2. **Code Comments**: Comments explaining REW API property name requirements
3. **Linting Rules**: Custom linting rules to accept REW property patterns
4. **IDE Configuration**: Configure IDEs to accept quoted property names

## Implementation Requirements

### 1. Exact Property Name Matching

All languages must use identical property names that serialize to exact REW API JSON:

**Required JSON Output**:
```json
{
    "include cal": true,
    "append lf tail": false,
    "frequency warping": true,
    "lf tail start": 10000
}
```

**Language-Specific Implementation**:

**JavaScript/TypeScript**: Native support
```javascript
const obj = { "include cal": true };  // Natural syntax
```

**C#**: Dictionary or dynamic object
```csharp
var obj = new Dictionary<string, object> { ["include cal"] = true };
// OR
dynamic obj = new ExpandoObject();
obj["include cal"] = true;
```

**C++**: JSON object with string keys
```cpp
json::value obj = json::value::object();
obj["include cal"] = json::value::boolean(true);
```

### 2. Cross-Language Serialization Consistency

All implementations must produce byte-identical JSON:

**Test Requirement**:
```javascript
// This test must pass for all language implementations
describe('Property Name Consistency', () => {
    it('should serialize to identical JSON across languages', () => {
        const expectedJson = '{"include cal":true,"append lf tail":false}';
        
        expect(serializeJS(jsObject)).toBe(expectedJson);
        expect(serializeCS(csObject)).toBe(expectedJson);
        expect(serializeCpp(cppObject)).toBe(expectedJson);
    });
});
```

### 3. Type Safety Considerations

**TypeScript**: Use string literal types for property names
```typescript
interface MeasurementParams {
    "include cal"?: boolean;           // String literal type
    "append lf tail"?: boolean;        // String literal type
    "lf tail start"?: number;          // String literal type
}

// Usage with type safety
const params: MeasurementParams = {
    "include cal": true,               // Type-checked
    "append lf tail": false            // Type-checked
};
```

**C#**: Use const strings for property validation
```csharp
public static class REWProperties
{
    public const string IncludeCal = "include cal";
    public const string AppendLfTail = "append lf tail";
    public const string LfTailStart = "lf tail start";
}

// Usage with validation
var parameters = new Dictionary<string, object>
{
    [REWProperties.IncludeCal] = true,
    [REWProperties.AppendLfTail] = false
};
```

## Property Name Catalog

### Measurement Command Parameters

| REW Property | Type | Description |
|--------------|------|-------------|
| `"include cal"` | boolean | Include calibration data |
| `"append lf tail"` | boolean | Append low-frequency tail |
| `"append hf tail"` | boolean | Append high-frequency tail |
| `"frequency warping"` | boolean | Apply frequency warping |
| `"replicate data"` | boolean | Replicate measurement data |
| `"lf tail start"` | number | Low-frequency tail start (Hz) |
| `"lf tail slope"` | number | Low-frequency tail slope (dB/octave) |
| `"hf tail start"` | number | High-frequency tail start (Hz) |
| `"hf tail slope"` | number | High-frequency tail slope (dB/octave) |

### Audio Settings Parameters

| REW Property | Type | Description |
|--------------|------|-------------|
| `"driver"` | string | Audio driver type |
| `"sampleRate"` | number | Sample rate (Hz) |
| `"inputDevice"` | object | Input device configuration |
| `"outputDevice"` | object | Output device configuration |

### EQ Settings Parameters

| REW Property | Type | Description |
|--------------|------|-------------|
| `"equalizerType"` | string | Equalizer type |
| `"targetCurve"` | string | Target curve name |
| `"maxBoost"` | number | Maximum boost (dB) |
| `"maxCut"` | number | Maximum cut (dB) |

## Validation and Error Handling

### Property Name Validation

```javascript
// Canonical validation (JavaScript)
function validatePropertyNames(obj, allowedProperties) {
    const objProperties = Object.keys(obj);
    const invalidProperties = objProperties.filter(prop => !allowedProperties.includes(prop));
    
    if (invalidProperties.length > 0) {
        throw new Error(`Invalid properties: ${invalidProperties.join(', ')}. Allowed properties: ${allowedProperties.join(', ')}`);
    }
}

// Usage
const allowedMeasurementProperties = [
    "include cal",
    "append lf tail", 
    "append hf tail",
    "frequency warping",
    "replicate data",
    "lf tail start",
    "lf tail slope",
    "hf tail start",
    "hf tail slope"
];

validatePropertyNames(parameters, allowedMeasurementProperties);
```

### Error Message Consistency

All languages must produce consistent error messages using exact property names:

```javascript
// Standard error message format
function validateLfTailSlope(value) {
    if (typeof value !== 'number' || value < 0) {
        return "Error: Invalid value for parameter 'lf tail slope'. Must be a number ≥ 0.";
    }
    return null;
}
```

## IDE and Development Tool Support

### TypeScript Configuration

Configure TypeScript to accept quoted property names:
```json
{
    "compilerOptions": {
        "suppressImplicitAnyIndexErrors": true
    }
}
```

### ESLint Configuration

Configure ESLint to accept REW property patterns:
```json
{
    "rules": {
        "quote-props": ["error", "as-needed", { 
            "keywords": false, 
            "unnecessary": false 
        }]
    }
}
```

### C# IDE Configuration

Use pragma directives for unusual property names:
```csharp
#pragma warning disable IDE1006 // Naming rule violation - REW API property names
var parameters = new Dictionary<string, object>
{
    ["include cal"] = true  // REW API requires exact property name
};
#pragma warning restore IDE1006
```

## Testing Strategy

### Cross-Language Property Consistency

```javascript
describe('REW Property Name Preservation', () => {
    const testCases = [
        { property: "include cal", value: true },
        { property: "append lf tail", value: false },
        { property: "lf tail start", value: 1000 },
        { property: "hf tail slope", value: -6.0 }
    ];
    
    testCases.forEach(({ property, value }) => {
        it(`should preserve property "${property}" across languages`, () => {
            const jsObj = { [property]: value };
            const jsJson = JSON.stringify(jsObj);
            
            // Test that all languages produce identical JSON
            expect(serializeCS(createCSObject(property, value))).toBe(jsJson);
            expect(serializeCpp(createCppObject(property, value))).toBe(jsJson);
            expect(serializeTS(createTSObject(property, value))).toBe(jsJson);
        });
    });
});
```

### REW API Compatibility

```javascript
describe('REW API Property Compatibility', () => {
    it('should successfully send REW properties to actual API', async () => {
        const rewApi = new REWApi('http://localhost:4735');
        
        // Test with exact REW property names
        const response = await rewApi.issueMeasurementCommandWithParams("123", "Smooth", {
            "include cal": true,
            "append lf tail": false,
            "frequency warping": true
        });
        
        expect(response).not.toContain('error');
        expect(response).not.toContain('unknown parameter');
    });
});
```

## Migration from Previous Approaches

### If Using camelCase Properties

**Before**:
```javascript
const params = {
    includeCal: true,
    appendLfTail: false,
    lfTailStart: 1000
};
```

**After**:
```javascript
const params = {
    "include cal": true,
    "append lf tail": false,
    "lf tail start": 1000
};
```

### If Using snake_case Properties

**Before**:
```javascript
const params = {
    include_cal: true,
    append_lf_tail: false,
    lf_tail_start: 1000
};
```

**After**:
```javascript
const params = {
    "include cal": true,
    "append lf tail": false,
    "lf tail start": 1000
};
```

## Related Decisions

- **ADR-004**: Simple Object Model Pattern (foundation)
- **ADR-005**: JavaScript/TypeScript as Canonical (authority source)

## Benefits

### For REW API Compatibility
- ✅ **Zero Translation Layer**: Direct property mapping to REW API
- ✅ **Error Message Alignment**: REW errors reference property names developers use
- ✅ **Documentation Consistency**: Code matches REW API documentation

### For Cross-Language Consistency
- ✅ **Identical JSON Output**: All languages produce same JSON
- ✅ **Simplified Testing**: Single test pattern for all languages
- ✅ **Reduced Complexity**: No property name mapping logic

### For Maintenance
- ✅ **Single Source of Truth**: Property names defined once in REW API
- ✅ **Automatic Updates**: New REW properties work immediately
- ✅ **Reduced Bugs**: No property name mapping errors

## Challenges and Mitigation

### Challenge: Unusual Property Names in Code

**Mitigation**: 
- Comprehensive documentation explaining REW requirements
- Code comments linking to REW API documentation
- IDE configuration to accept quoted properties

### Challenge: Language Convention Violation

**Mitigation**:
- Clear architectural decision documentation
- Team training on REW property requirements
- Linting rule customization

### Challenge: IDE Support Issues

**Mitigation**:
- TypeScript string literal types for type safety
- C# const strings for IntelliSense support
- Custom IDE snippets for common property patterns

## Success Metrics

- **JSON Consistency**: 100% identical JSON output across all languages
- **REW Compatibility**: 0% property name related API errors
- **Developer Experience**: Positive feedback on property name predictability
- **Maintenance Burden**: Reduced property mapping maintenance time

## Review and Evolution

This decision will be reviewed when:
- REW API changes property naming conventions
- Significant developer feedback indicates usability problems
- New languages are added to the client library
- IDE support limitations create major development friction

---

**Implementation Status**:
- ✅ **JavaScript/TypeScript**: Native quoted property support
- ✅ **C# Partial**: Dictionary-based implementation in progress
- ⏳ **C++**: JSON object implementation needed
- ⏳ **Testing**: Cross-language property consistency tests needed