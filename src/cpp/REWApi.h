#ifndef REWAPI_H
#define REWAPI_H

#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>

class REWApi
{
public:
    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint);
    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const web::json::value& content);
    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const web::json::value& content);

    // Methods for handling application settings
    pplx::task<utility::string_t> GetApplicationSettingsAsync();
    pplx::task<utility::string_t> UpdateApplicationSettingsAsync(const web::json::value& content);

    // Methods for handling audio settings
    pplx::task<utility::string_t> GetAudioSettingsAsync();
    pplx::task<utility::string_t> UpdateAudioSettingsAsync(const web::json::value& content);

    // Methods for handling measurements
    pplx::task<utility::string_t> GetMeasurementsAsync();
    pplx::task<utility::string_t> AddMeasurementAsync(const web::json::value& content);

    // Methods for handling frequency response
    pplx::task<utility::string_t> GetFrequencyResponseAsync();

    // Methods for handling impulse response
    pplx::task<utility::string_t> GetImpulseResponseAsync();

    // Methods for handling EQ and filters
    pplx::task<utility::string_t> GetEQSettingsAsync();
    pplx::task<utility::string_t> UpdateEQSettingsAsync(const web::json::value& content);

    // Methods for handling SPL meter
    pplx::task<utility::string_t> GetSPLMeterAsync();

    // Methods for handling RTA
    pplx::task<utility::string_t> GetRTAAsync();

    // Methods for handling room simulator
    pplx::task<utility::string_t> GetRoomSimulatorAsync();
};

#endif // REWAPI_H
