using System.Text.Json;

using var httpClient = new HttpClient();

Console.WriteLine("=== Getting measurements list ===");
var listResponse = await httpClient.GetAsync("http://localhost:4735/measurements");
var listJson = await listResponse.Content.ReadAsStringAsync();

var measurements = JsonSerializer.Deserialize<JsonElement>(listJson);

if (measurements.ValueKind != JsonValueKind.Object)
{
    Console.WriteLine("ERROR: Invalid measurements response");
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

Console.WriteLine("=== JSON PROPERTY NAMES ===");
var data = JsonSerializer.Deserialize<JsonElement>(freqJson);
foreach (var prop in data.EnumerateObject())
{
    var typeInfo = prop.Value.ValueKind.ToString();
    if (prop.Value.ValueKind == JsonValueKind.String)
    {
        var len = prop.Value.GetString()?.Length ?? 0;
        typeInfo = $"String (length={len})";
    }
    Console.WriteLine($"  {prop.Name}: {typeInfo}");
}
