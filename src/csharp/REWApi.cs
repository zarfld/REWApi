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
}
