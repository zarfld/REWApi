using System;
using System.Net.Http;
using System.Threading.Tasks;

public class FilterService
{
    private readonly HttpClient _httpClient;

    public FilterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetMeasurementFiltersAsync(string id)
    {
        string endpoint = $"/measurements/{id}/filters";
        return await GetAsync(endpoint);
    }

    public async Task<string> UpdateMeasurementFilterAsync(string id, string filterId, HttpContent content)
    {
        string endpoint = $"/measurements/{id}/filters/{filterId}";
        return await PutAsync(endpoint, content);
    }

    private async Task<string> GetAsync(string endpoint)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> PutAsync(string endpoint, HttpContent content)
    {
        HttpResponseMessage response = await _httpClient.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
