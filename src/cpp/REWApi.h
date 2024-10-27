#ifndef REWAPI_H
#define REWAPI_H

#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <vector>
#include <string>

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

    // Method to set graph update inhibition
    void SetGraphUpdateInhibition(bool inhibit);

    // Methods for encoding and decoding arrays
    std::string encodeArray(const std::vector<float>& array);
    std::vector<float> decodeArray(const std::string& base64Encoded);

    // Method to add a subscription
    pplx::task<utility::string_t> SubscribeAsync(const web::json::value& content);

    // Method to remove a subscription
    pplx::task<utility::string_t> UnsubscribeAsync(const web::json::value& content);

    // Method to get the list of subscribers
    pplx::task<utility::string_t> GetSubscribersAsync();

    // Methods for handling errors and warnings
    pplx::task<utility::string_t> GetErrorsAsync();
    pplx::task<utility::string_t> GetLastErrorAsync();
    pplx::task<utility::string_t> GetWarningsAsync();
    pplx::task<utility::string_t> GetLastWarningAsync();

    // Methods for enabling and disabling blocking mode
    pplx::task<utility::string_t> SetBlockingModeAsync(bool enable);

    // Methods for handling graph update inhibition
    pplx::task<utility::string_t> SetGraphUpdateInhibitionAsync(bool inhibit);

    // Methods for handling audio driver selection
    pplx::task<utility::string_t> GetAudioDriverAsync();
    pplx::task<utility::string_t> SetAudioDriverAsync(const web::json::value& content);

    // Methods for handling sample rate
    pplx::task<utility::string_t> GetSampleRateAsync();
    pplx::task<utility::string_t> SetSampleRateAsync(const web::json::value& content);

    // Methods for handling input calibration
    pplx::task<utility::string_t> GetInputCalibrationAsync();
    pplx::task<utility::string_t> SetInputCalibrationAsync(const web::json::value& content);

    // Methods for handling output calibration
    pplx::task<utility::string_t> GetOutputCalibrationAsync();
    pplx::task<utility::string_t> SetOutputCalibrationAsync(const web::json::value& content);

private:
    bool graphUpdateInhibited;
};

#endif // REWAPI_H
