#include <iostream>
#include <cpprest/http_client.h>
#include <cpprest/filestream.h>
#include <vector>
#include <string>
#include <cpprest/base64.h>

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace concurrency::streams;

class REWApi
{
private:
    bool graphUpdateInhibited = false;

public:
    void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const json::value& content)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const json::value& content)
    {
        if (graphUpdateInhibited) return pplx::task_from_result(utility::string_t());
        http_client client(U("http://localhost:4735"));
        return client.request(methods::POST, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    // Methods for handling application settings
    pplx::task<utility::string_t> GetApplicationSettingsAsync()
    {
        return GetAsync(U("http://localhost:4735/application/settings"));
    }

    pplx::task<utility::string_t> UpdateApplicationSettingsAsync(const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/application/settings"), content);
    }

    // Methods for handling audio settings
    pplx::task<utility::string_t> GetAudioSettingsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/settings"));
    }

    pplx::task<utility::string_t> UpdateAudioSettingsAsync(const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/audio/settings"), content);
    }

    // Methods for handling measurements
    pplx::task<utility::string_t> GetMeasurementsAsync()
    {
        return GetAsync(U("http://localhost:4735/measurements"));
    }

    pplx::task<utility::string_t> AddMeasurementAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/measurements"), content);
    }

    // Methods for handling frequency response
    pplx::task<utility::string_t> GetFrequencyResponseAsync()
    {
        return GetAsync(U("http://localhost:4735/frequencyresponse"));
    }

    // Methods for handling impulse response
    pplx::task<utility::string_t> GetImpulseResponseAsync()
    {
        return GetAsync(U("http://localhost:4735/impulseresponse"));
    }

    // Methods for handling EQ and filters
    pplx::task<utility::string_t> GetEQSettingsAsync()
    {
        return GetAsync(U("http://localhost:4735/eq/settings"));
    }

    pplx::task<utility::string_t> UpdateEQSettingsAsync(const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/eq/settings"), content);
    }

    // Methods for handling SPL meter
    pplx::task<utility::string_t> GetSPLMeterAsync()
    {
        return GetAsync(U("http://localhost:4735/splmeter"));
    }

    // Methods for handling RTA
    pplx::task<utility::string_t> GetRTAAsync()
    {
        return GetAsync(U("http://localhost:4735/rta"));
    }

    // Methods for handling room simulator
    pplx::task<utility::string_t> GetRoomSimulatorAsync()
    {
        return GetAsync(U("http://localhost:4735/roomsimulator"));
    }

    // Method for handling shutdown command
    pplx::task<utility::string_t> ShutdownAsync()
    {
        return PostAsync(U("http://localhost:4735/application/command"), json::value::parse(U("{\"command\":\"shutdown\"}")));
    }

    std::string encodeArray(const std::vector<float>& array)
    {
        std::vector<unsigned char> bytes(array.size() * sizeof(float));
        std::memcpy(bytes.data(), array.data(), bytes.size());
        return utility::conversions::to_utf8string(utility::conversions::to_base64(bytes));
    }

    std::vector<float> decodeArray(const std::string& base64Encoded)
    {
        auto bytes = utility::conversions::from_base64(utility::conversions::to_string_t(base64Encoded));
        std::vector<float> array(bytes.size() / sizeof(float));
        std::memcpy(array.data(), bytes.data(), bytes.size());
        return array;
    }

    // Method to add a subscription
    pplx::task<utility::string_t> SubscribeAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/subscribe"), content);
    }

    // Method to remove a subscription
    pplx::task<utility::string_t> UnsubscribeAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/unsubscribe"), content);
    }

    // Method to get the list of subscribers
    pplx::task<utility::string_t> GetSubscribersAsync()
    {
        return GetAsync(U("http://localhost:4735/subscribers"));
    }

    // Methods for handling errors and warnings
    pplx::task<utility::string_t> GetErrorsAsync()
    {
        return GetAsync(U("http://localhost:4735/application/errors"));
    }

    pplx::task<utility::string_t> GetLastErrorAsync()
    {
        return GetAsync(U("http://localhost:4735/application/last-error"));
    }

    pplx::task<utility::string_t> GetWarningsAsync()
    {
        return GetAsync(U("http://localhost:4735/application/warnings"));
    }

    pplx::task<utility::string_t> GetLastWarningAsync()
    {
        return GetAsync(U("http://localhost:4735/application/last-warning"));
    }

    // Methods for enabling and disabling blocking mode
    pplx::task<utility::string_t> SetBlockingModeAsync(bool enable)
    {
        json::value content;
        content[U("blocking")] = json::value::boolean(enable);
        return PostAsync(U("http://localhost:4735/application/blocking"), content);
    }

    // Methods for handling graph update inhibition
    pplx::task<utility::string_t> SetGraphUpdateInhibitionAsync(bool inhibit)
    {
        json::value content;
        content[U("inhibit")] = json::value::boolean(inhibit);
        return PostAsync(U("http://localhost:4735/application/inhibit-graph-updates"), content);
    }
};
