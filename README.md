# Room EQ Wizard API Implementation

This repository contains implementations of the Room EQ Wizard (REW) API in C#, C++, JavaScript, and TypeScript. The REW API allows users to interact with REW over HTTP at localhost (127.0.0.1) on the default port 4735. The API server can be started using the -api argument when running REW. The API supports GET, PUT, and POST methods, with PUT and POST requiring a Pro upgrade license for automated sweep measurements. The API documentation is available via swagger-ui at localhost:4735, with the OpenAPI specification accessible in JSON or YAML formats.

## Setting Up and Using the REW API

1. Start the REW API server by running REW with the -api argument.
2. Access the API documentation via swagger-ui at localhost:4735.
3. Interact with the API endpoints using GET, PUT, and POST methods.

Note: When running REW without a GUI, you must use the [application](https://www.roomeqwizard.com/help/help_en-GB/html/api.html#application) shutdown command to close REW when done.

## Examples

### C#

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class REWApi
{
    private static readonly HttpClient client = new HttpClient();

    public async Task<string> GetAsync(string endpoint)
    {
        HttpResponseMessage response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PutAsync(string endpoint, HttpContent content)
    {
        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostAsync(string endpoint, HttpContent content)
    {
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
public:
    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const json::value& content)
    {
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
    async get(endpoint) {
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint, data) {
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
    async get(endpoint: string): Promise<any> {
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint: string, data: any): Promise<any> {
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
