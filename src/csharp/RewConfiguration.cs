using System;

public class RewConfiguration
{
    public string ApiEndpoint { get; set; }
    public string AuthToken { get; set; }

    public RewConfiguration(string apiEndpoint, string authToken)
    {
        ApiEndpoint = apiEndpoint;
        AuthToken = authToken;
    }

    public void UpdateApiEndpoint(string newApiEndpoint)
    {
        ApiEndpoint = newApiEndpoint;
    }

    public void UpdateAuthToken(string newAuthToken)
    {
        AuthToken = newAuthToken;
    }
}
