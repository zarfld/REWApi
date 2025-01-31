using System;
using System.Net.Http;
using System.Threading.Tasks;

public class RewClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly string _authToken;

    public RewClient(string apiEndpoint, string authToken)
    {
        _httpClient = new HttpClient();
        _apiEndpoint = apiEndpoint;
        _authToken = authToken;
    }

    public async Task<string> SendRequestAsync(RewRequest request)
    {
        var httpRequestMessage = new HttpRequestMessage(request.Method, new Uri(_apiEndpoint + request.Endpoint))
        {
            Content = request.Content
        };

        httpRequestMessage.Headers.Add("Authorization", $"Bearer {_authToken}");

        var response = await _httpClient.SendAsync(httpRequestMessage);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
