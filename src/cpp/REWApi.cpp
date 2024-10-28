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

    pplx::task<utility::string_t> IssueMeasurementCommandWithParamsAsync(const utility::string_t& id, const utility::string_t& command, const json::value& parameters)
    {
        json::value content;
        content[U("command")] = json::value::string(command);
        content[U("parameters")] = parameters;

        utility::string_t endpoint = U("/measurements/") + id + U("/command");
        return PostAsync(endpoint, content);
    }

    pplx::task<utility::string_t> GenerateWaterfallAsync(const utility::string_t& mode, int slices, const json::value& parameters)
    {
        json::value content;
        content[U("command")] = json::value::string(U("Generate waterfall"));
        content[U("parameters")][U("mode")] = json::value::string(mode);
        content[U("parameters")][U("slices")] = json::value::number(slices);

        for (const auto& param : parameters.as_object())
        {
            content[U("parameters")][param.first] = param.second;
        }

        return PostAsync(U("/waterfalls"), content);
    }

    pplx::task<utility::string_t> GenerateSpectrogramAsync(const utility::string_t& mode, int slices, const json::value& parameters)
    {
        json::value content;
        content[U("command")] = json::value::string(U("Generate spectrogram"));
        content[U("parameters")][U("mode")] = json::value::string(mode);
        content[U("parameters")][U("slices")] = json::value::number(slices);

        for (const auto& param : parameters.as_object())
        {
            content[U("parameters")][param.first] = param.second;
        }

        return PostAsync(U("/spectrograms"), content);
    }

    pplx::task<utility::string_t> ProcessMeasurementsAsync(const json::value& content)
    {
        return PostAsync(U("/measurements/process-measurements"), content);
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
    pplx::task<utility::string_t> GetFrequencyResponseAsync(const utility::string_t& id, const utility::string_t& unit = U("SPL"), const utility::string_t& smoothing = U("default"), int ppo = 0)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/frequency-response?unit=") + unit + U("&smoothing=") + smoothing;
        if (ppo > 0)
        {
            endpoint += U("&ppo=") + std::to_wstring(ppo);
        }
        return GetAsync(endpoint);
    }

    // Methods for handling impulse response
    pplx::task<utility::string_t> GetImpulseResponseAsync(const utility::string_t& id, const utility::string_t& unit = U("Percent"), bool windowed = false, bool normalised = true)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/impulse-response?unit=") + unit;
        if (windowed)
        {
            endpoint += U("&windowed=true");
        }
        if (!normalised)
        {
            endpoint += U("&normalised=false");
        }
        return GetAsync(endpoint);
    }

    // Methods for handling IR window settings
    pplx::task<utility::string_t> GetIRWindowSettingsAsync(const utility::string_t& id)
    {
        return GetAsync(U("/measurements/") + id + U("/ir-windows"));
    }

    pplx::task<utility::string_t> UpdateIRWindowSettingsAsync(const utility::string_t& id, const json::value& content)
    {
        return PutAsync(U("/measurements/") + id + U("/ir-windows"), content);
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

    // Methods for handling audio driver selection
    pplx::task<utility::string_t> GetAudioDriverAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/driver"));
    }

    pplx::task<utility::string_t> SetAudioDriverAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/driver"), content);
    }

    // Methods for handling sample rate
    pplx::task<utility::string_t> GetSampleRateAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/samplerate"));
    }

    pplx::task<utility::string_t> SetSampleRateAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/samplerate"), content);
    }

    // Methods for handling input calibration
    pplx::task<utility::string_t> GetInputCalibrationAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/input-cal"));
    }

    pplx::task<utility::string_t> SetInputCalibrationAsync(const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/audio/input-cal"), content);
    }

    // Methods for handling output calibration
    pplx::task<utility::string_t> GetOutputCalibrationAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/output-cal"));
    }

    pplx::task<utility::string_t> SetOutputCalibrationAsync(const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/audio/output-cal"), content);
    }

    // Methods for handling Java driver items
    pplx::task<utility::string_t> GetJavaInputDeviceAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/input-device"));
    }

    pplx::task<utility::string_t> SetJavaInputDeviceAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/input-device"), content);
    }

    pplx::task<utility::string_t> GetJavaInputAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/input"));
    }

    pplx::task<utility::string_t> SetJavaInputAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/input"), content);
    }

    pplx::task<utility::string_t> GetJavaOutputDeviceAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/output-device"));
    }

    pplx::task<utility::string_t> SetJavaOutputDeviceAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/output-device"), content);
    }

    pplx::task<utility::string_t> GetJavaOutputAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/output"));
    }

    pplx::task<utility::string_t> SetJavaOutputAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/output"), content);
    }

    pplx::task<utility::string_t> GetJavaInputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/input-channel"));
    }

    pplx::task<utility::string_t> SetJavaInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/input-channel"), content);
    }

    pplx::task<utility::string_t> GetJavaOutputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/output-channel"));
    }

    pplx::task<utility::string_t> SetJavaOutputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/output-channel"), content);
    }

    pplx::task<utility::string_t> GetJavaRefInputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/ref-input-channel"));
    }

    pplx::task<utility::string_t> SetJavaRefInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/ref-input-channel"), content);
    }

    pplx::task<utility::string_t> GetJavaLastInputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/last-input-channel"));
    }

    pplx::task<utility::string_t> SetJavaLastInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/last-input-channel"), content);
    }

    pplx::task<utility::string_t> GetJavaRefOutputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/ref-output-channel"));
    }

    pplx::task<utility::string_t> SetJavaRefOutputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/ref-output-channel"), content);
    }

    pplx::task<utility::string_t> GetJavaNumInputChannelsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/num-input-channels"));
    }

    pplx::task<utility::string_t> GetJavaOutputChannelsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/output-channels"));
    }

    pplx::task<utility::string_t> GetJavaRefOutputChannelsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/ref-output-channels"));
    }

    pplx::task<utility::string_t> GetJavaOutputChannelMappingAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/output-channel-mapping"));
    }

    pplx::task<utility::string_t> SetJavaOutputChannelMappingAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/output-channel-mapping"), content);
    }

    pplx::task<utility::string_t> GetJavaStereoOnlyAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/java/stereo-only"));
    }

    pplx::task<utility::string_t> SetJavaStereoOnlyAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/java/stereo-only"), content);
    }

    // Methods for handling ASIO driver items
    pplx::task<utility::string_t> GetASIODeviceAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/device"));
    }

    pplx::task<utility::string_t> SetASIODeviceAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/device"), content);
    }

    pplx::task<utility::string_t> GetASIOInputAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/input"));
    }

    pplx::task<utility::string_t> SetASIOInputAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/input"), content);
    }

    pplx::task<utility::string_t> GetASIOOutputAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/output"));
    }

    pplx::task<utility::string_t> SetASIOOutputAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/output"), content);
    }

    pplx::task<utility::string_t> GetASIODevicesAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/devices"));
    }

    pplx::task<utility::string_t> GetASIOInputsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/inputs"));
    }

    pplx::task<utility::string_t> GetASIOOutputsAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/outputs"));
    }

    pplx::task<utility::string_t> GetASIOTimingReferenceOutputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/timing-reference-output-channel"));
    }

    pplx::task<utility::string_t> SetASIOTimingReferenceOutputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/timing-reference-output-channel"), content);
    }

    pplx::task<utility::string_t> GetASIORefInputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/ref-input-channel"));
    }

    pplx::task<utility::string_t> SetASIORefInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/ref-input-channel"), content);
    }

    pplx::task<utility::string_t> GetASIOLastInputChannelAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/last-input-channel"));
    }

    pplx::task<utility::string_t> SetASIOLastInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/last-input-channel"), content);
    }

    pplx::task<utility::string_t> SetASIOSecondaryOutputEnableAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/secondary-output-enable"), content);
    }

    pplx::task<utility::string_t> GetASIOSecondaryOutputAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/asio/secondary-output"));
    }

    pplx::task<utility::string_t> SetASIOSecondaryOutputAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/secondary-output"), content);
    }

    pplx::task<utility::string_t> SendASIOCommandAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/asio/command"), content);
    }

    // Methods for handling input levels
    pplx::task<utility::string_t> StartMonitoringInputLevelsAsync()
    {
        return PostAsync(U("http://localhost:4735/input-levels/command"), json::value::parse(U("{\"command\":\"start\"}")));
    }

    pplx::task<utility::string_t> StopMonitoringInputLevelsAsync()
    {
        return PostAsync(U("http://localhost:4735/input-levels/command"), json::value::parse(U("{\"command\":\"stop\"}")));
    }

    pplx::task<utility::string_t> SubscribeInputLevelsAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/input-levels/subscribe"), content);
    }

    pplx::task<utility::string_t> UnsubscribeInputLevelsAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/input-levels/unsubscribe"), content);
    }

    pplx::task<utility::string_t> GetLastInputLevelsAsync()
    {
        return GetAsync(U("http://localhost:4735/input-levels/last-levels"));
    }

    // Methods for handling selected measurement
    pplx::task<utility::string_t> SelectMeasurementByUUIDAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/measurements/selected-uuid"), content);
    }

    pplx::task<utility::string_t> SelectMeasurementByIndexAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/measurements/selected"), content);
    }

    pplx::task<utility::string_t> GetSelectedMeasurementUUIDAsync()
    {
        return GetAsync(U("http://localhost:4735/measurements/selected-uuid"));
    }

    pplx::task<utility::string_t> GetSelectedMeasurementIndexAsync()
    {
        return GetAsync(U("http://localhost:4735/measurements/selected"));
    }

    // Methods for handling measurement commands
    pplx::task<utility::string_t> GetMeasurementCommandsAsync()
    {
        return GetAsync(U("http://localhost:4735/measurements/commands"));
    }

    pplx::task<utility::string_t> IssueMeasurementCommandAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/measurements/command"), content);
    }

    // Method to update a measurement's name and/or notes
    pplx::task<utility::string_t> UpdateMeasurementAsync(const utility::string_t& id, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, U("/measurements/") + id, content).then([](http_response response) {
            return response.extract_string();
        });
    }

    // Methods for handling REW View preferences
    pplx::task<utility::string_t> GetREWViewPreferencesAsync()
    {
        return GetAsync(U("http://localhost:4735/application/settings"));
    }

    // Method to get distortion data
    pplx::task<utility::string_t> GetDistortionAsync(const utility::string_t& id, const utility::string_t& unit = U("percent"), int ppo = 3)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/distortion?unit=") + unit + U("&ppo=") + std::to_wstring(ppo);
        return GetAsync(endpoint);
    }
};
