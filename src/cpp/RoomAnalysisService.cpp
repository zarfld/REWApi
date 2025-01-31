#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <string>

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace pplx;

class RoomAnalysisService
{
public:
    task<utility::string_t> GetRoomSizeAsync()
    {
        return GetAsync(U("/roomsim/room-size"));
    }

    task<utility::string_t> SetRoomSizeAsync(const json::value& content)
    {
        return PostAsync(U("/roomsim/room-size"), content);
    }

    task<utility::string_t> GetRoomIsSealedAsync()
    {
        return GetAsync(U("/roomsim/room-is-sealed"));
    }

    task<utility::string_t> SetRoomIsSealedAsync(const json::value& content)
    {
        return PostAsync(U("/roomsim/room-is-sealed"), content);
    }

private:
    task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    task<utility::string_t> PostAsync(const utility::string_t& endpoint, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::POST, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }
};
