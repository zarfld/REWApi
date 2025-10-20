# ADR-005: JavaScript/TypeScript as Canonical Object Model

## Status

**Accepted** (2024-12-19)

## Context

With the adoption of the Simple Object Model Pattern (ADR-004), we need to establish a single authoritative source for object model definitions across four programming languages (C#, C++, JavaScript, TypeScript).

### Problems

1. **Multiple Sources of Truth**: Each language implementation could define its own object model, leading to inconsistencies
2. **Evolution Coordination**: Changes to the object model need to be coordinated across all languages
3. **REW API Compatibility**: Object models must perfectly match REW's expected JSON structure
4. **Validation Logic**: Business rules and parameter validation need consistent implementation

### Requirements

- Single canonical definition that other languages follow
- Natural mapping to REW API JSON expectations
- Clear interface definitions for developers
- Comprehensive validation patterns
- Easy maintenance and evolution

## Decision

**JavaScript and TypeScript implementations serve as the canonical object model definition** for all REW API client libraries.

### Rationale

1. **Natural JSON Handling**: JavaScript has native JSON object support, making it the most natural choice for REW's JSON API
2. **Type Definitions**: TypeScript provides clear interface definitions that serve as specification documents
3. **REW Compatibility**: Closest semantic match to REW's JSON expectations
4. **Simplicity**: Most straightforward object model representation
5. **Existing Implementation**: Current JS/TS implementations already demonstrate the desired patterns

### Implementation Structure

```
src/
├── js/rewApi.js          # Canonical JavaScript implementation
├── ts/rewApi.ts          # Canonical TypeScript interfaces + implementation
├── csharp/REWApi.cs      # Follows JS/TS patterns exactly
└── cpp/REWApi.cpp        # Follows JS/TS patterns exactly
```

## Canonical Object Model Authority

### TypeScript as Specification Language

TypeScript interfaces serve as the formal specification:

```typescript
// This interface definition is authoritative for ALL languages
interface MeasurementCommandParameters {
    "include cal"?: boolean;
    "append lf tail"?: boolean;
    "append hf tail"?: boolean;
    "frequency warping"?: boolean;
    "replicate data"?: boolean;
    "lf tail start"?: number;
    "lf tail slope"?: number;
    "hf tail start"?: number;
    "hf tail slope"?: number;
}
```

### JavaScript as Reference Implementation

JavaScript provides the reference implementation patterns:

```javascript
// This implementation pattern is authoritative for ALL languages
async updateEQSettings(data) {
    // Validation logic here is canonical
    if (!data || typeof data !== 'object') {
        throw new Error('EQ settings data must be an object');
    }
    
    // HTTP call pattern here is canonical
    const response = await this.httpClient.post('/rew/updateEQSettings', {
        body: JSON.stringify(data),
        headers: { 'Content-Type': 'application/json' }
    });
    
    return response;
}
```

## Cross-Language Implementation Rules

### 1. Method Signature Consistency

All languages must match JavaScript/TypeScript method signatures:

**Canonical (JS/TS)**:
```javascript
async updateEQSettings(data)
async updateAudioSettings(data)
async issueMeasurementCommandWithParams(trackId, command, parameters)
```

**C# Implementation**:
```csharp
public async Task<string> UpdateEQSettings(object data)
public async Task<string> UpdateAudioSettings(object data)  
public async Task<string> IssueMeasurementCommandWithParams(string trackId, string command, object parameters)
```

**C++ Implementation**:
```cpp
pplx::task<utility::string_t> UpdateEQSettingsAsync(const json::value& data);
pplx::task<utility::string_t> UpdateAudioSettingsAsync(const json::value& data);
pplx::task<utility::string_t> IssueMeasurementCommandWithParamsAsync(const utility::string_t& trackId, const utility::string_t& command, const json::value& parameters);
```

### 2. Object Structure Consistency

All languages must produce objects that serialize to identical JSON:

**Test Requirement**:
```javascript
// This test must pass for all language implementations
const jsParams = { "include cal": true, "append lf tail": false };
const csParams = new { include_cal = true, append_lf_tail = false };
const cppParams = /* json::value equivalent */;

// All must serialize to: {"include cal":true,"append lf tail":false}
assert(JSON.stringify(jsParams) === JSON.stringify(serializeCS(csParams)));
assert(JSON.stringify(jsParams) === JSON.stringify(serializeCpp(cppParams)));
```

### 3. Validation Logic Consistency

Validation patterns defined in JavaScript/TypeScript must be replicated:

**Canonical Validation (JS)**:
```javascript
// This validation logic is authoritative
function validateMeasurementParameters(parameters) {
    if (parameters["append lf tail"] && (!parameters["lf tail start"] || !parameters["lf tail slope"])) {
        return "Error: 'lf tail start' and 'lf tail slope' required when 'append lf tail' is true";
    }
    
    if (parameters["lf tail slope"] && parameters["lf tail slope"] < 0) {
        return "Error: 'lf tail slope' must be ≥ 0";
    }
    
    return null; // Valid
}
```

**Other Languages**: Must implement equivalent validation with identical error messages.

## Change Management Process

### 1. Object Model Evolution

When the REW API changes or new features are added:

1. **Update TypeScript interfaces** first (specification)
2. **Update JavaScript implementation** second (reference)
3. **Test against REW API** to validate correctness
4. **Update other languages** to match exactly
5. **Verify cross-language consistency** through automated tests

### 2. Validation Updates

When validation rules change:

1. **Update JavaScript validation** first
2. **Update TypeScript type definitions** if needed
3. **Update other languages** to match error messages exactly
4. **Update tests** to verify consistent behavior

### 3. New Method Addition

When adding new API methods:

1. **Design in TypeScript** (interfaces and signatures)
2. **Implement in JavaScript** (reference implementation)
3. **Validate with REW API** (functionality testing)
4. **Port to other languages** (exact signature matching)
5. **Create cross-language tests** (consistency verification)

## Quality Assurance

### Automated Consistency Verification

```javascript
// Automated test that runs in CI/CD
describe('Cross-Language Object Model Consistency', () => {
    it('should produce identical JSON across all languages', async () => {
        const testObjects = [
            { "include cal": true, "append lf tail": false },
            { "driver": "ASIO", "sampleRate": 48000 },
            // ... comprehensive test cases
        ];
        
        for (const testObj of testObjects) {
            const jsJson = JSON.stringify(testObj);
            const csJson = await callCSharpSerialization(testObj);
            const cppJson = await callCppSerialization(testObj);
            
            expect(jsJson).toBe(csJson);
            expect(jsJson).toBe(cppJson);
        }
    });
});
```

### REW API Compatibility Testing

```javascript
// Test that canonical implementations work with actual REW API
describe('REW API Compatibility', () => {
    it('should successfully call REW API with canonical objects', async () => {
        const rewApi = new REWApi('http://localhost:4735');
        
        // Test with canonical JavaScript objects
        const result = await rewApi.updateEQSettings({
            "equalizerType": "FIR",
            "enabled": true
        });
        
        expect(result).toBeDefined();
        expect(result).not.toContain('error');
    });
});
```

## Documentation Requirements

### 1. TypeScript Definitions as Specs

TypeScript interfaces must include comprehensive JSDoc documentation:

```typescript
/**
 * Parameters for REW measurement commands
 * @see https://rew-software.com/api-documentation
 */
interface MeasurementCommandParameters {
    /** Include calibration data in measurement processing */
    "include cal"?: boolean;
    
    /** 
     * Append low-frequency tail to measurement
     * @requires lf_tail_start when true
     * @requires lf_tail_slope when true
     */
    "append lf tail"?: boolean;
    
    /**
     * Low-frequency tail slope in dB/octave
     * @minimum 0
     * @required_when append_lf_tail is true
     */
    "lf tail slope"?: number;
}
```

### 2. Cross-Reference Documentation

All language implementations must reference the canonical definitions:

```csharp
/// <summary>
/// Updates EQ settings using REW API
/// </summary>
/// <param name="data">EQ settings object - see TypeScript EQSettings interface for specification</param>
/// <returns>REW API response</returns>
/// <seealso href="../ts/rewApi.ts">Canonical TypeScript definition</seealso>
public async Task<string> UpdateEQSettings(object data)
```

## Benefits

### For Developers

1. **Single Source of Truth**: No confusion about correct object structure
2. **Clear Specifications**: TypeScript interfaces serve as authoritative documentation
3. **Predictable Behavior**: Identical behavior across all language implementations
4. **Easy Learning**: Learn object model once, use in any language

### For Maintainers

1. **Simplified Evolution**: Changes start in one place (JS/TS)
2. **Consistent Testing**: Same test patterns across all languages
3. **REW Compatibility**: Natural JSON handling ensures API compatibility
4. **Quality Assurance**: Clear verification of cross-language consistency

## Related Decisions

- **ADR-004**: Simple Object Model Pattern (foundation for this decision)
- **ADR-006**: Property Name Preservation (implementation detail)

## Implementation Status

- ✅ **JavaScript**: Canonical implementation established
- ✅ **TypeScript**: Canonical interfaces defined
- ✅ **C# Partial**: Core methods following canonical pattern
- ⏳ **C++**: Needs alignment with canonical patterns
- ⏳ **Testing**: Cross-language consistency tests in development

## Success Metrics

- **Zero Drift**: All languages maintain 100% object structure consistency
- **REW Compatibility**: 100% success rate for canonical object patterns
- **Change Velocity**: New REW features implemented consistently within 1 sprint
- **Developer Experience**: Positive feedback on cross-language predictability

## Review Criteria

This decision will be reviewed when:
- REW API introduces breaking changes to object structures
- Performance requirements necessitate language-specific optimizations
- Developer feedback indicates significant usability issues
- Cross-language consistency falls below 95%

---

**Canonical Sources**:
- `src/js/rewApi.js` - Reference implementation
- `src/ts/rewApi.ts` - Authoritative interfaces and types
- `03-architecture/views/object-model.md` - Complete specification