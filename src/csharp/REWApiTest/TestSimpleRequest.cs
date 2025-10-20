using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace REWApiTest
{
    /// <summary>
    /// Simple debug test to check HTTP communication with REW API
    /// </summary>
    public static class TestSimpleRequest
    {
        public static async Task TestDirectHttpRequest()
        {
            Console.WriteLine("=== Testing direct HTTP request to REW API ===");
            
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            try
            {
                Console.WriteLine("Testing /version endpoint...");
                var response = await httpClient.GetAsync("http://localhost:4735/version");
                Console.WriteLine($"Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response: {content}");
                    Console.WriteLine("✅ Direct HTTP request successful!");
                }
                else
                {
                    Console.WriteLine($"❌ HTTP error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }
    }
}