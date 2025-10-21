using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient();
        
        Console.WriteLine("=== Getting measurements list ===");
        var listResponse = await httpClient.GetAsync("http://localhost:4735/measurements");
        var listJson = await listResponse.Content.ReadAsStringAsync();
        
        var measurements = JsonSerializer.Deserialize<JsonElement>(listJson);
        
        if (measurements.ValueKind != JsonValueKind.Object || measurements.EnumerateObject().MoveNext() == false)
        {
            Console.WriteLine("ERROR: No measurements found in REW");
            return;
        }
        
        // Get first measurement UUID
        var firstMeasurement = measurements.EnumerateObject().First().Value;
        var uuid = firstMeasurement.GetProperty("uuid").GetString();
        
        Console.WriteLine($"Using measurement UUID: {uuid}");
        Console.WriteLine();
        
        Console.WriteLine("=== Getting frequency response ===");
        var freqUrl = $"http://localhost:4735/measurements/{uuid}/frequency-response?unit=SPL";
        Console.WriteLine($"URL: {freqUrl}");
        Console.WriteLine();
        
        var freqResponse = await httpClient.GetAsync(freqUrl);
        var freqJson = await freqResponse.Content.ReadAsStringAsync();
        
        Console.WriteLine("=== RAW JSON (first 2000 chars) ===");
        Console.WriteLine(freqJson.Substring(0, Math.Min(2000, freqJson.Length)));
        Console.WriteLine();
        
        Console.WriteLine("=== JSON STRUCTURE ===");
        var data = JsonSerializer.Deserialize<JsonElement>(freqJson);
        PrintJsonStructure(data, "root", 0);
    }
    
    static void PrintJsonStructure(JsonElement element, string name, int indent)
    {
        var indentStr = new string(' ', indent * 2);
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                Console.WriteLine($"{indentStr}{name} [Object]");
                foreach (var prop in element.EnumerateObject())
                {
                    PrintJsonStructure(prop.Value, prop.Name, indent + 1);
                }
                break;
                
            case JsonValueKind.Array:
                Console.WriteLine($"{indentStr}{name} [Array, length={element.GetArrayLength()}]");
                break;
                
            case JsonValueKind.String:
                var str = element.GetString();
                var len = str?.Length ?? 0;
                var preview = len > 40 ? str!.Substring(0, 40) + "..." : str;
                Console.WriteLine($"{indentStr}{name} [String, length={len}] = \"{preview}\"");
                break;
                
            case JsonValueKind.Number:
                Console.WriteLine($"{indentStr}{name} [Number] = {element.GetRawText()}");
                break;
                
            case JsonValueKind.True:
            case JsonValueKind.False:
                Console.WriteLine($"{indentStr}{name} [Boolean] = {element.GetBoolean()}");
                break;
                
            case JsonValueKind.Null:
                Console.WriteLine($"{indentStr}{name} [Null]");
                break;
        }
    }
}
