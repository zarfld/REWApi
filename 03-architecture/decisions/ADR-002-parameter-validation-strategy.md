---
title: "ADR-002: Parameter Validation Strategy"
specType: architecture-decision
phase: 03-architecture
version: 1.0.0
status: accepted
author: code-to-requirements-analysis
date: 2025-10-06
source: "Derived from C# implementation patterns"
---

# ADR-002: Parameter Validation Strategy

## Status
**Accepted** - Partially implemented (C# complete, others need implementation)

## Context

REW API commands require complex parameter validation to prevent invalid requests from being sent to the REW server. The validation rules are specific to each command and include type checking, range validation, and dependency validation.

### Current Implementation Analysis

**C# Implementation** (Lines 41-120 in REWApi.cs):
```csharp
// Boolean parameter validation
var booleanParams = new[] { "include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data" };
foreach (var param in booleanParams)
{
    if (parameters.Headers.Contains(param) && !bool.TryParse(parameters.Headers.GetValues(param).FirstOrDefault(), out _))
    {
        return $"Error: Invalid value for parameter '{param}'. Expected a boolean value.";
    }
}

// Numeric range validation
if (parameters.Headers.Contains("lf tail slope") && double.TryParse(parameters.Headers.GetValues("lf tail slope").FirstOrDefault(), out var lfTailSlope) && lfTailSlope < 0)
{
    return "Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0.";
}
```

**Other Languages**: Currently lack comprehensive parameter validation.

## Decision

### 1. Client-Side Validation Strategy
**Decision**: Implement comprehensive parameter validation on the client side before sending requests to REW API.

**Rationale**:
- **Error Prevention**: Catch invalid parameters before network transmission
- **User Experience**: Immediate feedback without round-trip to server
- **Server Protection**: Reduce invalid requests to REW API
- **Debugging**: Clear error messages help developers identify issues

### 2. Validation Rule Categories

#### Type Validation
- **Boolean Parameters**: "include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data"
- **Numeric Parameters**: Integer and floating-point values
- **String Parameters**: Non-empty strings where required

#### Range Validation
- **lf tail slope**: Must be ≥ 0
- **hf tail slope**: Must be ≤ 0
- **octaveFrac**: Must be valid integer
- **filterOrder**: Must be positive integer

#### Dependency Validation
- If "append lf tail" is true, require "lf tail start" and "lf tail slope"
- If "append hf tail" is true, require "hf tail start", "hf tail slope", and "frequency warping"
- If neither tail option is true, require "replicate data"

### 3. Implementation Consistency
**Decision**: All language implementations must have identical validation logic.

**Implementation Strategy**:
1. Extract validation rules from C# implementation
2. Create validation specification document
3. Implement identical validation in C++, JavaScript, TypeScript
4. Create comprehensive test cases for all validations

## Consequences

### Positive
- **Reliability**: Prevents invalid commands from reaching REW API
- **Developer Experience**: Clear, immediate error messages
- **Consistency**: Identical behavior across all language implementations
- **Maintainability**: Centralized validation logic per language

### Negative
- **Code Duplication**: Validation logic must be maintained in 4 languages
- **Maintenance Overhead**: Changes require updates in all implementations
- **Performance**: Additional processing before API calls

### Risks
- **Validation Drift**: Different languages might implement validation differently over time
- **REW API Changes**: Server-side validation changes require client updates

## Implementation Guidelines

### Error Message Format
Standardized error message format:
```
"Error: Invalid value for parameter '{paramName}'. {specificRequirement}"
```

Examples:
- "Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0."
- "Error: Missing required parameter 'smoothing' for 'Smooth' command."

### Validation Order
1. **Presence Validation**: Check required parameters exist
2. **Type Validation**: Verify parameter types
3. **Range Validation**: Check numeric ranges
4. **Dependency Validation**: Verify parameter dependencies

### Language-Specific Implementation

#### C# (Already Implemented)
- Use `bool.TryParse()`, `double.TryParse()` for type checking
- Use conditional logic for dependency validation
- Return error strings for failures

#### C++ (To Be Implemented)
- Use string parsing with validation functions
- Return status codes or throw exceptions
- Match C# validation logic exactly

#### JavaScript/TypeScript (To Be Implemented)
- Use `typeof` checks and `parseFloat()`/`parseInt()`
- Throw Error objects with standardized messages
- Implement identical validation rules

## Validation Rules Specification

### Command-Specific Validation

#### "Smooth" Command
- **Required**: "smoothing" parameter (string)
- **Valid Values**: "1/12", "1/6", "1/3", "1/1", etc.

#### "Offset t=0" Command
- **Required**: "offset" (string/numeric), "unit" (string)
- **Valid Units**: "seconds", "metres", "feet", "samples"

#### "Generate RT60" Command
- **Required**: "octaveFrac" (integer), "filterOrder" (integer), "zeroPhaseFiltered" (boolean), "reverseFiltered" (boolean)
- **Valid octaveFrac**: 1, 3 (for full octave, third octave)

#### Minimum/Excess Phase Commands
- **Complex Dependencies**: Multiple boolean flags with conditional requirements
- **Range Constraints**: Slope values with specific ranges
- **Type Requirements**: Mix of boolean, numeric, and string parameters

## Testing Requirements

### Unit Tests Required
- All validation rules with valid/invalid inputs
- Edge cases (null, empty, boundary values)
- Cross-language consistency verification

### Integration Tests Required
- Validation behavior with actual REW API
- Error message consistency across languages

## Standards Compliance

### ISO/IEC/IEEE 29148:2018
- **Section 5.2.7**: Interface validation requirements
- **Section 6.4**: Requirements traceability (validation rules to tests)

### Code Quality Standards
- **Defensive Programming**: Validate all inputs
- **Fail-Fast Principle**: Catch errors as early as possible
- **Clear Error Messages**: Help developers identify and fix issues

---

**Decision Status**: ✅ **Accepted, Partially Implemented**  
**Next Steps**: Implement validation in C++, JavaScript, TypeScript  
**Success Criteria**: 100% validation consistency across all languages