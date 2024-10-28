using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

public class REWApi
{
    private static readonly HttpClient client = new HttpClient();
    private bool graphUpdateInhibited = false;

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
        return await GetAsync(endpoint);
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
        return await GetAsync("http://localhost:4735/measurements/commands");
    }

    public async Task<string> IssueMeasurementCommandAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/measurements/command", content);
    }

    // Method to update a measurement's name and/or notes
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
}
