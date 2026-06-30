# Room EQ Wizard API Client Library

Status: experimental / partial.

This repository contains partial client implementations for selected REW API endpoints,
primarily in C#, with additional C++, JavaScript, and TypeScript implementations at
different maturity levels.

It is not yet a complete implementation of the REW API.

## 📋 Documentation

- **[CHANGELOG](CHANGELOG.md)** - Detailed history of fixes, features, and improvements
- **[API Coverage Report](docs/API-COVERAGE.md)** - Test results and implementation status
- **[REW API Documentation](https://www.roomeqwizard.com/help/help_en-GB/html/api.html)** - Official REW API reference
- **[Object Model](03-architecture/views/object-model.md)** - System architecture and design

## ✅ Current Status

**Last Updated**: 2025-10-24

### Test Results Summary

**Total**: 468 tests | **Passing**: 414 (88.5%) | **Failed**: 30 (6.4%) | **Skipped**: 24 (5.1%)  
**Duration**: 1595.6 seconds | **Build Status**: ⚠️ 30 errors, 916 warnings

| Component | Status | Test Coverage | Notes |
|-----------|--------|---------------|-------|
| **Import Service** | ✅ Operational | High coverage | Core functionality working |
| **Measurements Service** | ✅ Operational | High coverage | Key operations tested |
| **Measure Service** | ✅ Operational | High coverage | Level operations working |
| **Filter Service** | ✅ Operational | Base64 encoding fixed | Data transfer working |
| **Room Simulator Service** | ✅ Operational | Object instantiation fixed | API calls functional |
| **RTA Service** | ⚠️ Partial | Some failures | See known issues |
| **Additional Services** | ⚠️ Mixed | Various coverage | Work in progress |

**Overall Test Pass Rate**: 88.5% across all implemented services

### Recent Fixes (2024-12-19)

- ✅ **Import Service**: Fixed endpoint URLs for file-based vs. data-based imports
- ✅ **Measurements Service**: Implemented smoothing and time offset operations
- ✅ **Measure Service**: Implemented measurement level get/set operations
- ✅ **Filter Service**: Fixed Base64 encoding/decoding for filter arrays
- ✅ **Room Simulator Service**: Fixed object instantiation issues

See [CHANGELOG.md](CHANGELOG.md) for detailed fix information.

### Skipped Tests (24 tests - 5.1%)

The test suite includes 24 intentionally skipped tests for various reasons:
- **API Limitations** (8 tests): Functionality not available via REW API
- **Manual Tests** (5 tests): Require human intervention (audio playback, long-running)
- **Implementation Pending** (2 tests): Features not yet implemented
- **Known API Issues** (4 tests): REW API validation errors under investigation
- **Educational Tests** (5 tests): Demonstrate wrong patterns or deprecated endpoints

To explore and run skipped tests:
```powershell
# List all skipped test categories
.\scripts\Run-SkippedTests-Simple.ps1 -Category List

# Attempt to run safe skipped tests
.\scripts\Run-SkippedTests-Simple.ps1 -Category Safe
```

See [SKIPPED-TESTS-ANALYSIS.md](docs/SKIPPED-TESTS-ANALYSIS.md) for detailed analysis.

## Setting Up and Using the REW API

1. Start the REW API server by running REW with the -api argument.
2. Access the API documentation via swagger-ui at localhost:4735.
3. Interact with the API endpoints using GET, PUT, and POST methods.

Note: When running REW without a GUI, you must use the [application](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#application) shutdown command to close REW when done.

### Starting the API Server on Windows

To start the API server on Windows, use the following command:

```sh
C:\Program Files\REW\roomeqwizard.exe -api
```

### Starting the API Server on macOS

To start the API server on macOS, use the following command:

```sh
open -a REW.app --args -api
```

### Specifying a Different Port

To specify a different port, add the `-port` argument followed by the desired port number. For example, to use port 4567, use the following command:

```sh
C:\Program Files\REW\roomeqwizard.exe -api -port 4567
```

Note: Port numbers below 1024 will be ignored.

### Running REW Without a GUI

To run REW without a GUI, use the `-nogui` argument. For example:

```sh
C:\Program Files\REW\roomeqwizard.exe -api -nogui
```

When running REW without a GUI, you must use the [application](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#application) shutdown command to close REW when done.

## Inhibiting Graph Updates

A flag can be set to inhibit any graph updates. This may be useful when running actions which delete or modify measurements to prevent graphs being updated with partial data or errors caused by trying to plot data which is no longer available.

### C#

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class REWApi
{
    private static readonly HttpClient client = new HttpClient();
    private bool graphUpdateInhibited = false;

    public void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    public async Task<string> GetAsync(string endpoint)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PutAsync(string endpoint, HttpContent content)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostAsync(string endpoint, HttpContent content)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

### C++

```cpp
#include <iostream>
#include <cpprest/http_client.h>
#include <cpprest/filestream.h>

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace concurrency::streams;

class REWApi
{
private:
    bool graphUpdateInhibited = false;

public:
    void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const json::value& content)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const json::value& content)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::POST, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }
};
```

### JavaScript

```javascript
const fetch = require('node-fetch');

class REWApi {
    constructor() {
        this.graphUpdateInhibited = false;
    }

    setGraphUpdateInhibition(inhibit) {
        this.graphUpdateInhibited = inhibit;
    }

    async get(endpoint) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint, data) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async post(endpoint, data) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }
}
```

### TypeScript

```typescript
import fetch from 'node-fetch';

class REWApi {
    private graphUpdateInhibited: boolean = false;

    setGraphUpdateInhibition(inhibit: boolean) {
        this.graphUpdateInhibited = inhibit;
    }

    async get(endpoint: string): Promise<any> {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint: string, data: any): Promise<any> {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async post(endpoint: string, data: any): Promise<any> {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }
}
```

## Base64 Encoding and Decoding of Arrays

Arrays are transferred as Base64-encoded strings generated from the raw bytes of the 32-bit float sample values. Base64 decoding is well supported in programming languages, here is how to recover the arrays in Java:

```java
public static float[] decodeArray(String base64Encoded) {
    return byteToFloatArray(Base64.getDecoder().decode(base64Encoded));
}

private static float[] byteToFloatArray(byte[] bytes) {
    FloatBuffer buf = ByteBuffer.wrap(bytes).asFloatBuffer();
    float[] floatArray = new float[buf.limit()];
    buf.get(floatArray);
    return floatArray;
}
```

### C#

```csharp
public string EncodeArray(float[] array)
{
    byte[] bytes = new byte[array.Length * sizeof(float)];
    Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
    return Convert.ToBase64String(bytes);
}

public float[] DecodeArray(string base64Encoded)
{
    byte[] bytes = Convert.FromBase64String(base64Encoded);
    float[] array = new float[bytes.Length / sizeof(float)];
    Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
    return array;
}
```

### JavaScript

```javascript
encodeArray(array) {
    const buffer = new ArrayBuffer(array.length * 4);
    const view = new DataView(buffer);
    for (let i = 0; i < array.length; i++) {
        view.setFloat32(i * 4, array[i], true);
    }
    return Buffer.from(buffer).toString('base64');
}

decodeArray(base64Encoded) {
    const buffer = Buffer.from(base64Encoded, 'base64');
    const view = new DataView(buffer.buffer);
    const array = new Float32Array(buffer.length / 4);
    for (let i = 0; i < array.length; i++) {
        array[i] = view.getFloat32(i * 4, true);
    }
    return array;
}
```

### TypeScript

```typescript
encodeArray(array: Float32Array): string {
    const buffer = new ArrayBuffer(array.length * 4);
    const view = new DataView(buffer);
    for (let i = 0; i < array.length; i++) {
        view.setFloat32(i * 4, array[i], true);
    }
    return Buffer.from(buffer).toString('base64');
}

decodeArray(base64Encoded: string): Float32Array {
    const buffer = Buffer.from(base64Encoded, 'base64');
    const view = new DataView(buffer.buffer);
    const array = new Float32Array(buffer.length / 4);
    for (let i = 0; array.length; i++) {
        array[i] = view.getFloat32(i * 4, true);
    }
    return array;
}
```

## Subscription Endpoints

The REW API now supports subscription endpoints to allow clients to be notified of changes. The following endpoints are available:

### Subscribe

To add a subscription, make a POST request to the `/subscribe` endpoint with a JSON body containing the subscriber's URL.

Example:

```json
{
  "url": "http://127.0.0.1:5374/mysubscriptions"
}
```

### Unsubscribe

To remove a subscription, make a POST request to the `/unsubscribe` endpoint with the same JSON body containing the subscriber's URL.

Example:

```json
{
  "url": "http://127.0.0.1:5374/mysubscriptions"
}
```

### Get Subscribers

To get the list of subscribers, make a GET request to the `/subscribers` endpoint.

Example:

```json
[
  {
    "url": "http://127.0.0.1:5374/mysubscriptions"
  }
]
```

## Application Endpoint

The `/application` endpoint provides application-level control of REW. The following sub-endpoints are available:

### Commands

To list available commands, make a GET request to the `/application/commands` endpoint.

Example:

```json
[
  "shutdown",
  "clearCommandInProgress"
]
```

To issue a command, make a POST request to the `/application/command` endpoint with a JSON body containing the command.

Example:

```json
{
  "command": "shutdown"
}
```

### Errors

To get the list of errors, make a GET request to the `/application/errors` endpoint.

Example:

```json
[
  {
    "timestamp": "2023-01-01T12:00:00Z",
    "message": "An error occurred."
  }
]
```

To get the most recent error, make a GET request to the `/application/last-error` endpoint.

Example:

```json
{
  "timestamp": "2023-01-01T12:00:00Z",
  "message": "An error occurred."
}
```

### Warnings

To get the list of warnings, make a GET request to the `/application/warnings` endpoint.

Example:

```json
[
  {
    "timestamp": "2023-01-01T12:00:00Z",
    "message": "A warning occurred."
  }
]
```

To get the most recent warning, make a GET request to the `/application/last-warning` endpoint.

Example:

```json
{
  "timestamp": "2023-01-01T12:00:00Z",
  "message": "A warning occurred."
}
```

### Blocking Mode

To enable or disable blocking mode, make a POST request to the `/application/blocking` endpoint with a JSON body containing a boolean value.

Example:

```json
{
  "blocking": true
}
```

### Inhibit Graph Updates

To inhibit graph updates, make a POST request to the `/application/inhibit-graph-updates` endpoint with a JSON body containing a boolean value.

Example:

```json
{
  "inhibit": true
}
```

## Audio Endpoint

The `/audio` endpoint provides full control of REW's audio input and output selections and associated controls. The following sub-endpoints are available:

### Driver

To get the current audio driver, make a GET request to the `/audio/driver` endpoint.

Example:

```json
{
  "driver": "Java"
}
```

To set the audio driver, make a POST request to the `/audio/driver` endpoint with a JSON body containing the driver.

Example:

```json
{
  "driver": "ASIO"
}
```

### Driver Types

To get the available audio driver types, make a GET request to the `/audio/driver-types` endpoint.

Example:

```json
[
  "Java",
  "ASIO"
]
```

### Sample Rate

To get the current sample rate, make a GET request to the `/audio/samplerate` endpoint.

Example:

```json
{
  "samplerate": 44100
}
```

To set the sample rate, make a POST request to the `/audio/samplerate` endpoint with a JSON body containing the sample rate.

Example:

```json
{
  "samplerate": 48000
}
```

### Sample Rates

To get the available sample rates for the current interface selection, make a GET request to the `/audio/samplerates` endpoint.

Example:

```json
[
  44100,
  48000,
  96000
]
```

### Configuration

To get the global option to treat 32-bit data as 24-bit, make a GET request to the `/audio/configuration` endpoint.

Example:

```json
{
  "treat32as24": true
}
```

To set the global option to treat 32-bit data as 24-bit, make a POST request to the `/audio/configuration` endpoint with a JSON body containing the option.

Example:

```json
{
  "treat32as24": false
}
```

### Input Calibration

To get the current input calibration configuration, make a GET request to the `/audio/input-cal` endpoint.

Example:

```json
{
  "inputCalConfiguration": {
    "inputSelection": "Mic",
    "calFilePath": "C:\\path\\to\\calfile.cal"
  }
}
```

To update the input calibration configuration, make a PUT request to the `/audio/input-cal` endpoint with a JSON body containing the configuration.

Example:

```json
{
  "inputCalConfiguration": {
    "inputSelection": "Mic",
    "calFilePath": "C:\\path\\to\\newcalfile.cal"
  }
}
```

### Output Calibration

To get the current output calibration configuration, make a GET request to the `/audio/output-cal` endpoint.

Example:

```json
{
  "outputCalConfiguration": {
    "outputSelection": "Soundcard",
    "calFilePath": "C:\\path\\to\\calfile.cal"
  }
}
```

To update the output calibration configuration, make a PUT request to the `/audio/output-cal` endpoint with a JSON body containing the configuration.

Example:

```json
{
  "outputCalConfiguration": {
    "outputSelection": "Soundcard",
    "calFilePath": "C:\\path\\to\\newcalfile.cal"
  }
}
```

## Java Driver Endpoints

The `/audio/java/` endpoints provide control over Java driver items. The following sub-endpoints are available:

### Input Device

To get the current input device, make a GET request to the `/audio/java/input-device` endpoint.

Example:

```json
{
  "inputDevice": "Microphone"
}
```

To set the input device, make a POST request to the `/audio/java/input-device` endpoint with a JSON body containing the input device.

Example:

```json
{
  "inputDevice": "Line In"
}
```

### Input

To get the current input, make a GET request to the `/audio/java/input` endpoint.

Example:

```json
{
  "input": "Mic"
}
```

To set the input, make a POST request to the `/audio/java/input` endpoint with a JSON body containing the input.

Example:

```json
{
  "input": "Line In"
}
```

### Output Device

To get the current output device, make a GET request to the `/audio/java/output-device` endpoint.

Example:

```json
{
  "outputDevice": "Speakers"
}
```

To set the output device, make a POST request to the `/audio/java/output-device` endpoint with a JSON body containing the output device.

Example:

```json
{
  "outputDevice": "Headphones"
}
```

### Output

To get the current output, make a GET request to the `/audio/java/output` endpoint.

Example:

```json
{
  "output": "Speakers"
}
```

To set the output, make a POST request to the `/audio/java/output` endpoint with a JSON body containing the output.

Example:

```json
{
  "output": "Headphones"
}
```

### Input Channel

To get the current input channel, make a GET request to the `/audio/java/input-channel` endpoint.

Example:

```json
{
  "inputChannel": 1
}
```

To set the input channel, make a POST request to the `/audio/java/input-channel` endpoint with a JSON body containing the input channel.

Example:

```json
{
  "inputChannel": 2
}
```

### Output Channel

To get the current output channel, make a GET request to the `/audio/java/output-channel` endpoint.

Example:

```json
{
  "outputChannel": "L+R"
}
```

To set the output channel, make a POST request to the `/audio/java/output-channel` endpoint with a JSON body containing the output channel.

Example:

```json
{
  "outputChannel": "L"
}
```

### Reference Input Channel

To get the current reference input channel, make a GET request to the `/audio/java/ref-input-channel` endpoint.

Example:

```json
{
  "refInputChannel": 1
}
```

To set the reference input channel, make a POST request to the `/audio/java/ref-input-channel` endpoint with a JSON body containing the reference input channel.

Example:

```json
{
  "refInputChannel": 2
}
```

### Last Input Channel

To get the current last input channel, make a GET request to the `/audio/java/last-input-channel` endpoint.

Example:

```json
{
  "lastInputChannel": 1
}
```

To set the last input channel, make a POST request to the `/audio/java/last-input-channel` endpoint with a JSON body containing the last input channel.

Example:

```json
{
  "lastInputChannel": 2
}
```

### Reference Output Channel

To get the current reference output channel, make a GET request to the `/audio/java/ref-output-channel` endpoint.

Example:

```json
{
  "refOutputChannel": "L+R"
}
```

To set the reference output channel, make a POST request to the `/audio/java/ref-output-channel` endpoint with a JSON body containing the reference output channel.

Example:

```json
{
  "refOutputChannel": "L"
}
```

### Number of Input Channels

To get the number of input channels, make a GET request to the `/audio/java/num-input-channels` endpoint.

Example:

```json
{
  "numInputChannels": 2
}
```

### Output Channels

To get the available output channels, make a GET request to the `/audio/java/output-channels` endpoint.

Example:

```json
[
  "L",
  "R",
  "L+R"
]
```

### Reference Output Channels

To get the available reference output channels, make a GET request to the `/audio/java/ref-output-channels` endpoint.

Example:

```json
[
  "L",
  "R",
  "L+R"
]
```

### Output Channel Mapping

To get the current output channel mapping, make a GET request to the `/audio/java/output-channel-mapping` endpoint.

Example:

```json
[
  "L",
  "R",
  "L+R"
]
```

To set the output channel mapping, make a POST request to the `/audio/java/output-channel-mapping` endpoint with a JSON body containing the output channel mapping.

Example:

```json
[
  "L",
  "R"
]
```

### Stereo Only

To get the stereo only flag, make a GET request to the `/audio/java/stereo-only` endpoint.

Example:

```json
{
  "stereoOnly": true
}
```

To set the stereo only flag, make a POST request to the `/audio/java/stereo-only` endpoint with a JSON body containing the stereo only flag.

Example:

```json
{
  "stereoOnly": false
}
```

## ASIO Driver Endpoints

The `/audio/asio/` endpoints provide control over ASIO driver items. The following sub-endpoints are available:

### Device

To get the current ASIO device, make a GET request to the `/audio/asio/device` endpoint.

Example:

```json
{
  "device": "ASIO4ALL"
}
```

To set the ASIO device, make a POST request to the `/audio/asio/device` endpoint with a JSON body containing the device.

Example:

```json
{
  "device": "Focusrite USB ASIO"
}
```

### Input

To get the current ASIO input, make a GET request to the `/audio/asio/input` endpoint.

Example:

```json
{
  "input": "Mic 1"
}
```

To set the ASIO input, make a POST request to the `/audio/asio/input` endpoint with a JSON body containing the input.

Example:

```json
{
  "input": "Mic 2"
}
```

### Output

To get the current ASIO output, make a GET request to the `/audio/asio/output` endpoint.

Example:

```json
{
  "output": "Speakers"
}
```

To set the ASIO output, make a POST request to the `/audio/asio/output` endpoint with a JSON body containing the output.

Example:

```json
{
  "output": "Headphones"
}
```

### Devices

To get the list of available ASIO devices, make a GET request to the `/audio/asio/devices` endpoint.

Example:

```json
[
  "ASIO4ALL",
  "Focusrite USB ASIO"
]
```

### Inputs

To get the list of available ASIO inputs, make a GET request to the `/audio/asio/inputs` endpoint.

Example:

```json
[
  "Mic 1",
  "Mic 2"
]
```

### Outputs

To get the list of available ASIO outputs, make a GET request to the `/audio/asio/outputs` endpoint.

Example:

```json
[
  "Speakers",
  "Headphones"
]
```

### Timing Reference Output Channel

To get the current timing reference output channel, make a GET request to the `/audio/asio/timing-reference-output-channel` endpoint.

Example:

```json
{
  "timingReferenceOutputChannel": "L"
}
```

To set the timing reference output channel, make a POST request to the `/audio/asio/timing-reference-output-channel` endpoint with a JSON body containing the timing reference output channel.

Example:

```json
{
  "timingReferenceOutputChannel": "R"
}
```

### Reference Input Channel

To get the current reference input channel, make a GET request to the `/audio/asio/ref-input-channel` endpoint.

Example:

```json
{
  "refInputChannel": "Mic 1"
}
```

To set the reference input channel, make a POST request to the `/audio/asio/ref-input-channel` endpoint with a JSON body containing the reference input channel.

Example:

```json
{
  "refInputChannel": "Mic 2"
}
```

### Last Input Channel

To get the current last input channel, make a GET request to the `/audio/asio/last-input-channel` endpoint.

Example:

```json
{
  "lastInputChannel": "Mic 1"
}
```

To set the last input channel, make a POST request to the `/audio/asio/last-input-channel` endpoint with a JSON body containing the last input channel.

Example:

```json
{
  "lastInputChannel": "Mic 2"
}
```

### Secondary Output Enable

To enable or disable the secondary ASIO output, make a POST request to the `/audio/asio/secondary-output-enable` endpoint with a JSON body containing a boolean value.

Example:

```json
{
  "secondaryOutputEnable": true
}
```

### Secondary Output

To get the current secondary ASIO output, make a GET request to the `/audio/asio/secondary-output` endpoint.

Example:

```json
{
  "secondaryOutput": "Speakers"
}
```

To set the secondary ASIO output, make a POST request to the `/audio/asio/secondary-output` endpoint with a JSON body containing the secondary output.

Example:

```json
{
  "secondaryOutput": "Headphones"
}
```

### Command

To issue a command to the ASIO driver, make a POST request to the `/audio/asio/command` endpoint with a JSON body containing the command.

Example:

```json
{
  "command": "reload"
}
```

## Input Levels Endpoint

The `/input-levels` endpoint provides access to the current input level via subscription whenever the inputs are being monitored. Monitoring is started or stopped by posting a command to the `/input-levels/command` endpoint. The list of commands is retrieved from the `/input-levels/commands` endpoint. Sending a command to start monitoring input levels will start audio capture if it is not already running. Audio capture will continue until a command is sent to stop monitoring input levels.

The last input levels update can be read from the `/input-levels/last-levels` endpoint. That will return the last InputLevels object generated by an input levels update. More commonly levels would be monitored through a subscription.

A subscription can be added by making a POST to the `/input-levels/subscribe` endpoint. The body contains a Subscriber object which has a URL string and an optional "unit" parameter that specifies the units of the input level values. The accepted units can be read from the `/input-levels/units` endpoint. If the unit is omitted levels are returned as dBFS. To have input level updates posted to a `/input-levels` endpoint a subscription could be:

```json
{
  "url": "http://127.0.0.1:5374/input-levels",
  "parameters": {"unit": "dBV"}
}
```

Levels are returned as an InputLevels object which has the unit, arrays of the rms and peak levels for each input since the last update and a timeSpanSeconds value with the time period over which the values were calculated.

### Starting and Stopping Monitoring Input Levels

To start monitoring input levels, make a POST request to the `/input-levels/command` endpoint with a JSON body containing the command.

Example:

```json
{
  "command": "start"
}
```

To stop monitoring input levels, make a POST request to the `/input-levels/command` endpoint with a JSON body containing the command.

Example:

```json
{
  "command": "stop"
}
```

### Subscribing and Unsubscribing to Input Levels

To subscribe to input levels, make a POST request to the `/input-levels/subscribe` endpoint with a JSON body containing the subscriber's URL and optional parameters.

Example:

```json
{
  "url": "http://127.0.0.1:5374/input-levels",
  "parameters": {"unit": "dBV"}
}
```

To unsubscribe from input levels, make a POST request to the `/input-levels/unsubscribe` endpoint with the same JSON body containing the subscriber's URL and optional parameters.

Example:

```json
{
  "url": "http://127.0.0.1:5374/input-levels",
  "parameters": {"unit": "dBV"}
}
```

### Retrieving the Last Input Levels Update

To retrieve the last input levels update, make a GET request to the `/input-levels/last-levels` endpoint.

Example:

```json
{
  "unit": "dBV",
  "rmsLevels": [0.1, 0.2, 0.3],
  "peakLevels": [0.4, 0.5, 0.6],
  "timeSpanSeconds": 1.0
}
```

### Retrieving Accepted Units for Input Levels

To retrieve the accepted units for input levels, make a GET request to the `/input-levels/units` endpoint.

Example:

```json
[
  "dBFS",
  "dBV",
  "dBu"
]
```

## Measurements Commands

A GET at the `/measurements/commands` endpoint returns a list of commands, to issue a command POST it to the `/measurements/command` endpoint. The "Save all" command has a parameter for the path to the file to be saved to, if the file exists it will be overwritten. A note to be saved with the measurement can optionally be specified as a second parameter. The "Load" command has a list of the filenames to load as parameters in the command. Note that if a file path has backslash as the path separator the string for the path will need to escape the backslash entries, i.e. use a double backslash instead or replace backslash by forward slash. Here are examples of loading two mdat files and of saving all files:

```json
{
  "command": "Load",
  "parameters": [
    "C:/Users/myusername/REW/file1.mdat",
    "C:/Users/myusername/REW/file2.mdat"
  ]
}

{
  "command": "Save all",
  "parameters": [
    "C:/Users/myusername/REW/latest.mdat",
    "These are my latest files"
  ]
}
```

## Frequency Response Endpoint

The frequency response for a measurement can be obtained by a GET request at `/measurements/:id/frequency-response`. A `FrequencyResponse` object is returned, with the following properties:

- `smoothing`: The smoothing setting.
- `startFrequency`: The start frequency.
- `pointsPerOctave`: The points-per-octave figure (for log-spaced data) or a frequency step value (for linear-spaced data).
- `magnitudes`: Base64-encoded string of response magnitudes.
- `phases`: Base64-encoded string of response phases.

The default unit for the magnitude data is SPL, but other units can be requested by including a `unit` query value in the URL, e.g., `?unit=dBFS`. The list of units available can be read from `/measurements/frequency-response/units`.

The default smoothing is whatever the measurement currently uses, but other smoothings can be requested by including a `smoothing` query value in the URL, e.g., `?smoothing="1/12"`. The list of smoothing choices can be read from `/measurements/frequency-response/smoothing-choices`.

Whether the returned data is log-spaced or linear-spaced depends on the measurement, but log-spaced data can be forced by including a `ppo` query value in the URL, e.g., `?ppo=96`. Note that to avoid sampling artifacts, log-spaced data will be smoothed to `ppo/2` if a greater smoothing has not already been applied.

## Impulse Response Endpoint

The impulse response for a measurement can be obtained by a GET request at `/measurements/:id/impulse-response`. An `ImpulseResponse` object is returned, with the following properties:

- `startTime`: The start time.
- `timingReference`: A description of the timing reference used.
- `sampleInterval`: The sample interval in seconds.
- `sampleRate`: The sample rate in Hz.
- `responseData`: Base64-encoded string of response data.

The default unit for the data is Percent, but other units can be requested by including a `unit` query value in the URL, e.g., `?unit=dBFS`. The list of units available can be read from `/measurements/impulse-response/units`.

Impulse response queries can return the windowed portion of the data by including a query value `?windowed=true` in the URL. Data is normalised by default; for data that is not normalised, include a query value `?normalised=false` in the URL.

## Impulse Response Window Settings Endpoint

The impulse response window settings for a measurement can be accessed and updated using the `/measurements/:id/ir-windows` endpoint. The settings are in an `IRWindows` object, which includes the following properties:

- `leftWindowType`: The type of the left window.
- `rightWindowType`: The type of the right window.
- `leftWindowWidthms`: The width of the left window in milliseconds.
- `rightWindowWidthms`: The width of the right window in milliseconds.
- `refTimems`: The reference time in milliseconds.
- `addFDW`: A boolean indicating whether to add FDW.
- `fdwWidthCycles`: The width of the FDW in cycles.

The FDW width can be in cycles or octaves according to the current selection in the REW View preferences. Either can be used when changing window settings by PUT or POST.

Example `IRWindows` object:

```json
{
  "leftWindowType": "Tukey 0.25",
  "rightWindowType": "Tukey 0.25",
  "leftWindowWidthms": 125,
  "rightWindowWidthms": 500,
  "refTimems": 0,
  "addFDW": true,
  "fdwWidthCycles": 15
}
```

To access the IR window settings, you can use the `/measurements/:id/ir-windows` endpoint. Here are the steps:

1. Make a GET request to the `/measurements/:id/ir-windows` endpoint to retrieve the current IR window settings for a specific measurement.
2. The response will be an `IRWindows` object containing the settings such as `leftWindowType`, `rightWindowType`, `leftWindowWidthms`, `rightWindowWidthms`, `refTimems`, `addFDW`, and `fdwWidthCycles`.
3. To update the IR window settings, make a PUT or POST request to the same endpoint with the updated `IRWindows` object in the request body.

For more details, you can refer to the API documentation available via swagger-ui at `localhost:4735`.

## Determining the Current Selection in the REW View Preferences

To determine the current selection in the REW View preferences, you can follow these steps:

1. Make a GET request to the `/application/settings` endpoint to retrieve the current application settings.
2. The response will include the current selection in the REW View preferences, which will help you understand the current configuration.

For more details, you can refer to the API documentation available via swagger-ui at `localhost:4735`.

## Distortion Data Endpoint

The distortion data for a measurement can be obtained by a GET request at `/measurements/:id/distortion`. A `Distortion` object is returned, with the following properties:

- `type`: A string describing the type of data.
- `columnHeaders`: An array of column header strings.
- `data`: A 2D array of Doubles with the data.

The default unit for the data is percent, but other units can be requested by including a `unit` query value in the URL, e.g., `?unit=dBr`. The list of units available can be read from `/measurements/distortion-units`.

Sweep distortion data is returned at the PPO value included in the URL, e.g., `?ppo=12`, or at 3 PPO if not specified. PPO is not required for other types of distortion data. The list of PPO choices can be read from `/measurements/distortion-ppo-choices`.

The 2D array of Doubles in the `Distortion` object should be interpreted as follows:

* Each row in the 2D array represents a different measurement or data point.
* Each column in the 2D array represents a different attribute or parameter of the measurement.
* The array is structured such that `data[row][column]` gives the value of the attribute at the specified row and column.
* The column headers array provides the names or descriptions of each attribute, corresponding to the columns in the 2D array.
* The data is returned in the default unit of percent, but other units can be requested by including a unit query value in the URL, e.g., `?unit=dBr`.

## Generate Spectrogram Command

The `Generate spectrogram` command allows you to generate a spectrogram with various parameters and modes. The command returns the 2D data in a `ProcessResult`. Here are some examples of commands to generate a spectrogram:

### Example 1: Fourier Mode

```json
{
  "command": "Generate spectrogram",
  "parameters": {
    "mode": "Fourier",
    "slices": "551",
    "amplitude": "Log (dB SPL)",
    "window type": "Gaussian",
    "window width ms": "300",
    "before ms": "50",
    "after ms": "500",
    "ppo": "48"
  },
  "resultUrl": "http://127.0.0.1:5374/spectrograms"
}
```

### Example 2: Morlet CWT Mode

```json
{
  "command": "Generate spectrogram",
  "parameters": {
    "mode": "Morlet CWT",
    "slices": "551",
    "amplitude": "Linear (% peak)",
    "before ms": "5",
    "after ms": "50",
    "bandwidth": "1/3",
    "ppo": "48"
  },
  "resultUrl": "http://127.0.0.1:5374/spectrograms"
}
```

The `Generate spectrogram` command supports the following parameters:

- `mode`: The mode of the spectrogram. Possible values are "Fourier", "Wavelet", "Airy CWT", "Morlet CWT", and "Burst decay".
- `slices`: The number of slices to generate.
- `ppo`: The frequency resolution of the result.
- `amplitude`: The data values for the amplitude.
- Additional parameters depending on the mode:
  - Fourier: `window type`, `window width ms`, `before ms`, `after ms`.
  - Burst decay: `bandwidth`, `periods`.
  - Wavelet modes: `bandwidth`, `before ms`, `after ms`.

The `Generate spectrogram` command returns the 2D data in a `ProcessResult`. The result includes an array of the frequencies, an array of the times (or periods for Burst Decay), and a set of arrays of SPL data vs frequency for each time index. It is best to specify a `resultUrl` for those as the generation can take some time. Note that the commands do not affect the REW GUI and will not produce a result in the GUI; they are provided as a way to generate and extract data.

## Process Measurements Endpoint

The `/measurements/process-measurements` endpoint allows you to process a set of measurements by POSTing a `ProcessMeasurements` object. The `ProcessMeasurements` object has the following properties:

- `measurements`: A list of either the numeric indices or UUID strings of the measurements to be processed.
- `url`: An optional URL to which the result of the process will be posted.

The process result can be read from the `/measurements/process-result` endpoint but may not be available immediately, as some processes take time to complete. The `ProcessMeasurementsResult` object has the following properties:

- `processName`: The name of the process it relates to.
- `message`: A message (which may be absent or empty).
- `results`: A set of results for each measurement that was processed, returned as a map of result names and result values (all strings).

### Align SPL Process

The Align SPL process has the following parameters:

- `targetdB`: Either "average" to align to the average SPL of the measurements or an SPL value in dB, e.g., "75.0".
- `frequencyHz`: The center frequency for the alignment in Hz, e.g., "1000".
- `spanOctaves`: The alignment span in octaves, e.g., "2".

## Smooth Command

The `Smooth` command allows you to apply smoothing to a measurement. The command supports an optional `smoothing` parameter with a default value of `1/3 Octave Smoothing`. Here is an example of the `Smooth` command:

### Example

```json
{
  "command": "Smooth",
  "parameters": {
    "smoothing": "1/3"
  }
}
```

The `smoothing` parameter is optional, and if not provided, the default value of `1/3 Octave Smoothing` will be used. This provides flexibility for users who may not need to specify the smoothing parameter, while ensuring consistent results even if the parameter is not provided.

## Groups Endpoint

The `/groups` endpoint provides access to any groups REW has generated or loaded. Groups are referenced by their UUID, as group names may be changed by the user. The UUID for a group is available from the `GroupInfo` object. If a measurement is in a group, the group name, notes, and ID will be included in the measurement's summary. The current list of measurements in a group can be obtained by a GET request at the `/groups/:uuid/measurements` endpoint. A measurement can be placed in a group by posting a `MeasurementSummary` containing the measurement's UUID to `/groups/:uuid/measurements` or by posting a `MeasurementSummary` with the required group UUID at the `/measurements/:id/` endpoint for the measurement.

### Creating a New Group

To create a new group using the API, you need to make a POST request to the `/groups` endpoint with a `GroupInfo` object. Here are the steps:

1. Ensure the `GroupInfo` object does not contain a UUID, as any UUID in the object will be ignored.
2. Make a POST request to the `/groups` endpoint with the `GroupInfo` object in the request body.
3. The response will include the new group's `GroupInfo` object, including its UUID.
4. If the name in the `GroupInfo` object already exists in a group, there will be a bad request response.

### Accessing Individual Groups

Individual groups may be accessed by including their UUID in the path. A group's name or notes may be changed by making a PUT request with a `GroupInfo` object containing the fields to change to the `/groups/:uuid` endpoint.

### Deleting Groups

To delete a group using the API, you need to make a DELETE request to the appropriate endpoint. Here are the steps:

1. To delete a specific group, make a DELETE request to the `/groups/:uuid` endpoint, where `:uuid` is the UUID of the group you want to delete.
2. To delete all groups, make a DELETE request to the `/groups` endpoint.
3. Note that there are no delete confirmations, so use these endpoints with care!

### Group Subscriptions

A subscription can be added to be notified of any changes that are made to the list of groups, including groups being added or deleted. There are no parameters for group subscriptions. The change notifications are conveyed in a `GroupsListChange` object, which has a string with a description of the type of change and the ID, name, and notes for the group changed.

For more details, you can refer to the API documentation available via swagger-ui at `localhost:4735`.

## Offset t=0 Command

The `Offset t=0` command allows you to offset the time (t) by a specified value and unit. The command supports the following parameters:

- `offset`: The offset value.
- `unit`: The unit for the offset value. Valid units are:
  - Seconds ⏱️
  - Metres 📏
  - Feet 👣
  - Samples 🎚️

Here is an example of a command to offset t=0:

```json
{
  "command": "Offset t=0",
  "parameters": {
    "offset": "0.5",
    "unit": "metres"
  },
  "resultUrl": "http://127.0.0.1:5374/result"
}
```

The `Offset t=0` command allows you to specify the offset value and unit to adjust the time accordingly. The valid units for the offset parameter are seconds, metres, feet, and samples. This command provides flexibility in adjusting the time based on different units of measurement.

## New Methods for Handling Global Option to Treat 32-bit Data as 24-bit

The following methods have been added to handle the global option to treat 32-bit data as 24-bit:

### C++

```cpp
class REWApi
{
public:
    pplx::task<utility::string_t> GetTreat32As24Async()
    {
        return GetAsync(U("http://localhost:4735/audio/configuration"));
    }

    pplx::task<utility::string_t> SetTreat32As24Async(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/configuration"), content);
    }
};
```

### C#

```csharp
public class REWApi
{
    public async Task<string> GetTreat32As24Async()
    {
        return await GetAsync("http://localhost:4735/audio/configuration");
    }

    public async Task<string> SetTreat32As24Async(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/configuration", content);
    }
}
```

### JavaScript

```javascript
class REWApi {
    async getTreat32As24() {
        return await this.get("http://localhost:4735/audio/configuration");
    }

    async setTreat32As24(data) {
        return await this.post("http://localhost:4735/audio/configuration", data);
    }
}
```

### TypeScript

```typescript
class REWApi {
    async getTreat32As24(): Promise<any> {
        return await this.get("http://localhost:4735/audio/configuration");
    }

    async setTreat32As24(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/configuration", data);
    }
}
```

## New Methods for Checking if Audio is Enabled and if Audio Endpoints are Ready for Use

The following methods have been added to check if audio is enabled and if audio endpoints are ready for use:

### C++

```cpp
class REWApi
{
public:
    pplx::task<utility::string_t> IsAudioEnabledAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/enabled"));
    }

    pplx::task<utility::string_t> AreAudioEndpointsReadyAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/endpoints-ready"));
    }
};
```

### C#

```csharp
public class REWApi
{
    public async Task<string> IsAudioEnabledAsync()
    {
        return await GetAsync("http://localhost:4735/audio/enabled");
    }

    public async Task<string> AreAudioEndpointsReadyAsync()
    {
        return await GetAsync("http://localhost:4735/audio/endpoints-ready");
    }
}
```

### JavaScript

```javascript
class REWApi {
    async isAudioEnabled() {
        return await this.get("http://localhost:4735/audio/enabled");
    }

    async areAudioEndpointsReady() {
        return await this.get("http://localhost:4735/audio/endpoints-ready");
    }
}
```

### TypeScript

```typescript
class REWApi {
    async isAudioEnabled(): Promise<any> {
        return await this.get("http://localhost:4735/audio/enabled");
    }

    async areAudioEndpointsReady(): Promise<any> {
        return await this.get("http://localhost:4735/audio/endpoints-ready");
    }
}
```

## EQ and Filter Endpoints

The REW API now supports reading and modifying a measurement's EQ and filter settings through the following endpoints:

### Measurement Equaliser

To read a measurement's equaliser selection, make a GET request to the `/measurements/:id/equaliser` endpoint.

Example:

```json
{
  "equaliser": "Equaliser 1"
}
```

To change a measurement's equaliser selection, make a POST request to the `/measurements/:id/equaliser` endpoint with a JSON body containing the new equaliser selection.

Example:

```json
{
  "equaliser": "Equaliser 2"
}
```

### Available Equalisers

To read the available equalisers, make a GET request to the `/eq/equalisers` endpoint.

Example:

```json
[
  "Equaliser 1",
  "Equaliser 2",
  "Equaliser 3"
]
```

### Measurement Filters

To read a measurement's filters, make a GET request to the `/measurements/:id/filters` endpoint. A list of `FilterSetting` objects is returned with the current settings for each filter of the measurement's equaliser.

Example:

```json
[
  {
    "filterId": "Filter 1",
    "type": "Low Pass",
    "frequency": 1000,
    "gain": -3,
    "q": 0.707
  },
  {
    "filterId": "Filter 2",
    "type": "High Pass",
    "frequency": 200,
    "gain": 0,
    "q": 1.0
  }
]
```

To change an individual filter's settings, make a PUT request to the `/measurements/:id/filters/:filterId` endpoint with a JSON body containing the new filter settings.

Example:

```json
{
  "type": "Low Pass",
  "frequency": 1500,
  "gain": -6,
  "q": 0.5
}
```

To change multiple (or all) filters, make a POST request to the `/measurements/:id/filters` endpoint with a JSON body containing a `FilterList` object.

Example:

```json
[
  {
    "filterId": "Filter 1",
    "type": "Low Pass",
    "frequency": 1500,
    "gain": -6,
    "q": 0.5
  },
  {
    "filterId": "Filter 2",
    "type": "High Pass",
    "frequency": 300,
    "gain": 0,
    "q": 1.0
  }
]
```

These new endpoints provide comprehensive support for managing a measurement's EQ and filter settings through the REW API.

## RTA Endpoint

The `/rta` endpoint provides control of REW's [RTA](https://www.roomeqwizard.com/help/help_en-GB/html/spectrum.html). A GET at the `/rta/commands` endpoint returns the list of commands, to issue a command POST it to the `/rta/command` endpoint.

The RTA setup can be queried or set via the `/rta/configuration`, `/rta/appearance-configuration` and `/rta/distortion-configuration` endpoints.

The current status of the RTA (whether it is enabled and whether it is running) can be read from the `/rta/status` endpoint. Rather than polling status with GET a [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified whenever the RTA status is updated. There are no parameters for status updates.

The current RTA input level can be read from the `/rta/levels` endpoint, that returns a list of RTALevel structures. The list has one RTALevel entry unless stereo inputs are being captured, when there are two. The data has a nanosecond timestamp and a running sum of the number of samples processed by the RTA since it was last started or restarted. The default unit for the level data is SPL but other units can be requested by including a unit query value in the url, e.g. `?unit=dBFS`. The list of units available can be read from `/rta/levels/units`. Rather than polling levels with GET a [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified whenever the RTA levels are updated. The subscription parameters can contain the required unit, for example:

```json
{
  "url": "http://127.0.0.1:5374/levels",
  "parameters": {"unit": "dBFS"}
}
```

The rms and peak RTA responses can be read from the `/rta/captured-data` and `/rta/captured-peak-data` endpoints respectively. A FrequencyResponse object is returned, with a nanosecond timestamp, a running sum of the number of samples processed by the RTA since it was last started or restarted, a smoothing setting, a start frequency, a points-per-octave figure (for fractional octave RTA captures) or a frequency step value (for spectrum captures), and a Base64-encoded string of response magnitudes generated from the raw bytes of 32-bit float magnitude values at each frequency. The default unit for the magnitude data is SPL but other units can be requested by including a unit query value in the url, e.g. `?unit=dBFS`. The list of units available can be read from `/rta/captured-data/units`. Rather than polling with GET a [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified whenever the RTA captured or captured peak data are updated. The subscription parameters can contain the required unit, for example:

```json
{
  "url": "http://127.0.0.1:5374/captured",
  "parameters": {"unit": "dBFS"}
}
```

When multiple inputs are being captured the parameters can contain an input index between 1 and the number of inputs, e.g index: 2. If there is no input index the rms average of the inputs is returned.

The current RTA input distortion results can be read from the `/rta/distortion` endpoint, that returns a list of RTADistortion structures. The list has one RTADistortion entry unless stereo inputs are being captured, when there are two. The data has a nanosecond timestamp and a running sum of the number of samples processed by the RTA since it was last started or restarted. The default unit for the level values is SPL but other units can be requested by including a unit query value in the url, e.g. `?unit=dBFS`. The list of units available can be read from `/rta/distortion/units`. The default unit for the relative distortion values is dB but percent can be requested by including a distortion query value in the url, e.g. `?distortion=percent`. The list of relative distortion units available can be read from `/rta/distortion/relative-units`. Rather than polling with GET a [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified whenever the RTA distortion results are updated. The subscription parameters can contain the required units, for example:

```json
{
  "url": "http://127.0.0.1:5374/distortion",
  "parameters": {"unit": "dBFS", "distortion": "percent"}
}
```

## Stepped Measurement Endpoint

The `/stepped-measurement` endpoint provides control of REW's [Stepped measurement](https://www.roomeqwizard.com/help/help_en-GB/html/spectrum.html#steppedsine) features.

A GET at the `/stepped-measurement/types` endpoint returns the list of measurement types. A GET at the `/stepped-measurement/type` endpoint returns the currently selected type, to select a type POST it to the `/stepped-measurement/type` endpoint.

A GET at the `/stepped-measurement/frequency-span` endpoint returns the current start frequency (Hz), end frequency (Hz) and PPO step for a measurement vs frequency as a SteppedFreqSpan object. To change the span POST a SteppedFreqSpan object to the `/stepped-measurement/frequency-span` endpoint. The list of accepted PPO values can be read from `/stepped-measurement/ppo-values`.

A GET at the `/stepped-measurement/level-span` endpoint returns the current start level (dBFS), end level (dBFS) and level step (dB) for a measurement vs level as a SteppedLevelSpan object. To change the span POST a SteppedLevelSpan object to the `/stepped-measurement/level-span` endpoint.

A GET at the `/stepped-measurement/fft-configuration` endpoint returns the current FFT configuration for stepped measurements. To change the configuration POST a SteppedFFTConfiguration object to the `/stepped-measurement/fft-configuration` endpoint.

A GET at the `/stepped-measurement/options` endpoint returns the current option settings for stepped measurements. To change the options POST a SteppedOptions object to the `/stepped-measurement/options` endpoint. Other RTA-related options such as distortion HP or LP should be configured through the `/rta` endpoint.

A GET at the `/stepped-measurement/commands` endpoint returns the list of commands, to issue a command POST it to the `/stepped-measurement/command` endpoint. The Start command requires a settlingTimems parameter and either a leveldBFS, frequencyHz or imdStimulus parameter depending on the type of measurement. Here is an example of starting a THD vs frequency measurement:

```json
{
  "command": "start",
  "parameters": {
    "settlingTimems": 0,
    "leveldBFS": -1.0
  }
}
```

The progress of a stepped measurement can be read from the `/stepped-measurement/progress` endpoint. That returns a SteppedProgress object, like the example below

```json
{
  "point": 5,
  "points": 31,
  "message": "5/31, approx 1m 8s remaining",
  "timeRemainingSeconds": 68
}
```

Rather than polling for progress, a [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified of progress updates. There are no parameters for progress subscriptions.

A [subscription](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#subscriptions) can be added to be notified of the distortion results for each point as they are generated. That is done by making a POST to the `/stepped-measurement/results/subscribe` endpoint. The results are returned as RTADistortion objects, see the [RTA section](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#rtadistortion) for the associated parameters. For example, to have updates posted to a `/stepped-results` endpoint a subscription could be:

```json
{
  "url": "http://127.0.0.1:5374/stepped-results",
  "parameters": {unit: dBFS, distortion: percent}
}
```

## Import Endpoint

The `/import` endpoint provides access to import functionality with subscription and blocking modes. The following sub-endpoints are available:

### Import Frequency Response

The `/import/frequency-response` endpoint allows the path to a text frequency response file to be set via a `FilePath` object. Note that backslashes in file paths must be escaped (meaning a double backslash is required), or forward slashes may be used. `FilePath` has a string for the path to the response file and an optional string for the channels that should be read from the file, not used for frequency response files. When the file is loaded it will generate a new measurement, that can be monitored by subscribing to the `/measurements` endpoint. The import may take some time. An import completed message will be posted to subscribers when an import has finished. A GET at the `/import/frequency-response` endpoint will return the path of the last file that finished importing. Any backslash in the path will be replaced by forward slash. An internal queue allows multiple imports to be requested without waiting for each to finish, up to 9 imports can be pending.

### Import Impulse Response

The `/import/impulse-response` endpoint allows the path to an impulse response file to be set via a `FilePath` object. Note that backslashes in file paths must be escaped (meaning a double backslash is required), or forward slashes may be used. `FilePath` has a string for the path to the response file and an optional string for the channels that should be read from the file if it has more than one channel. If the channels string is omitted or set to "All" all of the channels will be loaded, otherwise the specified channels will be loaded. Channels are numbered from 1 and channels to be loaded may be specified individually separated by commas or as a range separated by a dash, for example "1, 3, 5" or "2-4" or "1-3, 5, 7". As channels of data are loaded they will generate new measurements, that can be monitored by subscribing to the `/measurements` endpoint. The import may take some time. An import completed message will be posted to subscribers when an import has finished. A GET at the `/import/impulse-response` endpoint will return the path of the last file that finished importing. Any backslash in the path will be replaced by forward slash. An internal queue allows multiple imports to be requested without waiting for each to finish, up to 9 imports can be pending.

### Import RTA File

The `/import/rta-file` endpoint allows the path to a file for the RTA to process to be set via an `RTAFilePath` object. Note that backslashes in file paths must be escaped (meaning a double backslash is required), or forward slashes may be used. `FilePath` has a string for the path to the response file and an integer for the channel that should be read from the file. A string specifies the save option, whether the save the current, peak or both current and peak RTA results. Completion of RTA file import will generate a new measurement, that can be monitored by subscribing to the `/measurements` endpoint.

RTA file import may take some time. Import progress and an import completed message will be posted to subscribers. The percentage completion can be read from the `/import/rta-file/progress` endpoint. A GET at the `/import/rta-file` endpoint will return the path of the last file that finished importing. Any backslash in the path will be replaced by forward slash. An internal queue allows multiple imports to be requested without waiting for each to finish, up to 9 imports can be pending.

### Import Sweep Recordings

The `/import/sweep-recordings/stimulus` endpoint allows the path to the sweep stimulus file to be read or set. If the file is set successfully the response will contain a summary of the stimulus parameters. If the path has not been set reading the endpoint will return an empty string, otherwise it will return the file path. Note that backslashes in file paths must be escaped (meaning a double backslash is required), or forward slashes may be used.

The `/import/sweep-recordings/response` endpoint allows the path to the sweep response file to be read or set via a `FilePath` object. If the path has not been set reading the endpoint will return an empty string, otherwise it will return the last file path set. Note that backslashes in file paths must be escaped (meaning a double backslash is required), or forward slashes may be used. `FilePath` has a string for the path to the response file and an optional string for the channels that should be read from the file if it has more than one channel. If the channels string is omitted or set to "All" all of the channels will be loaded, otherwise the specified channels will be loaded. Channels are numbered from 1 and channels to be loaded may be specified individually separated by commas or as a range separated by a dash, for example "1, 3, 5" or "2-4" or "1-3, 5, 7". As channels of data are loaded they will generate new measurements, that can be monitored by subscribing to the `/measurements` endpoint. The import may take some time. An import completed message will be posted to subscribers when an import has finished. An internal queue allows multiple imports to be requested without waiting for each to finish, up to 9 imports can be pending.

## Object Model

This section describes the new object model for the REW API, including core objects, domain-specific entities, utility classes, and service classes.

### Core Objects

- **`RewClient`**: The primary client class responsible for connecting to the REW API, handling authentication, and managing requests and responses.
- **`RewConfiguration`**: Stores configuration settings like API endpoint, authentication tokens, and other relevant parameters for connecting to the REW API.
- **`RewRequest`** and **`RewResponse`**: Base classes for handling request and response specifics, possibly with subclasses for specific API endpoints.

### Domain-Specific Entities

- **`Measurement`**: Represents measurement data, with properties for frequency, amplitude, phase, and other measurement-specific details.
- **`RoomData`**: Contains details on room dimensions and properties relevant to acoustic measurement and optimization.
- **`Filter`**: Represents individual filter settings or profiles, including types (e.g., EQ filters, shelving), frequencies, and gains.

### Utility Classes

- **`RewParser`**: A class for parsing and interpreting data from REW's responses, converting raw API data into usable objects.
- **`FileHandler`**: If file operations are required, this can manage loading, saving, and interpreting data files used by REW.

### Service and Helper Classes

- **`MeasurementService`**: Manages all actions related to measurements, such as fetching, updating, and filtering.
- **`RoomAnalysisService`**: Handles computations based on room dimensions, calculates ideal EQ settings, and generates room-specific recommendations.
- **`FilterService`**: Handles the configuration of filters and interacts with the `MeasurementService` and `RoomAnalysisService` to apply filter adjustments.

This structure organizes API actions around core REW functions and maintains flexibility for extending capabilities in a modular, manageable way.
