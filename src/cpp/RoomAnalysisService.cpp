#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <string>
#include "RewClient.h"
#include "RewRequest.h"

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace pplx;

class RoomAnalysisService
{
public:
    RoomAnalysisService(RewClient& client) : client_(client) {}

    task<utility::string_t> GetRoomSizeAsync()
    {
        RewRequest request(U("/roomsim/room-size"), methods::GET);
        return client_.SendRequestAsync(request);
    }

    task<utility::string_t> SetRoomSizeAsync(const json::value& content)
    {
        RewRequest request(U("/roomsim/room-size"), methods::POST, content);
        return client_.SendRequestAsync(request);
    }

    task<utility::string_t> GetRoomIsSealedAsync()
    {
        RewRequest request(U("/roomsim/room-is-sealed"), methods::GET);
        return client_.SendRequestAsync(request);
    }

    task<utility::string_t> SetRoomIsSealedAsync(const json::value& content)
    {
        RewRequest request(U("/roomsim/room-is-sealed"), methods::POST, content);
        return client_.SendRequestAsync(request);
    }

private:
    RewClient& client_;
};
