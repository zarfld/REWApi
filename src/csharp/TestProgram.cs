using System;
using System.Net.Http;
using System.Threading.Tasks;
using REWApi;

namespace REWApi.TestConsole
{
    /// <summary>
    /// Simple test program to verify TDD implementation against running REW instance
    /// This represents our GREEN phase - making the test pass
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== REW API TDD Test Console ===");
            Console.WriteLine("Testing our ConnectAsync implementation against running REW...");
            Console.WriteLine();

            // Test our implementation
            await TestConnectAsync();
            await TestGetApplicationStatus();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task TestConnectAsync()
        {
            Console.WriteLine("🔴 RED: Testing ConnectAsync method (should have failed before implementation)");
            Console.WriteLine("🟢 GREEN: Now testing after implementation...");
            
            try
            {
                var baseUrl = "http://localhost:8080/api";  // REW default API port
                
                Console.WriteLine($"Attempting to connect to REW at {baseUrl}...");
                var client = await RewClient.ConnectAsync(baseUrl);
                
                Console.WriteLine("✅ SUCCESS: RewClient.ConnectAsync succeeded - REW is running and accessible!");
                Console.WriteLine($"   Connected to REW at {baseUrl}");
                
                // Now test our instance ConnectAsync method
                var instanceResult = await client.ConnectAsync();
                Console.WriteLine($"   Instance ConnectAsync result: {instanceResult}");
                
                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ FAILED: {ex.Message}");
                Console.WriteLine("   Expected REW to be running on localhost:8080 with API enabled");
            }
            
            Console.WriteLine();
        }

        static async Task TestGetApplicationStatus()
        {
            Console.WriteLine("Testing Application.GetStatus() method...");
            
            try
            {
                var baseUrl = "http://localhost:8080/api";
                
                var client = await RewClient.ConnectAsync(baseUrl);
                
                // Test getting status directly
                var status = await client.Application.GetStatus();
                
                Console.WriteLine("✅ SUCCESS: Retrieved REW application status:");
                Console.WriteLine($"   Version: {status.Version}");
                Console.WriteLine($"   Blocking Mode: {status.BlockingMode}");
                Console.WriteLine($"   Command In Progress: {status.CommandInProgress}");
                Console.WriteLine($"   Current Command: {status.CurrentCommand ?? "None"}");
                Console.WriteLine($"   Logging Enabled: {status.LoggingEnabled}");
                
                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine("   This indicates our API integration needs work");
            }
            
            Console.WriteLine();
        }
    }
}