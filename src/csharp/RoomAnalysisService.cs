using System.Net.Http;
using System.Threading.Tasks;

public class RoomAnalysisService
{
    private readonly HttpClient _httpClient;

    public RoomAnalysisService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRoomSizeAsync()
    {
        string endpoint = "/roomsim/room-size";
        return await GetAsync(endpoint);
    }

    public async Task<string> SetRoomSizeAsync(HttpContent content)
    {
        string endpoint = "/roomsim/room-size";
        return await PostAsync(endpoint, content);
    }

    public async Task<string> GetRoomIsSealedAsync()
    {
        string endpoint = "/roomsim/room-is-sealed";
        return await GetAsync(endpoint);
    }

    public async Task<string> SetRoomIsSealedAsync(HttpContent content)
    {
        string endpoint = "/roomsim/room-is-sealed";
        return await PostAsync(endpoint, content);
    }

    private async Task<string> GetAsync(string endpoint)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> PostAsync(string endpoint, HttpContent content)
    {
        HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
