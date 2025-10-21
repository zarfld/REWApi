# Phase 3: EQ Endpoints API Map

**Document**: REQ-IMPL-EQ-MAP-001  
**Date**: 2025-01-24  
**Status**: Exploration Complete  
**Phase**: 05-Implementation / Phase 3  

## Overview

Complete mapping of REW API EQ and Filter endpoints based on:
- API Documentation: https://www.roomeqwizard.com/help/help_en-GB/html/api.html
- Live API Testing: http://localhost:4735/
- Exploration Date: 2025-01-24

## Endpoint Categories

### 1. Global EQ Defaults (9 endpoints)

#### 1.1 Available Equalisers
- **Endpoint**: `GET /eq/equalisers`
- **Optional Query**: `?manufacturer={name}` - Filter by manufacturer
- **Response**:
  ```json
  {
    "value": [
      {"manufacturer": "Generic", "model": "Generic"},
      {"manufacturer": "MiniDSP", "model": "2x4 HD"},
      {"manufacturer": "Behringer", "model": "DCX2496"}
      // ... 50 total equalisers
    ],
    "Count": 50
  }
  ```
- **Domain Object**: `EqualiserInfo[]`
- **Notes**: 50 built-in equaliser profiles from 33 manufacturers

#### 1.2 Manufacturers List
- **Endpoint**: `GET /eq/manufacturers`
- **Response**:
  ```json
  {
    "value": ["Generic", "Apple", "MiniDSP", "Behringer", ...],
    "Count": 33
  }
  ```
- **Domain Object**: `string[]`
- **Notes**: 33 manufacturers available

#### 1.3 Default Equaliser
- **Endpoint**: `GET /eq/default-equaliser`
- **Response**: `{"manufacturer": "Generic", "model": "Generic"}`
- **Domain Object**: `EqualiserInfo`
- **Write Operations**: 
  - `POST /eq/default-equaliser` - Set default (body: EqualiserInfo)

#### 1.4 Default Target Settings
- **Endpoint**: `GET /eq/default-target-settings`
- **Response**:
  ```json
  {
    "shape": "Subwoofer",
    "bassManagementSlopedBPerOctave": 12,
    "bassManagementCutoffHz": 80,
    "lowFreqSlopedBPerOctave": 24,
    "lowFreqCutoffHz": 10,
    "lowPassCrossoverType": "L-R2",
    "highPassCrossoverType": "L-R2",
    "lowPassCutoffHz": 1000,
    "highPassCutoffHz": 100
  }
  ```
- **Domain Object**: `TargetSettings`
- **Shape Values**: "Full range", "Subwoofer", "LF", "LF + sub", "Other"
- **Crossover Types**: "Butterworth", "L-R2", "L-R4"
- **Write Operations**:
  - `POST /eq/default-target-settings` - Replace all settings
  - `PUT /eq/default-target-settings` - Update partial settings

#### 1.5 Default Target Level
- **Endpoint**: `GET /eq/default-target-level`
- **Response**: `75.0` (dB SPL)
- **Domain Object**: `double`
- **Write Operations**:
  - `POST /eq/default-target-level` - Set default (body: double)

#### 1.6 Default Room Curve Settings
- **Endpoint**: `GET /eq/default-room-curve-settings`
- **Response**:
  ```json
  {
    "addRoomCurve": false,
    "lowFreqRiseStartHz": 200,
    "lowFreqRiseEndHz": 20,
    "lowFreqRiseSlopedBPerOctave": 1.0,
    "highFreqFallStartHz": 1000,
    "highFreqFallSlopedBPerOctave": 0.5
  }
  ```
- **Domain Object**: `RoomCurveSettings`
- **Write Operations**:
  - `POST /eq/default-room-curve-settings` - Replace all settings
  - `PUT /eq/default-room-curve-settings` - Update partial settings

#### 1.7 House Curve
- **Endpoint**: `GET /eq/house-curve`
- **Response**: `""` or `"C:\\path\\to\\curve.txt"`
- **Domain Object**: `string` (file path)
- **Write Operations**:
  - `POST /eq/house-curve` - Set house curve file (body: string path)
  - `DELETE /eq/house-curve` - Clear house curve

#### 1.8 House Curve Log Interpolation
- **Endpoint**: `POST /eq/house-curve-log-interpolation`
- **Body**: `boolean` - Enable/disable log interpolation
- **Response**: Status message
- **Notes**: Controls how house curve is interpolated

#### 1.9 Match Target Settings
- **Endpoint**: `GET /eq/match-target-settings`
- **Response**:
  ```json
  {
    "startFrequency": 20,
    "endFrequency": 100,
    "individualMaxBoostdB": 9,
    "overallMaxBoostdB": 0,
    "flatnessTargetdB": 3,
    "allowNarrowFiltersBelow200Hz": true,
    "varyQAbove200Hz": false,
    "allowLowShelf": false,
    "lowShelfMin": -6,
    "lowShelfMax": 6,
    "allowHighShelf": false,
    "highShelfMin": -6,
    "highShelfMax": 6
  }
  ```
- **Domain Object**: `MatchTargetSettings`
- **Write Operations**:
  - `POST /eq/match-target-settings` - Replace all settings
  - `PUT /eq/match-target-settings` - Update partial settings
- **Notes**: Controls filter matching algorithm parameters

---

### 2. Measurement-Specific EQ Settings (10 endpoints)

#### 2.1 Measurement Equaliser
- **Endpoint**: `GET /measurements/{id}/equaliser`
- **Response**: `{"manufacturer": "Generic", "model": "Generic"}`
- **Domain Object**: `EqualiserInfo`
- **Write Operations**:
  - `POST /measurements/{id}/equaliser` - Set equaliser (body: EqualiserInfo)
- **Notes**: Overrides default for specific measurement

#### 2.2 Measurement Filters List
- **Endpoint**: `GET /measurements/{id}/filters`
- **Response**:
  ```json
  {
    "value": [
      {"index": 1, "type": "None", "enabled": true, "isAuto": true},
      {"index": 2, "type": "None", "enabled": true, "isAuto": true},
      // ... 22 filters total
    ],
    "Count": 22
  }
  ```
- **Domain Object**: `FilterSetting[]`
- **Filter Types**: "None", "PEQ", "Low Shelf", "High Shelf", "Low Pass", "High Pass", "Notch", "All Pass"
- **Notes**: Number of filters depends on selected equaliser

#### 2.3 Single Filter Update
- **Endpoint**: `PUT /measurements/{id}/filters`
- **Body**: Complete FilterSetting object
- **Response**: Updated filter
- **Notes**: Updates one filter at a time

#### 2.4 Multiple Filters Update
- **Endpoint**: `POST /measurements/{id}/filters`
- **Body**: Array of FilterSetting objects
- **Response**: Updated filters array
- **Notes**: Updates multiple filters in one call

#### 2.5 Measurement Target Settings
- **Endpoint**: `GET /measurements/{id}/target-settings`
- **Response**: Same structure as default-target-settings
- **Domain Object**: `TargetSettings`
- **Write Operations**:
  - `POST /measurements/{id}/target-settings` - Replace all
  - `PUT /measurements/{id}/target-settings` - Update partial
- **Notes**: Shape was "Full range" for tested measurement

#### 2.6 Measurement Target Level
- **Endpoint**: `GET /measurements/{id}/target-level`
- **Response**: `75.0` (dB SPL)
- **Domain Object**: `double`
- **Write Operations**:
  - `POST /measurements/{id}/target-level` - Set target level

#### 2.7 Measurement Room Curve Settings
- **Endpoint**: `GET /measurements/{id}/room-curve-settings`
- **Response**: Same structure as default-room-curve-settings
- **Domain Object**: `RoomCurveSettings`
- **Write Operations**:
  - `POST /measurements/{id}/room-curve-settings` - Replace all
  - `PUT /measurements/{id}/room-curve-settings` - Update partial

#### 2.8 Target Response Curve
- **Endpoint**: `GET /measurements/{id}/target-response`
- **Response**: GZIP-compressed frequency response data
- **Domain Object**: `FrequencyResponseData`
- **Notes**: Returns target curve based on target settings

#### 2.9 EQ Predicted Frequency Response
- **Endpoint**: `GET /measurements/{id}/eq/frequency-response`
- **Response**: GZIP-compressed frequency response data
- **Domain Object**: `FrequencyResponseData`
- **Notes**: Returns measurement response with EQ filters applied

#### 2.10 EQ Predicted Group Delay
- **Endpoint**: `GET /measurements/{id}/eq/group-delay`
- **Response**: GZIP-compressed group delay data
- **Domain Object**: `GroupDelayData`
- **Notes**: Returns group delay with EQ filters applied

#### 2.11 EQ Predicted Impulse Response
- **Endpoint**: `GET /measurements/{id}/eq/impulse-response`
- **Response**: GZIP-compressed impulse response data
- **Domain Object**: `ImpulseResponseData`
- **Notes**: Returns impulse response with EQ filters applied

---

### 3. EQ Commands (2 endpoints)

#### 3.1 Available EQ Commands
- **Endpoint**: `GET /measurements/eq/commands`
- **Response**:
  ```json
  {
    "value": [
      "Calculate target level",
      "Match target",
      "Optimise gains",
      "Optimise gains and Qs",
      "Optimise gains, Qs and Fcs",
      "Generate predicted measurement",
      "Generate filters measurement",
      "Generate target measurement"
    ],
    "Count": 8
  }
  ```
- **Domain Object**: `string[]`
- **Notes**: 8 available EQ operations

#### 3.2 Execute EQ Command
- **Endpoint**: `POST /measurements/{id}/eq/command`
- **Body**:
  ```json
  {
    "command": "Match target",
    "parameters": {}
  }
  ```
- **Response**: 
  ```json
  {
    "message": "Match target ID 1 in progress",
    "id": 1
  }
  ```
- **Domain Object**: `CommandResult`
- **Notes**: 
  - Long-running operations return progress ID
  - Can subscribe to progress via `/eq/subscribe` (WebSocket/SignalR)
  - Available commands:
    * "Calculate target level" - Computes optimal target level from measurement
    * "Match target" - Generates filters to match target curve
    * "Optimise gains" - Optimizes filter gain values
    * "Optimise gains and Qs" - Optimizes both gain and Q values
    * "Optimise gains, Qs and Fcs" - Optimizes gain, Q, and center frequency
    * "Generate predicted measurement" - Creates predicted response measurement
    * "Generate filters measurement" - Creates measurement with only filters applied
    * "Generate target measurement" - Creates target curve measurement

---

### 4. EQ Progress Subscription (1 endpoint)

#### 4.1 Subscribe to EQ Progress
- **Endpoint**: `POST /eq/subscribe`
- **Body**: `{"commandId": 1}`
- **Response**: WebSocket/SignalR connection for progress updates
- **Domain Object**: Progress subscription
- **Notes**: For tracking long-running EQ operations (Match target, Optimize, etc.)

---

## Implementation Priority

### Phase 3a: Core EQ Settings (HIGH PRIORITY)
1. ✅ GetMeasurementEqualiser (tested)
2. ✅ GetMeasurementFilters (tested)
3. UpdateMeasurementEqualiser
4. UpdateMeasurementFilter (single)
5. UpdateMeasurementFilters (batch)
6. GetTargetSettings
7. UpdateTargetSettings
8. GetTargetLevel
9. UpdateTargetLevel

### Phase 3b: EQ Commands (HIGH PRIORITY)
1. GetEQCommands (simple list)
2. ExecuteEQCommand (complex - handles 8 commands)
3. (Optional) SubscribeToEQProgress (advanced feature)

### Phase 3c: Global Defaults (MEDIUM PRIORITY)
1. GetAvailableEqualisers
2. GetManufacturers
3. GetDefaultEqualiser
4. SetDefaultEqualiser
5. GetDefaultTargetSettings
6. UpdateDefaultTargetSettings
7. GetDefaultTargetLevel
8. SetDefaultTargetLevel

### Phase 3d: Room Curves (MEDIUM PRIORITY)
1. GetRoomCurveSettings
2. UpdateRoomCurveSettings
3. GetDefaultRoomCurveSettings
4. UpdateDefaultRoomCurveSettings
5. GetHouseCurve
6. SetHouseCurve
7. ClearHouseCurve

### Phase 3e: Advanced Settings (LOW PRIORITY)
1. GetMatchTargetSettings
2. UpdateMatchTargetSettings

### Phase 3f: EQ Response Data (LOW PRIORITY - Complex)
1. GetTargetResponse (gzip-compressed)
2. GetEQFrequencyResponse (gzip-compressed)
3. GetEQGroupDelay (gzip-compressed)
4. GetEQImpulseResponse (gzip-compressed)
- **Note**: These endpoints return GZIP-compressed data requiring decompression logic
- **Challenge**: PowerShell `Invoke-RestMethod` auto-decompression failed (German error: "Magic Number im GZip-Header ist nicht richtig")
- **Solution**: May need custom HTTP client with proper Accept-Encoding headers

---

## Domain Objects Needed

### Existing (ValueObjects.cs)
- ✅ `EQSettings` (line 512) - Legacy, may need updating

### New Domain Objects Required
1. `EqualiserInfo` - manufacturer, model
2. `FilterSetting` - index, type, enabled, isAuto, fc, gain, Q
3. `TargetSettings` - shape, slopes, cutoffs, crossover types
4. `RoomCurveSettings` - add flag, rise/fall parameters
5. `MatchTargetSettings` - frequency range, boost limits, filter options
6. `EQCommandResult` - message, id, status

---

## API Response Patterns

### Standard List Response
```json
{
  "value": [...],
  "Count": n
}
```

### Simple Value Response
```json
75.0
```
or
```json
"string value"
```

### Complex Object Response
```json
{
  "property1": value,
  "property2": value
}
```

### Compressed Data Response
- Content-Type: application/gzip or similar
- Requires decompression before parsing
- Used for large data arrays (frequency response, impulse response)

---

## Testing Strategy

### Unit Tests (C#)
- Mock HttpClient responses
- Test domain object serialization/deserialization
- Test parameter validation
- Target: ~25-30 tests

### Integration Tests
- Requires running REW instance
- Test with actual measurement ID
- Test EQ command execution
- Test filter updates
- Target: ~10-15 tests

### Test Data Requirements
- Valid measurement ID with data
- Multiple equaliser profiles to test
- Filter configurations to apply
- Target settings variations

---

## Known Limitations

1. **Gzip Compression**: Target response and EQ predicted responses return compressed data
   - PowerShell testing encountered decompression errors
   - May need custom HTTP client configuration
   - Accept-Encoding header handling required

2. **Long-Running Operations**: EQ commands like "Match target" are asynchronous
   - Return command ID immediately
   - Require progress monitoring via subscription
   - Need timeout and error handling

3. **Filter Count Variability**: Number of filters depends on selected equaliser
   - "Generic" has 22 filters
   - Other equalisers may have different counts
   - Need dynamic handling

4. **Partial Updates**: Some endpoints support PUT (partial) vs POST (full replace)
   - TargetSettings: PUT for partial, POST for full
   - RoomCurveSettings: PUT for partial, POST for full
   - Need to implement both patterns

---

## Next Steps

1. ✅ Complete API exploration (DONE)
2. Design domain object structure
3. Implement Phase 3a (Core EQ Settings) - 9 methods
4. Implement Phase 3b (EQ Commands) - 2 methods
5. Write tests for implemented methods
6. Implement Phase 3c-3f based on priority
7. Handle gzip decompression for response data endpoints
8. Document EQ workflows (match target, optimize filters)

---

## References

- **API Documentation**: https://www.roomeqwizard.com/help/help_en-GB/html/api.html
- **REW API Base**: http://localhost:4735/
- **Architecture**: ADR-004 (Domain Service Pattern)
- **Standards**: ISO/IEC/IEEE 12207:2017 (Implementation Process)
