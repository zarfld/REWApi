# Room EQ Wizard API Implementation

This repository contains implementations of the Room EQ Wizard (REW) API in C#, C++, JavaScript, and TypeScript. The REW API allows users to interact with REW over HTTP at localhost (127.0.0.1) on the default port 4735. The API server can be started using the -api argument when running REW. The API supports GET, PUT, and POST methods, with PUT and POST requiring a Pro upgrade license for automated sweep measurements. The API documentation is available via swagger-ui at localhost:4735, with the OpenAPI specification accessible in JSON or YAML formats.

## Setting Up and Using the REW API

1. Start the REW API server by running REW with the -api argument.
2. Access the API documentation via swagger-ui at localhost:4735.
3. Interact with the API endpoints using GET, PUT, and POST methods.

Note: When running REW without a GUI, you must use the [application](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#application) shutdown command to close REW when done.

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
