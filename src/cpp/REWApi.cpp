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
    bool blockingModeEnabled = false;

    pplx::task<void> NotifySubscribers(const utility::string_t& endpoint, const json::value& content)
    {
        return GetSubscribersAsync().then([this, endpoint, content](utility::string_t subscribers) {
            json::value subscribersJson = json::value::parse(subscribers);
            std::vector<pplx::task<void>> tasks;
            for (const auto& subscriber : subscribersJson.as_array())
            {
                utility::string_t url = subscriber.at(U("url")).as_string();
                tasks.push_back(PostAsync(url + endpoint, content).then([](utility::string_t response) {
                    // Handle response if needed
                }));
            }
            return pplx::when_all(tasks.begin(), tasks.end());
        });
    }

public:
    void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    pplx::task<void> SetGraphUpdateInhibitionAsync(bool inhibit)
    {
        return pplx::create_task([this, inhibit]() {
            graphUpdateInhibited = inhibit;
        });
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
        return client.request(methods::POST, endpoint, content).then([this](http_response response) {
            if (blockingModeEnabled)
            {
                return response.content_ready().then([](http_response response) {
                    return response.extract_string();
                });
            }
            return response.extract_string();
        });
    }

    pplx::task<utility::string_t> IssueMeasurementCommandWithParamsAsync(const utility::string_t& id, const utility::string_t& command, const json::value& parameters)
    {
        // Validate boolean parameters
        std::vector<utility::string_t> booleanParams = {U("include cal"), U("append lf tail"), U("append hf tail"), U("frequency warping"), U("replicate data")};
        for (const auto& param : booleanParams)
        {
            if (parameters.has_field(param) && !parameters.at(param).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter '") + param + U("'. Expected a boolean value.")));
            }
        }

        // Validate numeric parameters
        if (parameters.has_field(U("lf tail slope")) && parameters.at(U("lf tail slope")).as_double() < 0)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0.")));
        }
        if (parameters.has_field(U("hf tail slope")) && parameters.at(U("hf tail slope")).as_double() > 0)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'hf tail slope'. Must be ≤ 0.")));
        }

        // Validate required parameters
        if (parameters.at(U("append lf tail")).as_bool())
        {
            if (!parameters.has_field(U("lf tail start")) || !parameters.has_field(U("lf tail slope")))
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing required parameters 'lf tail start' and/or 'lf tail slope'.")));
            }
        }
        if (parameters.at(U("append hf tail")).as_bool())
        {
            if (!parameters.has_field(U("hf tail start")) || !parameters.has_field(U("hf tail slope")) || !parameters.has_field(U("frequency warping")))
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing required parameters 'hf tail start', 'hf tail slope', and/or 'frequency warping'.")));
            }
        }
        if (!parameters.at(U("append lf tail")).as_bool() || !parameters.at(U("append hf tail")).as_bool())
        {
            if (!parameters.has_field(U("replicate data")))
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter 'replicate data'.")));
            }
        }

        json::value content;
        content[U("command")] = json::value::string(command);
        content[U("parameters")] = parameters;

        utility::string_t endpoint = U("/measurements/") + id + U("/command");
        return PostAsync(endpoint, content).then([this, endpoint, content](utility::string_t response) {
            return NotifySubscribers(endpoint, content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> GenerateWaterfallAsync(const utility::string_t& mode, int slices, const json::value& parameters)
    {
        // Validate required parameters for "Fourier" mode
        if (mode == U("Fourier"))
        {
            std::vector<utility::string_t> requiredParams = {U("left window type"), U("right window type"), U("window width ms"), U("time range ms"), U("rise time ms"), U("use csd mode")};
            for (const auto& param : requiredParams)
            {
                if (!parameters.has_field(param))
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter '") + param + U("' for 'Fourier' mode.")));
                }
            }
        }
        // Validate required parameters for "Burst decay" mode
        else if (mode == U("Burst decay"))
        {
            std::vector<utility::string_t> requiredParams = {U("bandwidth"), U("periods")};
            for (const auto& param : requiredParams)
            {
                if (!parameters.has_field(param))
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter '") + param + U("' for 'Burst decay' mode.")));
                }
            }
        }
        else
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid mode. Expected 'Fourier' or 'Burst decay'.")));
        }

        json::value content;
        content[U("command")] = json::value::string(U("Generate waterfall"));
        content[U("parameters")][U("mode")] = json::value::string(mode);
        content[U("parameters")][U("slices")] = json::value::number(slices);

        for (const auto& param : parameters.as_object())
        {
            content[U("parameters")][param.first] = param.second;
        }

        return PostAsync(U("/waterfalls"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/waterfalls"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> GenerateSpectrogramAsync(const utility::string_t& mode, int slices, const json::value& parameters)
    {
        // Validate the "mode" parameter
        std::vector<utility::string_t> validModes = {U("Fourier"), U("Wavelet"), U("Airy CWT"), U("Morlet CWT"), U("Burst decay")};
        if (std::find(validModes.begin(), validModes.end(), mode) == validModes.end())
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'mode'.")));
        }

        // Validate the "slices" parameter
        if (slices <= 0)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'slices'. Must be > 0.")));
        }

        // Validate the "ppo" parameter
        if (!parameters.has_field(U("ppo")) || !parameters.at(U("ppo")).is_number())
        {
            return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'ppo' parameter. Expected a number.")));
        }

        // Validate the "amplitude" parameter
        if (!parameters.has_field(U("amplitude")) || !parameters.at(U("amplitude")).is_string())
        {
            return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'amplitude' parameter. Expected a string.")));
        }

        // Validate additional parameters required for different modes
        if (mode == U("Fourier"))
        {
            std::vector<utility::string_t> requiredParams = {U("window type"), U("window width ms"), U("before ms"), U("after ms")};
            for (const auto& param : requiredParams)
            {
                if (!parameters.has_field(param))
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter '") + param + U("' for 'Fourier' mode.")));
                }
            }
        }
        else if (mode == U("Burst decay"))
        {
            std::vector<utility::string_t> requiredParams = {U("bandwidth"), U("periods")};
            for (const auto& param : requiredParams)
            {
                if (!parameters.has_field(param))
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter '") + param + U("' for 'Burst decay' mode.")));
                }
            }
        }
        else if (mode == U("Wavelet") || mode == U("Airy CWT") || mode == U("Morlet CWT"))
        {
            std::vector<utility::string_t> requiredParams = {U("bandwidth"), U("before ms"), U("after ms")};
            for (const auto& param : requiredParams)
            {
                if (!parameters.has_field(param))
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter '") + param + U("' for '") + mode + U("' mode.")));
                }
            }
        }

        json::value content;
        content[U("command")] = json::value::string(U("Generate spectrogram"));
        content[U("parameters")][U("mode")] = json::value::string(mode);
        content[U("parameters")][U("slices")] = json::value::number(slices);

        for (const auto& param : parameters.as_object())
        {
            content[U("parameters")][param.first] = param.second;
        }

        return PostAsync(U("/spectrograms"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/spectrograms"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> ProcessMeasurementsAsync(const json::value& content)
    {
        // Check if the processName is "Align SPL"
        if (content.has_field(U("processName")) && content.at(U("processName")).as_string() == U("Align SPL"))
        {
            // Validate required parameters
            if (!content.has_field(U("parameters")) || !content.at(U("parameters")).has_field(U("targetdB")) || !content.at(U("parameters")).has_field(U("frequencyHz")) || !content.at(U("parameters")).has_field(U("spanOctaves")))
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing required parameters 'targetdB', 'frequencyHz', and/or 'spanOctaves'.")));
            }

            // Validate measurementIndices field
            if (!content.has_field(U("measurementIndices")) || !content.at(U("measurementIndices")).is_array())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'measurementIndices' field. Must be an array.")));
            }
        }

        // Check if the processName is "Arithmetic"
        if (content.has_field(U("processName")) && content.at(U("processName")).as_string() == U("Arithmetic"))
        {
            // Validate required parameters
            if (!content.has_field(U("parameters")) || !content.at(U("parameters")).has_field(U("function")))
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing required parameter 'function'.")));
            }

            // Validate optional parameters
            if (content.at(U("parameters")).has_field(U("maxGain")) && !content.at(U("maxGain")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'maxGain'. Expected a number.")));
            }
            if (content.at(U("parameters")).has_field(U("lowerLimit")) && !content.at(U("lowerLimit")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'lowerLimit'. Expected a number.")));
            }
            if (content.at(U("parameters")).has_field(U("upperLimit")) && !content.at(U("upperLimit")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'upperLimit'. Expected a number.")));
            }
            if (content.at(U("parameters")).has_field(U("mergeFrequency")) && !content.at(U("mergeFrequency")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'mergeFrequency'. Expected a number.")));
            }
            if (content.at(U("parameters")).has_field(U("mergeBlend")) && !content.at(U("mergeBlend")).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'mergeBlend'. Expected a boolean.")));
            }
            if (content.at(U("parameters")).has_field(U("targetLevel")) && !content.at(U("targetLevel")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'targetLevel'. Expected a number.")));
            }
            if (content.at(U("parameters")).has_field(U("autoTarget")) && !content.at(U("autoTarget")).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'autoTarget'. Expected a boolean.")));
            }
            if (content.at(U("parameters")).has_field(U("excludeNotches")) && !content.at(U("excludeNotches")).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'excludeNotches'. Expected a boolean.")));
            }

            // Validate measurementIndices field
            if (!content.has_field(U("measurementIndices")) || !content.at(U("measurementIndices")).is_array())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'measurementIndices' field. Must be an array.")));
            }

            // Perform arithmetic operations based on the specified function and parameters
            utility::string_t function = content.at(U("parameters")).at(U("function")).as_string();
            std::vector<int> measurementIndices;
            for (const auto& index : content.at(U("measurementIndices")).as_array())
            {
                measurementIndices.push_back(index.as_integer());
            }

            // Placeholder for actual arithmetic operations
            // Perform the arithmetic operations on the measurements specified in measurementIndices
            // Apply optional parameters such as maxGain, lowerLimit, upperLimit, mergeFrequency, mergeBlend, targetLevel, autoTarget, and excludeNotches

            // Example: Perform A * B trace arithmetic
            if (function == U("A * B"))
            {
                // Perform A * B arithmetic operation
            }
            // Example: Perform 1 / A trace arithmetic
            else if (function == U("1 / A"))
            {
                // Perform 1 / A arithmetic operation
            }
        }

        return PostAsync(U("/measurements/process-measurements"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/measurements/process-measurements"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> GetProcessCommandsAsync()
    {
        json::value commands = json::value::array();
        commands[0] = json::value::string(U("Align SPL"));
        commands[1] = json::value::string(U("Arithmetic"));
        return pplx::task_from_result(commands.serialize());
    }

    pplx::task<utility::string_t> GetProcessResultAsync()
    {
        return GetAsync(U("/measurements/process-result"));
    }

    pplx::task<utility::string_t> PostProcessResultAsync(const utility::string_t& url, const json::value& content)
    {
        return PostAsync(url, content);
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
        return PostAsync(U("http://localhost:4735/measurements"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/measurements"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> DeleteMeasurementAsync(const utility::string_t& id)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::DEL, U("/measurements/") + id).then([this, id](http_response response) {
            return response.extract_string().then([this, id](utility::string_t response) {
                return NotifySubscribers(U("/measurements/") + id, json::value::parse(U("{}"))).then([response]() {
                    return response;
                });
            });
        });
    }

    pplx::task<utility::string_t> DeleteAllMeasurementsAsync()
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::DEL, U("/measurements")).then([this](http_response response) {
            return response.extract_string().then([this](utility::string_t response) {
                return NotifySubscribers(U("/measurements"), json::value::parse(U("{}"))).then([response]() {
                    return response;
                });
            });
        });
    }

    // Methods for handling frequency response
    pplx::task<utility::string_t> GetFrequencyResponseAsync(const utility::string_t& id, const utility::string_t& unit = U("SPL"), const utility::string_t& smoothing = U("default"), int ppo = 0)
    {
        utility::string_t endpoint = U("/measurements/") + id + U("/frequency-response?unit=") + unit + U("&smoothing=") + smoothing;
        if (ppo > 0)
        {
            endpoint += U("&ppo=") + std::to_wstring(ppo);
        }
        return GetAsync(endpoint).then([this, ppo, smoothing](utility::string_t response) {
            if (ppo > 0 && smoothing == U("default"))
            {
                // Apply smoothing to ppo/2 if greater smoothing has not already been applied
                // This is a placeholder for the actual smoothing logic
                // This is a placeholder for the actual smoothing logic
            }
            return response;
        });
    }

    pplx::task<utility::string_t> GetFrequencyResponseUnitsAsync()
    {
        return GetAsync(U("/measurements/frequency-response/units"));
    }

    pplx::task<utility::string_t> GetFrequencyResponseSmoothingChoicesAsync()
    {
        return GetAsync(U("/measurements/frequency-response/smoothing-choices"));
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

    pplx::task<utility::string_t> GetFiltersImpulseResponseAsync(const utility::string_t& id, int samplerate, int length)
    {
        // Validate samplerate and length parameters
        if (samplerate <= 0)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'samplerate'. Must be > 0.")));
        }
        if (length <= 0 || length > 4194304)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Invalid value for parameter 'length'. Must be > 0 and <= 4194304.")));
        }

        utility::string_t endpoint = U("/measurements/") + id + U("/filters-impulse-response?samplerate=") + std::to_wstring(samplerate) + U("&length=") + std::to_wstring(length);
        return GetAsync(endpoint).then([](utility::string_t response) {
            // Parse the response and return the ImpulseResponse object
            json::value jsonResponse = json::value::parse(response);
            utility::string_t startTime = jsonResponse[U("startTime")].as_string();
            utility::string_t sampleInterval = jsonResponse[U("sampleInterval")].as_string();
            utility::string_t sampleRate = jsonResponse[U("sampleRate")].as_string();
            utility::string_t responseData = jsonResponse[U("responseData")].as_string();

            json::value impulseResponse;
            impulseResponse[U("startTime")] = json::value::string(startTime);
            impulseResponse[U("sampleInterval")] = json::value::string(sampleInterval);
            impulseResponse[U("sampleRate")] = json::value::string(sampleRate);
            impulseResponse[U("responseData")] = json::value::string(responseData);

            return impulseResponse.serialize();
        });
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
        return GetAsync(U("/eq/settings"));
    }

    pplx::task<utility::string_t> UpdateEQSettingsAsync(const json::value& content)
    {
        return PutAsync(U("/eq/settings"), content);
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
        return PostAsync(U("/subscribe"), content);
    }

    // Method to remove a subscription
    pplx::task<utility::string_t> UnsubscribeAsync(const json::value& content)
    {
        return PostAsync(U("/unsubscribe"), content);
    }

    // Method to get the list of subscribers
    pplx::task<utility::string_t> GetSubscribersAsync()
    {
        return GetAsync(U("/subscribers"));
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
        blockingModeEnabled = enable;
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
        return GetAsync(U("/audio/asio/device"));
    }

    pplx::task<utility::string_t> SetASIODeviceAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/device"), content);
    }

    pplx::task<utility::string_t> GetASIOInputAsync()
    {
        return GetAsync(U("/audio/asio/input"));
    }

    pplx::task<utility::string_t> SetASIOInputAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/input"), content);
    }

    pplx::task<utility::string_t> GetASIOOutputAsync()
    {
        return GetAsync(U("/audio/asio/output"));
    }

    pplx::task<utility::string_t> SetASIOOutputAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/output"), content);
    }

    pplx::task<utility::string_t> GetASIODevicesAsync()
    {
        return GetAsync(U("/audio/asio/devices"));
    }

    pplx::task<utility::string_t> GetASIOInputsAsync()
    {
        return GetAsync(U("/audio/asio/inputs"));
    }

    pplx::task<utility::string_t> GetASIOOutputsAsync()
    {
        return GetAsync(U("/audio/asio/outputs"));
    }

    pplx::task<utility::string_t> GetASIOTimingReferenceOutputChannelAsync()
    {
        return GetAsync(U("/audio/asio/timing-reference-output-channel"));
    }

    pplx::task<utility::string_t> SetASIOTimingReferenceOutputChannelAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/timing-reference-output-channel"), content);
    }

    pplx::task<utility::string_t> GetASIORefInputChannelAsync()
    {
        return GetAsync(U("/audio/asio/ref-input-channel"));
    }

    pplx::task<utility::string_t> SetASIORefInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/ref-input-channel"), content);
    }

    pplx::task<utility::string_t> GetASIOLastInputChannelAsync()
    {
        return GetAsync(U("/audio/asio/last-input-channel"));
    }

    pplx::task<utility::string_t> SetASIOLastInputChannelAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/last-input-channel"), content);
    }

    pplx::task<utility::string_t> SetASIOSecondaryOutputEnableAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/secondary-output-enable"), content);
    }

    pplx::task<utility::string_t> GetASIOSecondaryOutputAsync()
    {
        return GetAsync(U("/audio/asio/secondary-output"));
    }

    pplx::task<utility::string_t> SetASIOSecondaryOutputAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/secondary-output"), content);
    }

    pplx::task<utility::string_t> SendASIOCommandAsync(const json::value& content)
    {
        return PostAsync(U("/audio/asio/command"), content);
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

    pplx::task<utility::string_t> GetInputLevelUnitsAsync()
    {
        return GetAsync(U("http://localhost:4735/input-levels/units"));
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
        json::value commands = json::value::array();
        commands[0] = json::value::string(U("Load"));
        commands[1] = json::value::string(U("Save all"));
        commands[2] = json::value::string(U("Save"));
        commands[3] = json::value::string(U("Generate minimum phase version"));
        commands[4] = json::value::string(U("Generate excess phase version"));
        commands[5] = json::value::string(U("Generate Mic in box correction"));
        commands[6] = json::value::string(U("Make response copy"));
        commands[7] = json::value::string(U("Merge cal data to IR"));
        commands[8] = json::value::string(U("Trim IR to windows"));
        commands[9] = json::value::string(U("Smooth"));
        commands[10] = json::value::string(U("Generate waterfalls"));
        commands[11] = json::value::string(U("Generate spectrograms"));
        commands[12] = json::value::string(U("Estimate IR delay"));
        commands[13] = json::value::string(U("Offset t=0"));
        commands[14] = json::value::string(U("Add SPL offset"));
        commands[15] = json::value::string(U("Invert"));
        commands[16] = json::value::string(U("Generate RT60 data"));
        return pplx::task_from_result(commands.serialize());
    }

    pplx::task<utility::string_t> IssueMeasurementCommandAsync(const json::value& content)
    {
        utility::string_t command = content.at(U("command")).as_string();
        json::value parameters = content.at(U("parameters"));

        if (command == U("Load"))
        {
            for (const auto& param : parameters.as_array())
            {
                if (!param.is_string())
                {
                    return pplx::task_from_result(utility::string_t(U("Error: Invalid parameter for 'Load' command. Expected a string.")));
                }
            }
        }
        else if (command == U("Save all"))
        {
            if (parameters.size() < 1 || parameters.size() > 2)
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid number of parameters for 'Save all' command. Expected 1 or 2 parameters.")));
            }
            if (!parameters[0].is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid parameter for 'Save all' command. Expected a string for the file path.")));
            }
            if (parameters.size() == 2 && !parameters[1].is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Invalid parameter for 'Save all' command. Expected a string for the note.")));
            }
        }
        else if (command == U("Save"))
        {
            if (!parameters.has_field(U("filename")) || !parameters.at(U("filename")).is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'filename' parameter for 'Save' command. Expected a string.")));
            }
        }
        else if (command == U("Smooth"))
        {
            if (!parameters.has_field(U("smoothing")) || !parameters.at(U("smoothing")).is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.")));
            }
        }
        else
        {
            return pplx::task_from_result(utility::string_t(U("Error: Unknown command.")));
        }

        return PostAsync(U("http://localhost:4735/measurements/command"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/measurements/command"), content).then([response]() {
                return response;
            });
        });
    }

    // Method to update a measurement's name and/or notes
    pplx::task<utility::string_t> UpdateMeasurementAsync(const utility::string_t& id, const json::value& content)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::PUT, U("/measurements/") + id, content).then([this, id, content](http_response response) {
            return response.extract_string().then([this, id, content](utility::string_t response) {
                return NotifySubscribers(U("/measurements/") + id, content).then([response]() {
                    return response;
                });
            });
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

    pplx::task<utility::string_t> GetDistortionUnitsAsync()
    {
        return GetAsync(U("/measurements/distortion-units"));
    }

    pplx::task<utility::string_t> GetDistortionPPOChoicesAsync()
    {
        return GetAsync(U("/measurements/distortion-ppo-choices"));
    }

    // Method to handle the Smooth command
    pplx::task<utility::string_t> SmoothAsync(const utility::string_t& id, const utility::string_t& smoothing = U("1/3"))
    {
        json::value content;
        content[U("command")] = json::value::string(U("Smooth"));
        content[U("parameters")][U("smoothing")] = json::value::string(smoothing);

        utility::string_t endpoint = U("/measurements/") + id + U("/command");
        return PostAsync(endpoint, content).then([this, endpoint, content](utility::string_t response) {
            return NotifySubscribers(endpoint, content).then([response]() {
                return response;
            });
        });
    }

    // Methods for handling groups and their measurements
    pplx::task<utility::string_t> GetGroupsAsync()
    {
        return GetAsync(U("http://localhost:4735/groups"));
    }

    pplx::task<utility::string_t> GetGroupAsync(const utility::string_t& uuid)
    {
        return GetAsync(U("http://localhost:4735/groups/") + uuid);
    }

    pplx::task<utility::string_t> CreateGroupAsync(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/groups"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/groups"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> UpdateGroupAsync(const utility::string_t& uuid, const json::value& content)
    {
        return PutAsync(U("http://localhost:4735/groups/") + uuid, content).then([this, uuid, content](utility::string_t response) {
            return NotifySubscribers(U("/groups/") + uuid, content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> DeleteGroupAsync(const utility::string_t& uuid)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::DEL, U("/groups/") + uuid).then([this, uuid](http_response response) {
            return response.extract_string().then([this, uuid](utility::string_t response) {
                return NotifySubscribers(U("/groups/") + uuid, json::value::parse(U("{}"))).then([response]() {
                    return response;
                });
            });
        });
    }

    pplx::task<utility::string_t> DeleteAllGroupsAsync()
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::DEL, U("/groups")).then([this](http_response response) {
            return response.extract_string().then([this](utility::string_t response) {
                return NotifySubscribers(U("/groups"), json::value::parse(U("{}"))).then([response]() {
                    return response;
                });
            });
        });
    }

    pplx::task<utility::string_t> GetGroupMeasurementsAsync(const utility::string_t& uuid)
    {
        return GetAsync(U("http://localhost:4735/groups/") + uuid + U("/measurements"));
    }

    pplx::task<utility::string_t> AddMeasurementToGroupAsync(const utility::string_t& uuid, const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/groups/") + uuid + U("/measurements"), content).then([this, uuid, content](utility::string_t response) {
            return NotifySubscribers(U("/groups/") + uuid + U("/measurements"), content).then([response]() {
                return response;
            });
        });
    }

    pplx::task<utility::string_t> RemoveMeasurementFromGroupAsync(const utility::string_t& uuid, const utility::string_t& measurementUuid)
    {
        http_client client(U("http://localhost:4735"));
        return client.request(methods::DEL, U("/groups/") + uuid + U("/measurements/") + measurementUuid).then([this, uuid, measurementUuid](http_response response) {
            return response.extract_string().then([this, uuid, measurementUuid](utility::string_t response) {
                return NotifySubscribers(U("/groups/") + uuid + U("/measurements/") + measurementUuid, json::value::parse(U("{}"))).then([response]() {
                    return response;
                });
            });
        });
    }

    // Method to handle the Offset t=0 command
    pplx::task<utility::string_t> OffsetTZeroAsync(const utility::string_t& offset, const utility::string_t& unit)
    {
        json::value content;
        content[U("command")] = json::value::string(U("Offset t=0"));
        content[U("parameters")][U("offset")] = json::value::string(offset);
        content[U("parameters")][U("unit")] = json::value::string(unit);

        return PostAsync(U("/offset-t-zero"), content).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/offset-t-zero"), content).then([response]() {
                return response;
            });
        });
    }

    // Method to handle group change subscriptions
    pplx::task<utility::string_t> SubscribeToGroupChangesAsync(const json::value& content)
    {
        return PostAsync(U("/groups/subscribe"), content);
    }

    // Method to handle group change unsubscriptions
    pplx::task<utility::string_t> UnsubscribeFromGroupChangesAsync(const json::value& content)
    {
        return PostAsync(U("/groups/unsubscribe"), content);
    }

    // Method to get the list of group subscribers
    pplx::task<utility::string_t> GetGroupSubscribersAsync()
    {
        return GetAsync(U("/groups/subscribers"));
    }

    // Method to handle error subscriptions
    pplx::task<utility::string_t> SubscribeToErrorsAsync(const json::value& content)
    {
        return PostAsync(U("/application/subscribe-errors"), content);
    }

    // Method to handle warning subscriptions
    pplx::task<utility::string_t> SubscribeToWarningsAsync(const json::value& content)
    {
        return PostAsync(U("/application/subscribe-warnings"), content);
    }

    // Method to get the global option to treat 32-bit data as 24-bit
    pplx::task<utility::string_t> GetTreat32As24Async()
    {
        return GetAsync(U("http://localhost:4735/audio/configuration"));
    }

    // Method to set the global option to treat 32-bit data as 24-bit
    pplx::task<utility::string_t> SetTreat32As24Async(const json::value& content)
    {
        return PostAsync(U("http://localhost:4735/audio/configuration"), content);
    }

    // Method to check if audio is enabled
    pplx::task<utility::string_t> IsAudioEnabledAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/enabled"));
    }

    // Method to check if audio endpoints are ready for use
    pplx::task<utility::string_t> AreAudioEndpointsReadyAsync()
    {
        return GetAsync(U("http://localhost:4735/audio/endpoints-ready"));
    }

    // Method to start the API server
    pplx::task<utility::string_t> StartApiServerAsync()
    {
        return PostAsync(U("http://localhost:4735/application/command"), json::value::parse(U("{\"command\":\"start\"}")));
    }

    // Method to stop the API server
    pplx::task<utility::string_t> StopApiServerAsync()
    {
        return PostAsync(U("http://localhost:4735/application/command"), json::value::parse(U("{\"command\":\"stop\"}")));
    }

    // Method to specify a different port
    pplx::task<utility::string_t> SetPortAsync(int port)
    {
        if (port < 1024)
        {
            return pplx::task_from_result(utility::string_t(U("Error: Port numbers below 1024 will be ignored.")));
        }
        json::value content;
        content[U("port")] = json::value::number(port);
        return PostAsync(U("http://localhost:4735/application/command"), content);
    }

    // Method to run REW without a GUI
    pplx::task<utility::string_t> RunWithoutGuiAsync()
    {
        return PostAsync(U("http://localhost:4735/application/command"), json::value::parse(U("{\"command\":\"run\",\"args\":\"-nogui\"}")));
    }
};
