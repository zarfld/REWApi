#include <cpprest/json.h>
#include <string>

using namespace web;

class RewResponse
{
public:
    RewResponse(const json::value& jsonResponse) : jsonResponse_(jsonResponse) {}

    json::value GetJsonResponse() const { return jsonResponse_; }

    std::string GetStringField(const std::string& fieldName) const
    {
        if (jsonResponse_.has_field(utility::conversions::to_string_t(fieldName)))
        {
            return utility::conversions::to_utf8string(jsonResponse_.at(utility::conversions::to_string_t(fieldName)).as_string());
        }
        return "";
    }

    int GetIntField(const std::string& fieldName) const
    {
        if (jsonResponse_.has_field(utility::conversions::to_string_t(fieldName)))
        {
            return jsonResponse_.at(utility::conversions::to_string_t(fieldName)).as_integer();
        }
        return 0;
    }

    double GetDoubleField(const std::string& fieldName) const
    {
        if (jsonResponse_.has_field(utility::conversions::to_string_t(fieldName)))
        {
            return jsonResponse_.at(utility::conversions::to_string_t(fieldName)).as_double();
        }
        return 0.0;
    }

    bool GetBoolField(const std::string& fieldName) const
    {
        if (jsonResponse_.has_field(utility::conversions::to_string_t(fieldName)))
        {
            return jsonResponse_.at(utility::conversions::to_string_t(fieldName)).as_bool();
        }
        return false;
    }

private:
    json::value jsonResponse_;
};
