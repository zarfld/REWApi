#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <string>
#include "RewConfiguration.h"
#include "RewRequest.h"
#include "RewResponse.h"

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace pplx;

class RewClient
{
public:
    RewClient(const RewConfiguration& config) : config_(config) {}

    task<RewResponse> SendRequestAsync(const RewRequest& request)
    {
        http_client client(config_.GetApiEndpoint());
        return client.request(request.GetMethod(), request.GetEndpoint(), request.GetContent())
            .then([](http_response response) {
                return response.extract_json();
            })
            .then([](json::value jsonResponse) {
                return RewResponse(jsonResponse);
            });
    }

private:
    RewConfiguration config_;
};
