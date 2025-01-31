using System.Net.Http;

public class RewRequest
{
    public string Endpoint { get; }
    public HttpMethod Method { get; }
    public HttpContent Content { get; }

    public RewRequest(string endpoint, HttpMethod method, HttpContent content = null)
    {
        Endpoint = endpoint;
        Method = method;
        Content = content;
    }
}
