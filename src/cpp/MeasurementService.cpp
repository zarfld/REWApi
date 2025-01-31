#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <string>
#include <vector>
#include <algorithm>
#include "RewClient.h"
#include "RewRequest.h"

using namespace web;
using namespace web::http;
using namespace web::http::client;
using namespace pplx;

class MeasurementService
{
public:
    MeasurementService(RewClient& client) : client_(client) {}

    task<utility::string_t> IssueMeasurementCommandWithParamsAsync(const utility::string_t& id, const utility::string_t& command, const json::value& parameters)
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

        if (command == U("Smooth"))
        {
            if (!parameters.has_field(U("smoothing")) || !parameters.at(U("smoothing")).is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.")));
            }
        }

        if (command == U("Offset t=0"))
        {
            if (!parameters.has_field(U("offset")) || !parameters.at(U("offset")).is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'offset' parameter for 'Offset t=0' command. Expected a string.")));
            }
            if (!parameters.has_field(U("unit")) || !parameters.at(U("unit")).is_string())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'unit' parameter for 'Offset t=0' command. Expected a string.")));
            }
        }

        if (command == U("Generate RT60"))
        {
            if (!parameters.has_field(U("octaveFrac")) || !parameters.at(U("octaveFrac")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'octaveFrac' parameter for 'Generate RT60' command. Expected a number.")));
            }
            if (!parameters.has_field(U("filterOrder")) || !parameters.at(U("filterOrder")).is_number())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'filterOrder' parameter for 'Generate RT60' command. Expected a number.")));
            }
            if (!parameters.has_field(U("zeroPhaseFiltered")) || !parameters.at(U("zeroPhaseFiltered")).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'zeroPhaseFiltered' parameter for 'Generate RT60' command. Expected a boolean.")));
            }
            if (!parameters.has_field(U("reverseFiltered")) || !parameters.at(U("reverseFiltered")).is_boolean())
            {
                return pplx::task_from_result(utility::string_t(U("Error: Missing or invalid 'reverseFiltered' parameter for 'Generate RT60' command. Expected a boolean.")));
            }
        }

        json::value content;
        content[U("command")] = json::value::string(command);
        content[U("parameters")] = parameters;

        RewRequest request(U("/measurements/") + id + U("/command"), methods::POST, content);
        return client_.SendRequestAsync(request).then([this, request, content](utility::string_t response) {
            return NotifySubscribers(request.endpoint(), content).then([response]() {
                return response;
            });
        });
    }

    task<utility::string_t> GenerateWaterfallAsync(const utility::string_t& mode, int slices, const json::value& parameters)
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

        RewRequest request(U("/waterfalls"), methods::POST, content);
        return client_.SendRequestAsync(request).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/waterfalls"), content).then([response]() {
                return response;
            });
        });
    }

    task<utility::string_t> GenerateSpectrogramAsync(const utility::string_t& mode, int slices, const json::value& parameters)
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

        RewRequest request(U("/spectrograms"), methods::POST, content);
        return client_.SendRequestAsync(request).then([this, content](utility::string_t response) {
            return NotifySubscribers(U("/spectrograms"), content).then([response]() {
                return response;
            });
        });
    }

private:
    task<utility::string_t> NotifySubscribers(const utility::string_t& endpoint, const json::value& content)
    {
        return GetSubscribersAsync().then([this, endpoint, content](utility::string_t subscribers) {
            json::value subscribersJson = json::value::parse(subscribers);
            std::vector<pplx::task<void>> tasks;
            for (const auto& subscriber : subscribersJson.as_array())
            {
                utility::string_t url = subscriber.at(U("url")).as_string();
                RewRequest request(url + endpoint, methods::POST, content);
                tasks.push_back(client_.SendRequestAsync(request).then([](utility::string_t response) {
                    // Handle response if needed
                }));
            }
            return pplx::when_all(tasks.begin(), tasks.end()).then([] {
                return utility::string_t(U("Notification sent to all subscribers."));
            });
        });
    }

    task<utility::string_t> GetSubscribersAsync()
    {
        RewRequest request(U("/subscribers"), methods::GET);
        return client_.SendRequestAsync(request);
    }

    RewClient& client_;
};
