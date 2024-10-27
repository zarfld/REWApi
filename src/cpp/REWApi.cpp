#include <iostream>
#include <cpprest/http_client.h>
#include <cpprest/filestream.h>

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace concurrency::streams;

class REWApi
{
public:
    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::GET, endpoint).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, endpoint, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const json::value& content)
    {
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
};
