using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using REWApi.Domain;

namespace REWApi;

public class REWApi
{
    private static readonly HttpClient client = new HttpClient();
    private bool graphUpdateInhibited = false;
    private bool blockingModeEnabled = false;

    public void SetGraphUpdateInhibition(bool inhibit)
    {
        graphUpdateInhibited = inhibit;
    }

    public async Task<string?> GetAsync(string endpoint)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string?> PutAsync(string endpoint, HttpContent content)
    {
        if (graphUpdateInhibited) return null;
        HttpResponseMessage response = await client.PutAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string?> PostAsync(string endpoint, HttpContent content)
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

    /// <summary>
    /// Issues a measurement command with parameters using simple objects (matches JS/TS API)
    /// Following REQ-F-MEASURE-001.2 for measurement command operations
    /// </summary>
    /// <param name="id">Measurement ID</param>
    /// <param name="command">Command to execute</param>
    /// <param name="parameters">Parameters as simple object (same structure as JS/TS)</param>
    /// <returns>API response</returns>
    public async Task<string> IssueMeasurementCommandWithParams(string id, string command, object? parameters = null)
    {
        // Create request payload (exactly like JS/TS)
        var requestPayload = new
        {
            command = command,
            parameters = parameters ?? new { }
        };

        var json = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        string endpoint = $"/measurements/{id}/command";
        return await PostAsync(endpoint, content);
    }

    /// <summary>
    /// Legacy method - Issues a measurement command with HttpContent parameters
    /// DEPRECATED: Use IssueMeasurementCommandWithParams with object parameters instead
    /// This method is kept for backward compatibility but exposes HTTP implementation details
    /// </summary>
    [Obsolete("Use IssueMeasurementCommandWithParams with object parameters instead")]
    public async Task<string> IssueMeasurementCommandWithParamsAsync(string id, string command, HttpContent parameters)
    {
        // Convert HttpContent to legacy implementation for backward compatibility
        var parametersJson = await parameters.ReadAsStringAsync();
        var content = new StringContent($"{{\"command\":\"{command}\",\"parameters\":{parametersJson}}}", Encoding.UTF8, "application/json");
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
        // Validate the "mode" parameter
        var validModes = new[] { "Fourier", "Wavelet", "Airy CWT", "Morlet CWT", "Burst decay" };
        if (!validModes.Contains(mode))
        {
            return "Error: Invalid value for parameter 'mode'.";
        }

        // Validate the "slices" parameter
        if (slices <= 0)
        {
            return "Error: Invalid value for parameter 'slices'. Must be > 0.";
        }

        // Validate the "ppo" parameter
        if (!parameters.Headers.Contains("ppo") || !int.TryParse(parameters.Headers.GetValues("ppo").FirstOrDefault(), out _))
        {
            return "Error: Missing or invalid 'ppo' parameter. Expected a number.";
        }

        // Validate the "amplitude" parameter
        if (!parameters.Headers.Contains("amplitude") || string.IsNullOrEmpty(parameters.Headers.GetValues("amplitude").FirstOrDefault()))
        {
            return "Error: Missing or invalid 'amplitude' parameter. Expected a string.";
        }

        // Validate additional parameters required for different modes
        if (mode == "Fourier")
        {
            var requiredParams = new[] { "window type", "window width ms", "before ms", "after ms" };
            foreach (var param in requiredParams)
            {
                if (!parameters.Headers.Contains(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Fourier' mode.";
                }
            }
        }
        else if (mode == "Burst decay")
        {
            var requiredParams = new[] { "bandwidth", "periods" };
            foreach (var param in requiredParams)
            {
                if (!parameters.Headers.Contains(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Burst decay' mode.";
                }
            }
        }
        else if (mode == "Wavelet" || mode == "Airy CWT" || mode == "Morlet CWT")
        {
            var requiredParams = new[] { "bandwidth", "before ms", "after ms" };
            foreach (var param in requiredParams)
            {
                if (!parameters.Headers.Contains(param))
                {
                    return $"Error: Missing required parameter '{param}' for '{mode}' mode.";
                }
            }
        }

        var content = new StringContent("{\"command\":\"Generate spectrogram\",\"parameters\":{\"mode\":\"" + mode + "\",\"slices\":" + slices + "," + parameters.ReadAsStringAsync().Result + "}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/spectrograms", content);
    }

    // Methods for handling application settings
    public async Task<string> GetApplicationSettingsAsync()
    {
        return await GetAsync("http://localhost:4735/application/settings");
    }

    /// <summary>
    /// Updates application settings using simple objects (matches JS/TS API)
    /// Following REQ-F-CONFIG-001 for application configuration
    /// </summary>
    /// <param name="data">Settings data as simple object (same structure as JS/TS)</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateApplicationSettings(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync("http://localhost:4735/application/settings", content);
    }

    /// <summary>
    /// Legacy method - Updates application settings with HttpContent
    /// DEPRECATED: Use UpdateApplicationSettings with object data instead
    /// </summary>
    [Obsolete("Use UpdateApplicationSettings with object data instead")]
    public async Task<string> UpdateApplicationSettingsAsync(ApplicationSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync("http://localhost:4735/application/settings", content);
    }

    /// <summary>
    /// Legacy method - Updates application settings with HttpContent
    /// DEPRECATED: Use UpdateApplicationSettings with object data instead
    /// </summary>
    [Obsolete("Use UpdateApplicationSettings with object data instead")]
    public async Task<string> UpdateApplicationSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/application/settings", content);
    }

    // Methods for handling audio settings
    public async Task<string> GetAudioSettingsAsync()
    {
        return await GetAsync("http://localhost:4735/audio/settings");
    }

    /// <summary>
    /// Updates audio settings using simple objects (matches JS/TS API)
    /// Following REQ-F-AUDIO-001 for audio configuration
    /// </summary>
    /// <param name="data">Audio settings data as simple object (same structure as JS/TS)</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateAudioSettings(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync("http://localhost:4735/audio/settings", content);
    }

    /// <summary>
    /// Legacy method - Updates audio settings with HttpContent
    /// DEPRECATED: Use UpdateAudioSettings with object data instead
    /// </summary>
    [Obsolete("Use UpdateAudioSettings with object data instead")]
    public async Task<string> UpdateAudioSettingsAsync(AudioSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync("http://localhost:4735/audio/settings", content);
    }

    /// <summary>
    /// Legacy method - Updates audio settings with HttpContent
    /// DEPRECATED: Use UpdateAudioSettings with object data instead
    /// </summary>
    [Obsolete("Use UpdateAudioSettings with object data instead")]
    public async Task<string> UpdateAudioSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/audio/settings", content);
    }

    // Methods for handling measurements
    public async Task<string> GetMeasurementsAsync()
    {
        return await GetAsync("http://localhost:4735/measurements");
    }

    /// <summary>
    /// Adds a new measurement using simple objects (matches JS/TS API)
    /// Following REQ-F-MEASURE-001 for measurement creation
    /// </summary>
    /// <param name="data">Measurement data as simple object (same structure as JS/TS)</param>
    /// <returns>API response</returns>
    public async Task<string> AddMeasurement(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/measurements", content);
    }

    /// <summary>
    /// Legacy method - Adds a measurement with HttpContent
    /// DEPRECATED: Use AddMeasurement with object data instead
    /// </summary>
    [Obsolete("Use AddMeasurement with object data instead")]
    public async Task<string> AddMeasurementAsync(MeasurementData measurement)
    {
        var json = JsonSerializer.Serialize(measurement);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/measurements", content);
    }

    /// <summary>
    /// Legacy method - Adds a measurement with HttpContent
    /// DEPRECATED: Use AddMeasurement with object data instead
    /// </summary>
    [Obsolete("Use AddMeasurement with object data instead")]
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

    /// <summary>
    /// Updates EQ settings using simple objects (matches JS/TS API)
    /// Following REQ-F-EQ-001.1 for equalizer functionality
    /// </summary>
    /// <param name="data">EQ settings data as simple object (same structure as JS/TS)</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateEQSettings(object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PutAsync("http://localhost:4735/eq/settings", content);
    }

    /// <summary>
    /// Legacy method - Updates EQ settings with HttpContent
    /// DEPRECATED: Use UpdateEQSettings with object data instead
    /// </summary>
    [Obsolete("Use UpdateEQSettings with object data instead")]
    public async Task<string> UpdateEQSettingsAsync(HttpContent content)
    {
        return await PutAsync("http://localhost:4735/eq/settings", content);
    }

    public async Task<string> GetMeasurementEqualiserAsync(string id)
    {
        return await GetAsync($"http://localhost:4735/measurements/{id}/equaliser");
    }

    /// <summary>
    /// Updates measurement equalizer using domain objects
    /// Following REQ-F-EQ-001.1 for measurement equalization
    /// </summary>
    /// <param name="id">Measurement ID</param>
    /// <param name="settings">Strongly-typed EQ settings</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateMeasurementEqualizerAsync(string id, EQSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync($"http://localhost:4735/measurements/{id}/equaliser", content);
    }

    /// <summary>
    /// Legacy method - Updates measurement equalizer with HttpContent
    /// DEPRECATED: Use UpdateMeasurementEqualizerAsync with EQSettings instead
    /// </summary>
    [Obsolete("Use UpdateMeasurementEqualizerAsync with EQSettings instead")]
    public async Task<string> UpdateMeasurementEqualiserAsync(string id, HttpContent content)
    {
        return await PostAsync($"http://localhost:4735/measurements/{id}/equaliser", content);
    }

    public async Task<string> GetAvailableEqualisersAsync()
    {
        return await GetAsync("http://localhost:4735/eq/equalisers");
    }

    public async Task<string> GetMeasurementFiltersAsync(string id)
    {
        return await GetAsync($"http://localhost:4735/measurements/{id}/filters");
    }

    public async Task<string> UpdateMeasurementFilterAsync(string id, string filterId, HttpContent content)
    {
        return await PutAsync($"http://localhost:4735/measurements/{id}/filters/{filterId}", content);
    }

    // Methods for handling SPL meter
    public async Task<string> GetSPLMeterAsync()
    {
        return await GetAsync("http://localhost:4735/splmeter");
    }

    public async Task<string> GetSPLMeterCommandsAsync()
    {
        return await GetAsync("http://localhost:4735/spl-meter/commands");
    }

    /// <summary>
    /// Issues an SPL meter command with parameters using domain objects
    /// Following REQ-F-SPL-001.1 for SPL meter functionality
    /// </summary>
    /// <param name="id">SPL meter ID</param>
    /// <param name="command">Command to execute</param>
    /// <param name="parameters">Strongly-typed SPL meter parameters</param>
    /// <returns>API response</returns>
    public async Task<string> IssueSPLMeterCommandAsync(string id, string command, SPLMeterCommandParameters? parameters = null)
    {
        // Parameters validated by type system
        // No additional validation needed for now

        // Create request payload
        var requestPayload = new
        {
            command = command,
            parameters = parameters ?? new object()
        };

        var json = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync($"http://localhost:4735/spl-meter/{id}/command", content);
    }

    /// <summary>
    /// Legacy method - Issues an SPL meter command with HttpContent parameters
    /// DEPRECATED: Use IssueSPLMeterCommandAsync with SPLMeterCommandParameters instead
    /// </summary>
    [Obsolete("Use IssueSPLMeterCommandAsync with SPLMeterCommandParameters instead")]
    public async Task<string> PostSPLMeterCommandAsync(string id, HttpContent content)
    {
        return await PostAsync($"http://localhost:4735/spl-meter/{id}/command", content);
    }

    /// <summary>
    /// Updates SPL meter configuration using domain objects
    /// Following REQ-F-SPL-001.1 for SPL meter configuration
    /// </summary>
    /// <param name="id">SPL meter ID</param>
    /// <param name="configuration">Strongly-typed SPL meter configuration</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateSPLMeterConfigurationAsync(string id, SPLMeterCommandParameters configuration)
    {
        // Configuration validated by type system

        var json = JsonSerializer.Serialize(configuration);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync($"http://localhost:4735/spl-meter/{id}/configuration", content);
    }

    /// <summary>
    /// Legacy method - Updates SPL meter configuration with HttpContent
    /// DEPRECATED: Use UpdateSPLMeterConfigurationAsync with SPLMeterCommandParameters instead
    /// </summary>
    [Obsolete("Use UpdateSPLMeterConfigurationAsync with SPLMeterCommandParameters instead")]
    public async Task<string> PostSPLMeterConfigurationAsync(string id, HttpContent content)
    {
        return await PostAsync($"http://localhost:4735/spl-meter/{id}/configuration", content);
    }

    public async Task<string> GetSPLMeterModesAsync()
    {
        return await GetAsync("http://localhost:4735/spl-meter/modes");
    }

    public async Task<string> GetSPLMeterWeightingsAsync()
    {
        return await GetAsync("http://localhost:4735/spl-meter/weightings");
    }

    public async Task<string> GetSPLMeterFiltersAsync()
    {
        return await GetAsync("http://localhost:4735/spl-meter/filters");
    }

    public async Task<string> GetSPLMeterLevelsAsync(string id)
    {
        return await GetAsync($"http://localhost:4735/spl-meter/{id}/levels");
    }

    // Methods for handling RTA (Real Time Analyzer)
    public async Task<string> GetRTAAsync()
    {
        return await GetAsync("http://localhost:4735/rta");
    }

    public async Task<string> GetRTACommandsAsync()
    {
        return await GetAsync("http://localhost:4735/rta/commands");
    }

    /// <summary>
    /// Issues an RTA command with parameters using domain objects
    /// Following REQ-F-RTA-001.1 for RTA functionality
    /// </summary>
    /// <param name="command">Command to execute</param>
    /// <param name="parameters">Strongly-typed RTA parameters</param>
    /// <returns>API response</returns>
    public async Task<string> IssueRTACommandAsync(string command, RTACommandParameters? parameters = null)
    {
        // Parameters validated by type system
        // No additional validation needed for now

        // Create request payload
        var requestPayload = new
        {
            command = command,
            parameters = parameters ?? new object()
        };

        var json = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/rta/command", content);
    }

    /// <summary>
    /// Legacy method - Issues an RTA command with HttpContent parameters
    /// DEPRECATED: Use IssueRTACommandAsync with RTACommandParameters instead
    /// </summary>
    [Obsolete("Use IssueRTACommandAsync with RTACommandParameters instead")]
    public async Task<string> PostRTACommandAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/rta/command", content);
    }

    public async Task<string> GetRTAConfigurationAsync()
    {
        return await GetAsync("http://localhost:4735/rta/configuration");
    }

    /// <summary>
    /// Updates RTA configuration using domain objects
    /// Following REQ-F-RTA-001.1 for RTA configuration
    /// </summary>
    /// <param name="configuration">Strongly-typed RTA configuration</param>
    /// <returns>API response</returns>
    public async Task<string> UpdateRTAConfigurationAsync(RTACommandParameters configuration)
    {
        // Configuration validated by type system

        var json = JsonSerializer.Serialize(configuration);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/rta/configuration", content);
    }

    /// <summary>
    /// Legacy method - Updates RTA configuration with HttpContent
    /// DEPRECATED: Use UpdateRTAConfigurationAsync with RTACommandParameters instead
    /// </summary>
    [Obsolete("Use UpdateRTAConfigurationAsync with RTACommandParameters instead")]
    public async Task<string> PostRTAConfigurationAsync(HttpContent content)
    {
        return await PostAsync("http://localhost:4735/rta/configuration", content);
    }

    // Methods for handling room simulator
    public async Task<string> GetRoomSimulatorAsync()
    {
        return await GetAsync("http://localhost:4735/roomsimulator");
    }

    public async Task<string> GetRoomSizeAsync()
    {
        return await GetAsync("/roomsim/room-size");
    }

    public async Task<string> SetRoomSizeAsync(HttpContent content)
    {
        return await PostAsync("/roomsim/room-size", content);
    }

    public async Task<string> GetRoomIsSealedAsync()
    {
        return await GetAsync("/roomsim/room-is-sealed");
    }

    public async Task<string> SetRoomIsSealedAsync(HttpContent content)
    {
        return await PostAsync("/roomsim/room-is-sealed", content);
    }

    public async Task<string> GetRoomSurfaceAbsorptionsAsync()
    {
        return await GetAsync("/roomsim/absorptions");
    }

    public async Task<string> SetRoomSurfaceAbsorptionsAsync(HttpContent content)
    {
        return await PostAsync("/roomsim/absorptions", content);
    }

    public async Task<string> GetRoomSourcesAsync()
    {
        return await GetAsync("/roomsim/sources");
    }

    public async Task<string> SetRoomSourcesAsync(HttpContent content)
    {
        return await PostAsync("/roomsim/sources", content);
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
        var commands = new[] { "Load", "Save all", "Save", "Generate minimum phase version", "Generate excess phase version", "Generate Mic in box correction", "Make response copy", "Merge cal data to IR", "Trim IR to windows", "Smooth", "Generate waterfalls", "Generate spectrograms", "Estimate IR delay", "Offset t=0", "Add SPL offset", "Invert", "Generate RT60" };
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
            if (string.IsNullOrEmpty(parameters) || !parameters.Contains("filename"))
            {
                return "Error: Missing or invalid 'filename' parameter for 'Save' command. Expected a string.";
            }
        }
        else if (command == "Smooth")
        {
            if (string.IsNullOrEmpty(parameters) || !parameters.Contains("smoothing"))
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

    public async Task<string> GetDistortionUnitsAsync()
    {
        return await GetAsync("/measurements/distortion-units");
    }

    public async Task<string> GetDistortionPPOChoicesAsync()
    {
        return await GetAsync("/measurements/distortion-ppo-choices");
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

    // Method to get process commands
    public async Task<string> GetProcessCommandsAsync()
    {
        var commands = new[] { "Align SPL", "Arithmetic" };
        return await Task.FromResult(string.Join(",", commands));
    }

    // Method to get process result
    public async Task<string> GetProcessResultAsync()
    {
        return await GetAsync("/measurements/process-result");
    }

    // Method to post process result
    public async Task<string> PostProcessResultAsync(string url, HttpContent content)
    {
        return await PostAsync(url, content);
    }

    // Methods for handling the /measure endpoint
    public async Task<string> GetMeasureCommandsAsync()
    {
        return await GetAsync("/measure/commands");
    }

    public async Task<string> PostMeasureCommandAsync(HttpContent content)
    {
        return await PostAsync("/measure/command", content);
    }

    public async Task<string> GetMeasureNamingAsync()
    {
        return await GetAsync("/measure/naming");
    }

    public async Task<string> PostMeasureNamingAsync(HttpContent content)
    {
        return await PostAsync("/measure/naming", content);
    }

    public async Task<string> PutMeasureNamingAsync(HttpContent content)
    {
        return await PutAsync("/measure/naming", content);
    }

    public async Task<string> GetMeasureNamingOptionsAsync()
    {
        return await GetAsync("/measure/naming/naming-options");
    }

    public async Task<string> GetMeasureDateTimeFormatsAsync()
    {
        return await GetAsync("/measure/naming/date-time-formats");
    }

    public async Task<string> GetMeasureNotesAsync()
    {
        return await GetAsync("/measure/notes");
    }

    public async Task<string> PostMeasureNotesAsync(HttpContent content)
    {
        return await PostAsync("/measure/notes", content);
    }

    public async Task<string> GetMeasureLevelAsync()
    {
        return await GetAsync("/measure/level");
    }

    public async Task<string> PostMeasureLevelAsync(HttpContent content)
    {
        return await PostAsync("/measure/level", content);
    }

    public async Task<string> GetMeasureLevelUnitsAsync()
    {
        return await GetAsync("/measure/level/units");
    }

    public async Task<string> GetMeasureProtectionOptionsAsync()
    {
        return await GetAsync("/measure/protection-options");
    }

    public async Task<string> PostMeasureProtectionOptionsAsync(HttpContent content)
    {
        return await PostAsync("/measure/protection-options", content);
    }

    public async Task<string> PutMeasureProtectionOptionsAsync(HttpContent content)
    {
        return await PutAsync("/measure/protection-options", content);
    }

    public async Task<string> GetMeasureSweepConfigurationAsync()
    {
        return await GetAsync("/measure/sweep/configuration");
    }

    public async Task<string> PostMeasureSweepConfigurationAsync(HttpContent content)
    {
        return await PostAsync("/measure/sweep/configuration", content);
    }

    public async Task<string> PutMeasureSweepConfigurationAsync(HttpContent content)
    {
        return await PutAsync("/measure/sweep/configuration", content);
    }

    public async Task<string> GetMeasureSweepRepetitionsAsync()
    {
        return await GetAsync("/measure/sweep/repetitions");
    }

    public async Task<string> PostMeasureSweepRepetitionsAsync(HttpContent content)
    {
        return await PostAsync("/measure/sweep/repetitions", content);
    }

    public async Task<string> GetMeasureTimingAsync()
    {
        return await GetAsync("/measure/timing");
    }

    public async Task<string> PostMeasureTimingAsync(HttpContent content)
    {
        return await PostAsync("/measure/timing", content);
    }

    public async Task<string> PutMeasureTimingAsync(HttpContent content)
    {
        return await PutAsync("/measure/timing", content);
    }

    public async Task<string> GetMeasureTimingOffsetAsync()
    {
        return await GetAsync("/measure/timing-offset");
    }

    public async Task<string> PostMeasureTimingOffsetAsync(HttpContent content)
    {
        return await PostAsync("/measure/timing-offset", content);
    }

    public async Task<string> GetMeasurePlaybackModeAsync()
    {
        return await GetAsync("/measure/playback-mode");
    }

    public async Task<string> PostMeasurePlaybackModeAsync(HttpContent content)
    {
        return await PostAsync("/measure/playback-mode", content);
    }

    public async Task<string> GetMeasurePlaybackModeChoicesAsync()
    {
        return await GetAsync("/measure/playback-mode/choices");
    }

    public async Task<string> PostMeasureFilePlaybackStimulusAsync(HttpContent content)
    {
        return await PostAsync("/measure/file-playback-stimulus", content);
    }

    public async Task<string> GetMeasureMeasurementModeAsync()
    {
        return await GetAsync("/measure/measurement-mode");
    }

    public async Task<string> PostMeasureMeasurementModeAsync(HttpContent content)
    {
        return await PostAsync("/measure/measurement-mode", content);
    }

    public async Task<string> GetMeasureMeasurementModeChoicesAsync()
    {
        return await GetAsync("/measure/measurement-mode/choices");
    }

    public async Task<string> GetMeasureNumberOfRepetitionsAsync()
    {
        return await GetAsync("/measure/number-of-repetitions");
    }

    public async Task<string> PostMeasureNumberOfRepetitionsAsync(HttpContent content)
    {
        return await PostAsync("/measure/number-of-repetitions", content);
    }

    public async Task<string> GetMeasureSequentialChannelsAsync()
    {
        return await GetAsync("/measure/sequential-channels");
    }

    public async Task<string> PostMeasureSequentialChannelsAsync(HttpContent content)
    {
        return await PostAsync("/measure/sequential-channels", content);
    }

    public async Task<string> GetMeasureSequentialChoicesAsync()
    {
        return await GetAsync("/measure/sequential-choices");
    }

    public async Task<string> GetMeasureStartDelayAsync()
    {
        return await GetAsync("/measure/start-delay");
    }

    public async Task<string> PostMeasureStartDelayAsync(HttpContent content)
    {
        return await PostAsync("/measure/start-delay", content);
    }

    public async Task<string> GetMeasureInvertSecondOutputAsync()
    {
        return await GetAsync("/measure/invert-second-output");
    }

    public async Task<string> PostMeasureInvertSecondOutputAsync(HttpContent content)
    {
        return await PostAsync("/measure/invert-second-output", content);
    }

    public async Task<string> GetMeasureFillSilenceWithDitherAsync()
    {
        return await GetAsync("/measure/fill-silence-with-dither");
    }

    public async Task<string> PostMeasureFillSilenceWithDitherAsync(HttpContent content)
    {
        return await PostAsync("/measure/fill-silence-with-dither", content);
    }

    public async Task<string> GetMeasureCaptureNoiseFloorAsync()
    {
        return await GetAsync("/measure/capture-noise-floor");
    }

    public async Task<string> PostMeasureCaptureNoiseFloorAsync(HttpContent content)
    {
        return await PostAsync("/measure/capture-noise-floor", content);
    }

    public async Task<string> SubscribeMeasureAsync(HttpContent content)
    {
        return await PostAsync("/measure/subscribe", content);
    }

    public async Task<string> UnsubscribeMeasureAsync(HttpContent content)
    {
        return await PostAsync("/measure/unsubscribe", content);
    }

    // Methods for handling the /alignment-tool endpoint
    public async Task<string> GetAlignmentToolCommandsAsync()
    {
        var commands = new[] { "Align SPL", "Arithmetic" };
        return await Task.FromResult(string.Join(",", commands));
    }

    public async Task<string> PostAlignmentToolCommandAsync(HttpContent content)
    {
        return await PostAsync("/alignment-tool/command", content);
    }

    public async Task<string> GetAlignmentToolModeAsync()
    {
        return await GetAsync("/alignment-tool/mode");
    }

    public async Task<string> PostAlignmentToolModeAsync(HttpContent content)
    {
        return await PostAsync("/alignment-tool/mode", content);
    }

    // Methods for handling default EQ settings
    public async Task<string> GetDefaultEqualiserAsync()
    {
        return await GetAsync("/eq/default-equaliser");
    }

    public async Task<string> UpdateDefaultEqualiserAsync(HttpContent content)
    {
        return await PostAsync("/eq/default-equaliser", content);
    }

    public async Task<string> GetDefaultTargetSettingsAsync()
    {
        return await GetAsync("/eq/default-target-settings");
    }

    public async Task<string> UpdateDefaultTargetSettingsAsync(HttpContent content)
    {
        return await PostAsync("/eq/default-target-settings", content);
    }

    public async Task<string> GetDefaultTargetLevelAsync()
    {
        return await GetAsync("/eq/default-target-level");
    }

    public async Task<string> UpdateDefaultTargetLevelAsync(HttpContent content)
    {
        return await PostAsync("/eq/default-target-level", content);
    }

    public async Task<string> GetDefaultRoomCurveSettingsAsync()
    {
        return await GetAsync("/eq/default-room-curve-settings");
    }

    public async Task<string> UpdateDefaultRoomCurveSettingsAsync(HttpContent content)
    {
        return await PostAsync("/eq/default-room-curve-settings", content);
    }

    public async Task<string> GetHouseCurveAsync()
    {
        return await GetAsync("/eq/house-curve");
    }

    public async Task<string> UpdateHouseCurveAsync(HttpContent content)
    {
        return await PostAsync("/eq/house-curve", content);
    }

    public async Task<string> ClearHouseCurveAsync()
    {
        return await PostAsync("/eq/house-curve", new StringContent("", Encoding.UTF8, "application/json"));
    }

    public async Task<string> GetMatchTargetSettingsAsync()
    {
        return await GetAsync("/eq/match-target-settings");
    }

    public async Task<string> UpdateMatchTargetSettingsAsync(HttpContent content)
    {
        return await PostAsync("/eq/match-target-settings", content);
    }

    // Methods for handling the /generator endpoint
    public async Task<string> GetGeneratorStatusAsync()
    {
        return await GetAsync("/generator/status");
    }

    public async Task<string> GetGeneratorSignalAsync()
    {
        return await GetAsync("/generator/signal");
    }

    public async Task<string> SetGeneratorSignalAsync(HttpContent content)
    {
        return await PutAsync("/generator/signal", content);
    }

    public async Task<string> GetGeneratorSignalsAsync()
    {
        return await GetAsync("/generator/signals");
    }

    public async Task<string> GetGeneratorSignalConfigurationAsync()
    {
        return await GetAsync("/generator/signal/configuration");
    }

    public async Task<string> SetGeneratorSignalConfigurationAsync(HttpContent content)
    {
        return await PostAsync("/generator/signal/configuration", content);
    }

    public async Task<string> GetGeneratorSignalCommandsAsync()
    {
        return await GetAsync("/generator/signal/commands");
    }

    public async Task<string> SendGeneratorSignalCommandAsync(HttpContent content)
    {
        return await PostAsync("/generator/signal/command", content);
    }

    public async Task<string> GetGeneratorLevelAsync()
    {
        return await GetAsync("/generator/level");
    }

    public async Task<string> SetGeneratorLevelAsync(HttpContent content)
    {
        return await PostAsync("/generator/level", content);
    }

    public async Task<string> GetGeneratorLevelUnitsAsync()
    {
        return await GetAsync("/generator/level/units");
    }

    public async Task<string> GetGeneratorFrequencyAsync()
    {
        return await GetAsync("/generator/frequency");
    }

    public async Task<string> SetGeneratorFrequencyAsync(HttpContent content)
    {
        return await PostAsync("/generator/frequency", content);
    }

    public async Task<string> GetGeneratorInvertSecondOutputAsync()
    {
        return await GetAsync("/generator/invert-second-output");
    }

    public async Task<string> SetGeneratorInvertSecondOutputAsync(HttpContent content)
    {
        return await PostAsync("/generator/invert-second-output", content);
    }

    public async Task<string> GetGeneratorProtectionAsync()
    {
        return await GetAsync("/generator/protection");
    }

    public async Task<string> SetGeneratorProtectionAsync(HttpContent content)
    {
        return await PostAsync("/generator/protection", content);
    }

    public async Task<string> StartGeneratorAsync()
    {
        var content = new StringContent("{\"command\":\"start\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("/generator/commands", content);
    }

    public async Task<string> StopGeneratorAsync()
    {
        var content = new StringContent("{\"command\":\"stop\"}", System.Text.Encoding.UTF8, "application/json");
        return await PostAsync("/generator/commands", content);
    }
}
