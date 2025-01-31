#include <string>

class RewConfiguration
{
public:
    RewConfiguration(const std::string& apiEndpoint, const std::string& authToken)
        : apiEndpoint_(apiEndpoint), authToken_(authToken) {}

    std::string GetApiEndpoint() const { return apiEndpoint_; }
    std::string GetAuthToken() const { return authToken_; }

    void SetApiEndpoint(const std::string& apiEndpoint) { apiEndpoint_ = apiEndpoint; }
    void SetAuthToken(const std::string& authToken) { authToken_ = authToken; }

private:
    std::string apiEndpoint_;
    std::string authToken_;
};
