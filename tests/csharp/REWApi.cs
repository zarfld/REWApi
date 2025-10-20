using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace REWApi.Tests.CSharp;

/// <summary>
/// Minimal REWApi class for testing HTTP Communication Core functionality
/// Contains only the methods needed to test REQ-F-001: HTTP Communication Core
/// Copied from src/csharp/REWApi.cs for testing purposes
/// </summary>
public class REWApi
{
    private static readonly HttpClient client = new HttpClient();
    private bool graphUpdateInhibited = false;
    private bool blockingModeEnabled = false;

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
        if (blockingModeEnabled)
        {
            await response.Content.LoadIntoBufferAsync();
        }
        return await response.Content.ReadAsStringAsync();
    }
}