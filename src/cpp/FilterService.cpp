#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <string>

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace pplx;

class FilterService
{
public:
    task<utility::string_t> GetMeasurementFiltersAsync(const utility::string_t& id)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/filters");
        return GetAsync(endpoint);
    }

    task<utility::string_t> UpdateMeasurementFilterAsync(const utility::string_t& id, const utility::string_t& filterId, const json::value& content)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/filters/") + filterId;
        return PutAsync(endpoint, content);
    }

private:
    task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    task<utility::string_t> PutAsync(const utility::string_t& endpoint, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }
};
