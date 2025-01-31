using System;
using System.Net.Http;
using System.Threading.Tasks;

public class FilterService
{
    private readonly RewClient _rewClient;

    public FilterService(RewClient rewClient)
    {
        _rewClient = rewClient;
    }

    public async Task<string> GetMeasurementFiltersAsync(string id)
    {
        RewRequest request = new RewRequest($"/measurements/{id}/filters", HttpMethod.Get);
        return await _rewClient.SendRequestAsync(request);
    }

    public async Task<string> UpdateMeasurementFilterAsync(string id, string filterId, HttpContent content)
    {
        RewRequest request = new RewRequest($"/measurements/{id}/filters/{filterId}", HttpMethod.Put, content);
        return await _rewClient.SendRequestAsync(request);
    }
}
