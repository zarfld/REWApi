using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

public class REWApi
{
    private static readonly HttpClient client = new HttpClient();
    private bool graphUpdateInhibited = false;
    private bool blockingModeEnabled = false;

    public void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    public async Task<string> GetAsync(string endpoint)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PutAsync(string endpoint, HttpContent content)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> PostAsync(string endpoint, HttpContent content)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        if (blockingModeEnabled)
        {
            await response.Content.LoadIntoBufferAsync();
        }
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> IssueMeasurementCommandWithParamsAsync(string id, string command, HttpContent parameters)
    {
        // Validate boolean parameters
        var booleanParams = new[] { "include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data" };
        foreach (var param in booleanParams)
        {
            if (parameters.Headers.Contains(param) && !bool.TryParse(parameters.Headers.GetValues(param).FirstOrDefault(), out _))
            {
                return $"Error: Invalid value for parameter '{param}'. Expected a boolean value.";
            }
        }

        // Validate numeric parameters
        if (parameters.Headers.Contains("lf tail slope") && double.TryParse(parameters.Headers.GetValues("lf tail slope").FirstOrDefault(), out var lfTailSlope) && lfTailSlope < 0)
        {
            return "Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0.";
        }
        if (parameters.Headers.Contains("hf tail slope") && double.TryParse(parameters.Headers.GetValues("hf tail slope").FirstOrDefault(), out var hfTailSlope) && hfTailSlope > 0)
        {
            return "Error: Invalid value for parameter 'hf tail slope'. Must be ≤ 0.";
        }

        // Validate required parameters
        if (bool.TryParse(parameters.Headers.GetValues("append lf tail").FirstOrDefault(), out var appendLfTail) && appendLfTail)
        {
            if (!parameters.Headers.Contains("lf tail start") || !parameters.Headers.Contains("lf tail slope"))
            {
                return "Error: Missing required parameters 'lf tail start' and/or 'lf tail slope'.";
            }
        }
        if (bool.TryParse(parameters.Headers.GetValues("append hf tail").FirstOrDefault(), out var appendHfTail) && appendHfTail)
        {
            if (!parameters.Headers.Contains("hf tail start") || !parameters.Headers.Contains("hf tail slope") || !parameters.Headers.Contains("frequency warping"))
            {
                return "Error: Missing required parameters 'hf tail start', 'hf tail slope', and/or 'frequency warping'.";
            }
        }
        if (!appendLfTail || !appendHfTail)
        {
            if (!parameters.Headers.Contains("replicate data"))
            {
                return "Error: Missing required parameter 'replicate data'.";
            }
        }

        var content = new StringContent("{\"command\":\"" + command + "\",\"parameters\":" + parameters.ReadAsStringAsync().Result + "}", Encoding.UTF8, "application/json");
        string endpoint = $"/measurements/{id}/command";
        return await PostAsync(endpoint, content);
    }

    public async Task<string> GenerateWaterfallAsync(string mode, int slices, HttpContent parameters)
    {
        var content = new StringContent("{\"command\":\"Generate waterfall\",\"parameters\":{\"mode\":\"" + mode + "\",\"slices\":" + slices + "," + parameters.ReadAsStringAsync().Result + "}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/waterfalls", content);
    }

    public async Task<string> GenerateSpectrogramAsync(string mode, int slices, HttpContent parameters)
    {
        var content = new StringContent("{\"command\":\"Generate spectrogram\",\"parameters\":{\"mode\":\"" + mode + "\",\"slices\":" + slices + "," + parameters.ReadAsStringAsync().Result + "}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/spectrograms", content);
    }

    // Methods for handling application settings
    public async Task<string> GetApplicationSettingsAsync()
    {
        return await GetAsync("http://localhost:4735/application/settings");
    }

    public async Task<string> UpdateApplicationSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/application/settings", content);
    }

    // Methods for handling audio settings
    public async Task<string> GetAudioSettingsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/settings");
    }

    public async Task<string> UpdateAudioSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/audio/settings", content);
    }

    // Methods for handling measurements
    public async Task<string> GetMeasurementsAsync()
    {
        return await GetAsync("http://localhost:4735/measurements");
    }

    public async Task<string> AddMeasurementAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/measurements", content);
    }

    // Methods for handling frequency response
    public async Task<string> GetFrequencyResponseAsync(string id, string unit = "SPL", string smoothing = "default", int ppo = 0)
    {
        string endpoint = $"/measurements/{id}/frequency-response?unit={unit}&smoothing={smoothing}";
        if (ppo > 0)
        {
            endpoint += $"&ppo={ppo}";
        }
        var response = await GetAsync(endpoint);
        if (ppo > 0 && smoothing == "default")
        {
            // Apply smoothing to ppo/2 if greater smoothing has not already been applied
            // This is a placeholder for the actual smoothing logic
            // This is a placeholder for the actual smoothing logic
        }
        return response;
    }

    public async Task<string> GetFrequencyResponseUnitsAsync()
    {
        return await GetAsync("/measurements/frequency-response/units");
    }

    public async Task<string> GetFrequencyResponseSmoothingChoicesAsync()
    {
        return await GetAsync("/measurements/frequency-response/smoothing-choices");
    }

    // Methods for handling impulse response
    public async Task<string> GetImpulseResponseAsync(string id, string unit = "Percent", bool windowed = false, bool normalised = true)
    {
        string endpoint = $"/measurements/{id}/impulse-response?unit={unit}";
        if (windowed)
        {
            endpoint += "&windowed=true";
        }
        if (!normalised)
        {
            endpoint += "&normalised=false";
        }
        return await GetAsync(endpoint);
    }

    // Methods for handling EQ and filters
    public async Task<string> GetEQSettingsAsync()
    {
        return await GetAsync("http://localhost:4735/eq/settings");
    }

    public async Task<string> UpdateEQSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/eq/settings", content);
    }

    // Methods for handling SPL meter
    public async Task<string> GetSPLMeterAsync()
    {
        return await GetAsync("http://localhost:4735/splmeter");
    }

    // Methods for handling RTA
    public async Task<string> GetRTAAsync()
    {
        return await GetAsync("http://localhost:4735/rta");
    }

    // Methods for handling room simulator
    public async Task<string> GetRoomSimulatorAsync()
    {
        return await GetAsync("http://localhost:4735/roomsimulator");
    }

    // Method for handling shutdown command
    public async Task<string> ShutdownAsync()
    {
        var content = new StringContent("{\"command\":\"shutdown\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/command", content);
    }

    public string EncodeArray(float[] array)
    {
        byte[] bytes = new byte[array.Length * sizeof(float)];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return Convert.ToBase64String(bytes);
    }

    public float[] DecodeArray(string base64Encoded)
    {
        byte[] bytes = Convert.FromBase64String(base64Encoded);
        float[] array = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
        return array;
    }

    // Method to add a subscription
    public async Task<string> SubscribeAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/subscribe", content);
    }

    // Method to remove a subscription
    public async Task<string> UnsubscribeAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/unsubscribe", content);
    }

    // Method to get the list of subscribers
    public async Task<string> GetSubscribersAsync()
    {
        return await GetAsync("http://localhost:4735/subscribers");
    }

    // Methods for handling errors and warnings
    public async Task<string> GetErrorsAsync()
    {
        return await GetAsync("http://localhost:4735/application/errors");
    }

    public async Task<string> GetLastErrorAsync()
    {
        return await GetAsync("http://localhost:4735/application/last-error");
    }

    public async Task<string> GetWarningsAsync()
    {
        return await GetAsync("http://localhost:4735/application/warnings");
    }

    public async Task<string> GetLastWarningAsync()
    {
        return await GetAsync("http://localhost:4735/application/last-warning");
    }

    // Methods for enabling and disabling blocking mode
    public async Task<string> SetBlockingModeAsync(bool enable)
    {
        blockingModeEnabled = enable;
        var content = new StringContent("{\"blocking\":" + enable.ToString().ToLower() + "}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/blocking", content);
    }

    // Methods for handling graph update inhibition
    public async Task<string> SetGraphUpdateInhibitionAsync(bool inhibit)
    {
        var content = new StringContent("{\"inhibit\":" + inhibit.ToString().ToLower() + "}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/inhibit-graph-updates", content);
    }

    // Methods for handling audio driver selection
    public async Task<string> GetAudioDriverAsync()
    {
        return await GetAsync("http://localhost:4735/audio/driver");
    }

    public async Task<string> SetAudioDriverAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/driver", content);
    }

    // Methods for handling sample rate
    public async Task<string> GetSampleRateAsync()
    {
        return await GetAsync("http://localhost:4735/audio/samplerate");
    }

    public async Task<string> SetSampleRateAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/samplerate", content);
    }

    // Methods for handling input calibration
    public async Task<string> GetInputCalibrationAsync()
    {
        return await GetAsync("http://localhost:4735/audio/input-cal");
    }

    public async Task<string> SetInputCalibrationAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/audio/input-cal", content);
    }

    // Methods for handling output calibration
    public async Task<string> GetOutputCalibrationAsync()
    {
        return await GetAsync("http://localhost:4735/audio/output-cal");
    }

    public async Task<string> SetOutputCalibrationAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/audio/output-cal", content);
    }

    // Methods for handling Java driver items
    public async Task<string> GetJavaInputDeviceAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/input-device");
    }

    public async Task<string> SetJavaInputDeviceAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/input-device", content);
    }

    public async Task<string> GetJavaInputAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/input");
    }

    public async Task<string> SetJavaInputAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/input", content);
    }

    public async Task<string> GetJavaOutputDeviceAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/output-device");
    }

    public async Task<string> SetJavaOutputDeviceAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/output-device", content);
    }

    public async Task<string> GetJavaOutputAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/output");
    }

    public async Task<string> SetJavaOutputAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/output", content);
    }

    public async Task<string> GetJavaInputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/input-channel");
    }

    public async Task<string> SetJavaInputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/input-channel", content);
    }

    public async Task<string> GetJavaOutputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/output-channel");
    }

    public async Task<string> SetJavaOutputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/output-channel", content);
    }

    public async Task<string> GetJavaRefInputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/ref-input-channel");
    }

    public async Task<string> SetJavaRefInputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/ref-input-channel", content);
    }

    public async Task<string> GetJavaLastInputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/last-input-channel");
    }

    public async Task<string> SetJavaLastInputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/last-input-channel", content);
    }

    public async Task<string> GetJavaRefOutputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/ref-output-channel");
    }

    public async Task<string> SetJavaRefOutputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/ref-output-channel", content);
    }

    public async Task<string> GetJavaNumInputChannelsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/num-input-channels");
    }

    public async Task<string> GetJavaOutputChannelsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/output-channels");
    }

    public async Task<string> GetJavaRefOutputChannelsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/ref-output-channels");
    }

    public async Task<string> GetJavaOutputChannelMappingAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/output-channel-mapping");
    }

    public async Task<string> SetJavaOutputChannelMappingAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/output-channel-mapping", content);
    }

    public async Task<string> GetJavaStereoOnlyAsync()
    {
        return await GetAsync("http://localhost:4735/audio/java/stereo-only");
    }

    public async Task<string> SetJavaStereoOnlyAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/java/stereo-only", content);
    }

    // Methods for handling ASIO driver items
    public async Task<string> GetASIODeviceAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/device");
    }

    public async Task<string> SetASIODeviceAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/device", content);
    }

    public async Task<string> GetASIOInputAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/input");
    }

    public async Task<string> SetASIOInputAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/input", content);
    }

    public async Task<string> GetASIOOutputAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/output");
    }

    public async Task<string> SetASIOOutputAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/output", content);
    }

    public async Task<string> GetASIODevicesAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/devices");
    }

    public async Task<string> GetASIOInputsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/inputs");
    }

    public async Task<string> GetASIOOutputsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/outputs");
    }

    public async Task<string> GetASIOTimingReferenceOutputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/timing-reference-output-channel");
    }

    public async Task<string> SetASIOTimingReferenceOutputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/timing-reference-output-channel", content);
    }

    public async Task<string> GetASIORefInputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/ref-input-channel");
    }

    public async Task<string> SetASIORefInputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/ref-input-channel", content);
    }

    public async Task<string> GetASIOLastInputChannelAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/last-input-channel");
    }

    public async Task<string> SetASIOLastInputChannelAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/last-input-channel", content);
    }

    public async Task<string> SetASIOSecondaryOutputEnableAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/secondary-output-enable", content);
    }

    public async Task<string> GetASIOSecondaryOutputAsync()
    {
        return await GetAsync("http://localhost:4735/audio/asio/secondary-output");
    }

    public async Task<string> SetASIOSecondaryOutputAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/secondary-output", content);
    }

    public async Task<string> SendASIOCommandAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/asio/command", content);
    }

    // Methods for handling input levels
    public async Task<string> StartMonitoringInputLevelsAsync()
    {
        var content = new StringContent("{\"command\":\"start\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/input-levels/command", content);
    }

    public async Task<string> StopMonitoringInputLevelsAsync()
    {
        var content = new StringContent("{\"command\":\"stop\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/input-levels/command", content);
    }

    public async Task<string> SubscribeInputLevelsAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/input-levels/subscribe", content);
    }

    public async Task<string> UnsubscribeInputLevelsAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/input-levels/unsubscribe", content);
    }

    public async Task<string> GetLastInputLevelsAsync()
    {
        return await GetAsync("http://localhost:4735/input-levels/last-levels");
    }

    public async Task<string> GetInputLevelUnitsAsync()
    {
        return await GetAsync("http://localhost:4735/input-levels/units");
    }

    // Methods for handling selected measurement
    public async Task<string> SelectMeasurementByUUIDAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/measurements/selected-uuid", content);
    }

    public async Task<string> SelectMeasurementByIndexAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/measurements/selected", content);
    }

    public async Task<string> GetSelectedMeasurementUUIDAsync()
    {
        return await GetAsync("http://localhost:4735/measurements/selected-uuid");
    }

    public async Task<string> GetSelectedMeasurementIndexAsync()
    {
        return await GetAsync("http://localhost:4735/measurements/selected");
    }

    // Methods for handling measurement commands
    public async Task<string> GetMeasurementCommandsAsync()
    {
        var commands = new[] { "Load", "Save all", "Save", "Generate minimum phase version", "Generate excess phase version", "Generate Mic in box correction", "Make response copy", "Merge cal data to IR", "Trim IR to windows", "Smooth", "Generate waterfalls", "Generate spectrograms", "Estimate IR delay", "Offset t=0", "Add SPL offset", "Invert", "Generate RT60 data" };
        return await Task.FromResult(string.Join(",", commands));
    }

    public async Task<string> IssueMeasurementCommandAsync(HttpContent content)
    {
        var command = content.Headers.GetValues("command").FirstOrDefault();
        var parameters = content.Headers.GetValues("parameters").FirstOrDefault();

        if (command == "Load")
        {
            var paramArray = parameters.Split(',');
            foreach (var param in paramArray)
            {
                if (string.IsNullOrEmpty(param))
                {
                    return "Error: Invalid parameter for 'Load' command. Expected a string.";
                }
            }
        }
        else if (command == "Save all")
        {
            var paramArray = parameters.Split(',');
            if (paramArray.Length < 1 || paramArray.Length > 2)
            {
                return "Error: Invalid number of parameters for 'Save all' command. Expected 1 or 2 parameters.";
            }
            if (string.IsNullOrEmpty(paramArray[0]))
            {
                return "Error: Invalid parameter for 'Save all' command. Expected a string for the file path.";
            }
            if (paramArray.Length == 2 && string.IsNullOrEmpty(paramArray[1]))
            {
                return "Error: Invalid parameter for 'Save all' command. Expected a string for the note.";
            }
        }
        else if (command == "Save")
        {
            if (!parameters.Contains("filename") || string.IsNullOrEmpty(parameters["filename"]))
            {
                return "Error: Missing or invalid 'filename' parameter for 'Save' command. Expected a string.";
            }
        }
        else if (command == "Smooth")
        {
            if (!parameters.Contains("smoothing") || string.IsNullOrEmpty(parameters["smoothing"]))
            {
                return "Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.";
            }
        }
        else
        {
            return "Error: Unknown command.";
        }

        return await PostAsync("http://localhost:4735/measurements/command", content);
    }

    // Method to update a measurement's name and/or notes
    // This method uses a MeasurementSummary object with the title and/or notes field set to the new value
    public async Task<string> UpdateMeasurementAsync(string id, HttpContent content)
    {
        return await PutAsync($"http://localhost:4735/measurements/{id}", content);
    }

    // Methods for handling IR window settings
    public async Task<string> GetIRWindowSettingsAsync(string id)
    {
        return await GetAsync($"http://localhost:4735/measurements/{id}/ir-windows");
    }

    public async Task<string> UpdateIRWindowSettingsAsync(string id, HttpContent content)
    {
        return await PutAsync($"http://localhost:4735/measurements/{id}/ir-windows", content);
    }

    public async Task<string> GetLeftWindowTypeAsync(string id)
    {
        var response = await GetIRWindowSettingsAsync(id);
        var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
        return jsonResponse["leftWindowType"].ToString();
    }

    public async Task<string> UpdateLeftWindowTypeAsync(string id, string leftWindowType)
    {
        var response = await GetIRWindowSettingsAsync(id);
        var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
        jsonResponse["leftWindowType"] = leftWindowType;
        var content = new StringContent(jsonResponse.ToString(), Encoding.UTF8, "application/json");
        return await UpdateIRWindowSettingsAsync(id, content);
    }

    public async Task<string> GetRightWindowTypeAsync(string id)
    {
        var response = await GetIRWindowSettingsAsync(id);
        var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
        return jsonResponse["rightWindowType"].ToString();
    }

    public async Task<string> UpdateRightWindowTypeAsync(string id, string rightWindowType)
    {
        var response = await GetIRWindowSettingsAsync(id);
        var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
        jsonResponse["rightWindowType"] = rightWindowType;
        var content = new StringContent(jsonResponse.ToString(), Encoding.UTF8, "application/json");
        return await UpdateIRWindowSettingsAsync(id, content);
    }

    // Methods for handling REW View preferences
    public async Task<string> GetREWViewPreferencesAsync()
    {
        return await GetAsync("http://localhost:4735/application/settings");
    }

    // Method to get distortion data
    public async Task<string> GetDistortionAsync(string id, string unit = "percent", int ppo = 3)
    {
        string endpoint = $"/measurements/{id}/distortion?unit={unit}&ppo={ppo}";
        return await GetAsync(endpoint);
    }

    // Method to process measurements
    public async Task<string> ProcessMeasurementsAsync(HttpContent content)
    {
        // Check if the processName is "Align SPL"
        if (content.Headers.Contains("processName") && content.Headers.GetValues("processName").FirstOrDefault() == "Align SPL")
        {
            // Validate required parameters
            if (!content.Headers.Contains("parameters") || !content.Headers.Contains("targetdB") || !content.Headers.Contains("frequencyHz") || !content.Headers.Contains("spanOctaves"))
            {
                return "Error: Missing required parameters 'targetdB', 'frequencyHz', and/or 'spanOctaves'.";
            }

            // Validate measurementIndices field
            if (!content.Headers.Contains("measurementIndices") || !content.Headers.GetValues("measurementIndices").FirstOrDefault().StartsWith("["))
            {
                return "Error: Missing or invalid 'measurementIndices' field. Must be an array.";
            }
        }

        // Check if the processName is "Arithmetic"
        if (content.Headers.Contains("processName") && content.Headers.GetValues("processName").FirstOrDefault() == "Arithmetic")
        {
            // Validate required parameters
            if (!content.Headers.Contains("parameters") || !content.Headers.Contains("function"))
            {
                return "Error: Missing required parameter 'function'.";
            }

            // Validate optional parameters
            if (content.Headers.Contains("maxGain") && !double.TryParse(content.Headers.GetValues("maxGain").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'maxGain'. Expected a number.";
            }
            if (content.Headers.Contains("lowerLimit") && !double.TryParse(content.Headers.GetValues("lowerLimit").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'lowerLimit'. Expected a number.";
            }
            if (content.Headers.Contains("upperLimit") && !double.TryParse(content.Headers.GetValues("upperLimit").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'upperLimit'. Expected a number.";
            }
            if (content.Headers.Contains("mergeFrequency") && !double.TryParse(content.Headers.GetValues("mergeFrequency").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'mergeFrequency'. Expected a number.";
            }
            if (content.Headers.Contains("mergeBlend") && !bool.TryParse(content.Headers.GetValues("mergeBlend").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'mergeBlend'. Expected a boolean.";
            }
            if (content.Headers.Contains("targetLevel") && !double.TryParse(content.Headers.GetValues("targetLevel").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'targetLevel'. Expected a number.";
            }
            if (content.Headers.Contains("autoTarget") && !bool.TryParse(content.Headers.GetValues("autoTarget").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'autoTarget'. Expected a boolean.";
            }
            if (content.Headers.Contains("excludeNotches") && !bool.TryParse(content.Headers.GetValues("excludeNotches").FirstOrDefault(), out _))
            {
                return "Error: Invalid value for parameter 'excludeNotches'. Expected a boolean.";
            }

            // Validate measurementIndices field
            if (!content.Headers.Contains("measurementIndices") || !content.Headers.GetValues("measurementIndices").FirstOrDefault().StartsWith("["))
            {
                return "Error: Missing or invalid 'measurementIndices' field. Must be an array.";
            }
        }

        return await PostAsync("http://localhost:4735/measurements/process-measurements", content);
    }

    // Method to handle the Smooth command
    public async Task<string> SmoothAsync(string id, string smoothing = "1/3")
    {
        var content = new StringContent("{\"command\":\"Smooth\",\"parameters\":{\"smoothing\":\"" + smoothing + "\"}}", Encoding.UTF8, "application/json");
        string endpoint = $"/measurements/{id}/command";
        return await PostAsync(endpoint, content);
    }

    // Methods for handling groups and their measurements
    public async Task<string> GetGroupsAsync()
    {
        return await GetAsync("http://localhost:4735/groups");
    }

    public async Task<string> GetGroupAsync(string uuid)
    {
        return await GetAsync($"http://localhost:4735/groups/{uuid}");
    }

    public async Task<string> CreateGroupAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/groups", content);
    }

    public async Task<string> UpdateGroupAsync(string uuid, HttpContent content)
    {
        return await PutAsync($"http://localhost:4735/groups/{uuid}", content);
    }

    public async Task<string> DeleteGroupAsync(string uuid)
    {
        return await DeleteAsync($"http://localhost:4735/groups/{uuid}");
    }

    public async Task<string> DeleteAllGroupsAsync()
    {
        return await DeleteAsync("http://localhost:4735/groups");
    }

    public async Task<string> GetGroupMeasurementsAsync(string uuid)
    {
        return await GetAsync($"http://localhost:4735/groups/{uuid}/measurements");
    }

    public async Task<string> AddMeasurementToGroupAsync(string uuid, HttpContent content)
    {
        return await PostAsync($"http://localhost:4735/groups/{uuid}/measurements", content);
    }

    public async Task<string> RemoveMeasurementFromGroupAsync(string uuid, string measurementUuid)
    {
        return await DeleteAsync($"http://localhost:4735/groups/{uuid}/measurements/{measurementUuid}");
    }

    private async Task<string> DeleteAsync(string endpoint)
    {
        HttpResponseMessage response = await client.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    // Method to handle the Offset t=0 command
    public async Task<string> OffsetTZeroAsync(string offset, string unit)
    {
        var content = new StringContent("{\"command\":\"Offset t=0\",\"parameters\":{\"offset\":\"" + offset + "\",\"unit\":\"" + unit + "\"}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/offset-t-zero", content);
    }

    // Method to handle group change subscriptions
    public async Task<string> SubscribeToGroupChangesAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/groups/subscribe", content);
    }

    // Method to handle group change unsubscriptions
    public async Task<string> UnsubscribeFromGroupChangesAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/groups/unsubscribe", content);
    }

    // Method to get the list of group subscribers
    public async Task<string> GetGroupSubscribersAsync()
    {
        return await GetAsync("http://localhost:4735/groups/subscribers");
    }

    // Method to handle error subscriptions
    public async Task<string> SubscribeToErrorsAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/application/subscribe-errors", content);
    }

    // Method to handle warning subscriptions
    public async Task<string> SubscribeToWarningsAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/application/subscribe-warnings", content);
    }

    // Method to get the global option to treat 32-bit data as 24-bit
    public async Task<string> GetTreat32As24Async()
    {
        return await GetAsync("http://localhost:4735/audio/configuration");
    }

    // Method to set the global option to treat 32-bit data as 24-bit
    public async Task<string> SetTreat32As24Async(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/audio/configuration", content);
    }

    // Method to check if audio is enabled
    public async Task<string> IsAudioEnabledAsync()
    {
        return await GetAsync("http://localhost:4735/audio/enabled");
    }

    // Method to check if audio endpoints are ready for use
    public async Task<string> AreAudioEndpointsReadyAsync()
    {
        return await GetAsync("http://localhost:4735/audio/endpoints-ready");
    }

    // Method to start the API server
    public async Task<string> StartApiServerAsync()
    {
        var content = new StringContent("{\"command\":\"start\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/command", content);
    }

    // Method to stop the API server
    public async Task<string> StopApiServerAsync()
    {
        var content = new StringContent("{\"command\":\"stop\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/command", content);
    }

    // Method to specify a different port
    public async Task<string> SetPortAsync(int port)
    {
        if (port < 1024)
        {
            return "Error: Port numbers below 1024 will be ignored.";
        }
        var content = new StringContent("{\"port\":" + port + "}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/command", content);
    }

    // Method to run REW without a GUI
    public async Task<string> RunWithoutGuiAsync()
    {
        var content = new StringContent("{\"command\":\"run\",\"args\":\"-nogui\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/application/command", content);
    }
}
