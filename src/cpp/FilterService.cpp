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

class FilterService
{
public:
    FilterService(RewClient& client) : client_(client) {}

    task<utility::string_t> GetMeasurementFiltersAsync(const utility::string_t& id)
    {
        RewRequest request(U("/measurements/") + id + U("/filters"), methods::GET);
        return client_.SendRequestAsync(request);
    }

    task<utility::string_t> UpdateMeasurementFilterAsync(const utility::string_t& id, const utility::string_t& filterId, const json::value& content)
    {
        RewRequest request(U("/measurements/") + id + U("/filters/") + filterId, methods::PUT, content);
        return client_.SendRequestAsync(request);
    }

private:
    RewClient& client_;
};
