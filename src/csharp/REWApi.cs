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
    public async Task<string> GetFrequencyResponseAsync()
    {
        return await GetAsync("http://localhost:4735/frequencyresponse");
    }

    // Methods for handling impulse response
    public async Task<string> GetImpulseResponseAsync()
    {
        return await GetAsync("http://localhost:4735/impulseresponse");
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
}
