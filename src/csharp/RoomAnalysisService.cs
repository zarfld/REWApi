using System.Net.Http;
using System.Threading.Tasks;

public class RoomAnalysisService
{
    private readonly RewClient _rewClient;

    public RoomAnalysisService(RewClient rewClient)
    {
        _rewClient = rewClient;
    }

    public async Task<string> GetRoomSizeAsync()
    {
        RewRequest request = new RewRequest("/roomsim/room-size", HttpMethod.Get);
        return await _rewClient.SendRequestAsync(request);
    }

    public async Task<string> SetRoomSizeAsync(HttpContent content)
    {
        RewRequest request = new RewRequest("/roomsim/room-size", HttpMethod.Post, content);
        return await _rewClient.SendRequestAsync(request);
    }

    public async Task<string> GetRoomIsSealedAsync()
    {
        RewRequest request = new RewRequest("/roomsim/room-is-sealed", HttpMethod.Get);
        return await _rewClient.SendRequestAsync(request);
    }

    public async Task<string> SetRoomIsSealedAsync(HttpContent content)
    {
        RewRequest request = new RewRequest("/roomsim/room-is-sealed", HttpMethod.Post, content);
        return await _rewClient.SendRequestAsync(request);
    }
}
