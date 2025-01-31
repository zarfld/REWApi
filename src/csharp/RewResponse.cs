using System.Net.Http;
using System.Threading.Tasks;

public class RewResponse
{
    public HttpResponseMessage HttpResponseMessage { get; }

    public RewResponse(HttpResponseMessage httpResponseMessage)
    {
        HttpResponseMessage = httpResponseMessage;
    }

    public async Task<string> GetContentAsStringAsync()
    {
        return await HttpResponseMessage.Content.ReadAsStringAsync();
    }

    public async Task<byte[]> GetContentAsByteArrayAsync()
    {
        return await HttpResponseMessage.Content.ReadAsByteArrayAsync();
    }

    public async Task<Stream> GetContentAsStreamAsync()
    {
        return await HttpResponseMessage.Content.ReadAsStreamAsync();
    }
}
