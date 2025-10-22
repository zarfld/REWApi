# MeasureService API Response Fixes

## Issue Summary
The original implementation assumed complex objects for all endpoints, but the actual REW API returns primitive values (string, int, double, boolean) for most measure configuration endpoints.

## Actual API Responses (from OpenAPI spec localhost:4735/doc.json)

### Simple Values (Primitives)
1. `/measure/notes` → `string` (empty string)
2. `/measure/sweep/repetitions` → `integer` (e.g., 1)
3. `/measure/timing/reference` → `string` (e.g., "None")
4. `/measure/timing/offset` → `double` (e.g., 0.0)
5. `/measure/playback-mode` → `string` (e.g., "From REW")
6. `/measure/measurement-mode` → `string` (e.g., "Single")
7. `/measure/number-of-repetitions` → `integer` (e.g., 1)
8. `/measure/start-delay` → `integer` (e.g., 0)
9. `/measure/invert-second-output` → `boolean` (e.g., false)
10. `/measure/fill-silence-with-dither` → `boolean` (e.g., false)
11. `/measure/capture-noise-floor` → `boolean` (e.g., true)

### Complex Objects
12. `/measure/sweep/configuration` → `MeasSweepConfiguration`:
    ```json
    {
      "startFrequency": 0,
      "endFrequency": 20000,
      "length": "256k",
      "fillSilenceWithDither": false
    }
    ```

## Endpoint Corrections
- `/measure/timing` → **404 NOT FOUND** (wrong endpoint)
- `/measure/timing/reference` → **CORRECT**
- `/measure/timing-offset` → **404 NOT FOUND** (wrong endpoint)
- `/measure/timing/offset` → **CORRECT**
- `/measure/sweep` → **404 NOT FOUND** (wrong endpoint)
- `/measure/sweep/configuration` → **CORRECT**

## Required Changes

### 1. Update Domain Objects (Keep for POST requests)
Domain objects are still needed for setting values, but GET methods need to deserialize primitives.

### 2. Update Service Methods
Each GET method that returns a primitive should:
1. Deserialize the primitive value
2. Wrap it in the domain object
3. Return the domain object

Example:
```csharp
// OLD (incorrect):
var content = await response.Content.ReadAsStringAsync();
return JsonSerializer.Deserialize<MeasureNotes>(content) ?? new MeasureNotes();

// NEW (correct):
var content = await response.Content.ReadAsStringAsync();
var notes = JsonSerializer.Deserialize<string>(content) ?? string.Empty;
return new MeasureNotes { Notes = notes };
```

### 3. Fix SweepConfiguration
The `length` field is a STRING ("256k"), not a double!

```csharp
// Update SweepConfiguration.Length from double to string
public string Length { get; set; } = "256k";
```

## Testing Plan
1. Run diagnostic tests to confirm API responses
2. Update service methods to match actual API
3. Update domain objects where needed
4. Re-run all 33 tests
5. Target: 33/33 passing (100%)
