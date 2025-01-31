#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <string>

using namespace web;
using namespace web::http;

class RewRequest
{
public:
    RewRequest(const utility::string_t& endpoint, const method& method, const json::value& content = json::value())
        : endpoint_(endpoint), method_(method), content_(content) {}

    utility::string_t GetEndpoint() const { return endpoint_; }
    method GetMethod() const { return method_; }
    json::value GetContent() const { return content_; }

private:
    utility::string_t endpoint_;
    method method_;
    json::value content_;
};
