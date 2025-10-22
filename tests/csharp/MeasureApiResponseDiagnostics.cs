using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace REWApi.Tests
{
    /// <summary>
    /// Diagnostic tests to inspect actual REW API responses
    /// </summary>
    public class MeasureApiResponseDiagnostics : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:4735";

        public MeasureApiResponseDiagnostics(ITestOutputHelper output)
        {
            _output = output;
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task InspectNotes()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/notes");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectSweepRepetitions()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/sweep/repetitions");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectTimingReference()
        {
            // Try different possible endpoints
            var endpoints = new[] { "/measure/timing", "/measure/timing/reference", "/measure/timing-reference" };
            
            foreach (var endpoint in endpoints)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Endpoint: {endpoint}");
                _output.WriteLine($"Status: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
                    _output.WriteLine($"Raw Response: {content}");
                    _output.WriteLine($"Length: {content.Length}");
                }
                _output.WriteLine("---");
            }
        }

        [Fact]
        public async Task InspectTimingOffset()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/timing/offset");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectPlaybackMode()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/playback-mode");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectMeasurementMode()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/measurement-mode");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectNumberOfRepetitions()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/number-of-repetitions");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectStartDelay()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/start-delay");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectInvertSecondOutput()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/invert-second-output");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectFillSilenceWithDither()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/fill-silence-with-dither");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectCaptureNoiseFloor()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/measure/capture-noise-floor");
            var content = await response.Content.ReadAsStringAsync();
            
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"Raw Response: {content}");
            _output.WriteLine($"Length: {content.Length}");
        }

        [Fact]
        public async Task InspectSweepConfiguration()
        {
            // Try different possible endpoints
            var endpoints = new[] { "/measure/sweep", "/measure/sweep/configuration", "/measure/sweep-configuration" };
            
            foreach (var endpoint in endpoints)
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                var content = await response.Content.ReadAsStringAsync();
                
                _output.WriteLine($"Endpoint: {endpoint}");
                _output.WriteLine($"Status: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    _output.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
                    _output.WriteLine($"Raw Response: {content}");
                    _output.WriteLine($"Length: {content.Length}");
                }
                _output.WriteLine("---");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
