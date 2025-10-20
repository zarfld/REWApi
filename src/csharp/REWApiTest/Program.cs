using REWApi;
using System;
using System.Threading.Tasks;

namespace REWApiTest;

/// <summary>
/// Test program for TDD verification of REW API client
/// Following Red-Green-Refactor methodology against live REW instance
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Starting TDD verification against live REW instance...");
        Console.WriteLine("🔄 This follows Red-Green-Refactor methodology");

        try
        {
            // First test simple HTTP communication
            Console.WriteLine("\n🧪 Testing basic HTTP communication...");
            await TestSimpleRequest.TestDirectHttpRequest();

            // Test 1: RED -> GREEN phase verification
            Console.WriteLine("\n✅ Test 1: ConnectAsync Instance Method (TDD GREEN Phase)");
            await TestConnectAsyncInstanceMethod();

            // Test 2: Static factory method verification  
            Console.WriteLine("\n✅ Test 2: Static Factory ConnectAsync Method");
            await TestStaticConnectAsync();

            // Test 3: Application status verification
            Console.WriteLine("\n✅ Test 3: Application Status via Instance");
            await TestGetApplicationStatusViaInstance();

            Console.WriteLine("\n🎉 All TDD tests passed! GREEN phase successful!");
            Console.WriteLine("📋 Next: REFACTOR phase - improve design while keeping tests green");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ TDD test failed - need to return to RED phase");
            Console.WriteLine($"\nTest failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Test the ConnectAsync instance method added in GREEN phase
    /// This verifies our TDD implementation works against live REW
    /// </summary>
    private static async Task TestConnectAsyncInstanceMethod()
    {
        Console.WriteLine("Creating REW client for localhost:4735...");
        
        // Create instance using static factory (proper API usage)
        var rewClient = await RewClient.ConnectAsync("http://localhost:4735");
        
        Console.WriteLine("✅ Static ConnectAsync factory method succeeded");
        
        // Now test the instance ConnectAsync method we added in GREEN phase
        Console.WriteLine("Testing instance ConnectAsync method...");
        await rewClient.ConnectAsync();
        
        Console.WriteLine("✅ Instance ConnectAsync method succeeded");
        Console.WriteLine("🟢 TDD GREEN phase verified - our implementation works!");
        
        rewClient.Dispose();
    }

    /// <summary>
    /// Test static factory method ConnectAsync
    /// </summary>
    private static async Task TestStaticConnectAsync()
    {
        Console.WriteLine("Testing static ConnectAsync factory method...");
        
        var rewClient = await RewClient.ConnectAsync("http://localhost:4735");
        Console.WriteLine("✅ Static ConnectAsync succeeded");
        
        // Verify we have a working client
        if (rewClient.Application == null)
        {
            throw new InvalidOperationException("Application service is null");
        }
        
        Console.WriteLine("✅ Application service properly initialized");
        rewClient.Dispose();
    }

    /// <summary>
    /// Test getting application status through the instance
    /// </summary>
    private static async Task TestGetApplicationStatusViaInstance()
    {
        Console.WriteLine("Testing application status retrieval...");
        
        var rewClient = await RewClient.ConnectAsync("http://localhost:4735");
        
        // Get status through the Application service
        var status = await rewClient.Application.GetStatus();
        
        Console.WriteLine($"✅ REW Status: Version={status.Version}, Blocking={status.BlockingMode}");
        Console.WriteLine($"   Command in Progress: {status.CommandInProgress}");
        Console.WriteLine($"   Current Command: {status.CurrentCommand ?? "None"}");
        
        if (string.IsNullOrEmpty(status.Version))
        {
            throw new InvalidOperationException("REW version information is missing");
        }
        
        Console.WriteLine("✅ Application status retrieval successful");
        rewClient.Dispose();
    }
}