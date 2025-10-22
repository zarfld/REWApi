using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// Service for room simulator operations
    /// Provides access to room simulation, speaker/subwoofer positioning, and acoustic modeling
    /// </summary>
    public class RoomSimulatorService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RoomSimulatorService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Length Units

        /// <summary>
        /// Get available length units for room simulator
        /// </summary>
        /// <returns>List of available units (e.g., "metres", "feet")</returns>
        public async Task<List<string>> GetLengthUnits()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/length-units");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            
            // API returns JSON array
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Source Names

        /// <summary>
        /// Get available source names
        /// </summary>
        /// <returns>List of source names</returns>
        public async Task<List<string>> GetSourceNames()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/source-names");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // API returns list directly
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Room Size

        /// <summary>
        /// Get room size configuration
        /// </summary>
        /// <returns>Room size with dimensions and unit</returns>
        public async Task<RoomSize> GetRoomSize()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/room-size");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomSize>(content) ?? new RoomSize();
        }

        /// <summary>
        /// Set room size configuration
        /// </summary>
        /// <param name="roomSize">Room size to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetRoomSize(RoomSize roomSize)
        {
            var json = JsonSerializer.Serialize(roomSize);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/room-size", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update room size configuration
        /// </summary>
        /// <param name="roomSize">Room size to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateRoomSize(RoomSize roomSize)
        {
            var json = JsonSerializer.Serialize(roomSize);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/roomsim/room-size", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Room Sealed Status

        /// <summary>
        /// Check if room is sealed
        /// </summary>
        /// <returns>True if room is sealed</returns>
        public async Task<bool> GetRoomIsSealed()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/room-is-sealed");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return bool.Parse(content);
        }

        /// <summary>
        /// Set room sealed status
        /// </summary>
        /// <param name="isSealed">Whether room is sealed</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetRoomIsSealed(bool isSealed)
        {
            var json = JsonSerializer.Serialize(isSealed);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/room-is-sealed", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Absorptions

        /// <summary>
        /// Get room surface absorption coefficients
        /// </summary>
        /// <returns>Absorption coefficients for all surfaces</returns>
        public async Task<RoomAbsorptions> GetAbsorptions()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/absorptions");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomAbsorptions>(content) ?? new RoomAbsorptions();
        }

        /// <summary>
        /// Set room surface absorption coefficients
        /// </summary>
        /// <param name="absorptions">Absorption coefficients to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetAbsorptions(RoomAbsorptions absorptions)
        {
            var json = JsonSerializer.Serialize(absorptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/absorptions", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update room surface absorption coefficients
        /// </summary>
        /// <param name="absorptions">Absorption coefficients to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateAbsorptions(RoomAbsorptions absorptions)
        {
            var json = JsonSerializer.Serialize(absorptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/roomsim/absorptions", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Options

        /// <summary>
        /// Get room simulator options
        /// </summary>
        /// <returns>Current options configuration</returns>
        public async Task<RoomSimOptions> GetOptions()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/options");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomSimOptions>(content) ?? new RoomSimOptions();
        }

        /// <summary>
        /// Set room simulator options
        /// </summary>
        /// <param name="options">Options to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetOptions(RoomSimOptions options)
        {
            var json = JsonSerializer.Serialize(options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/options", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update room simulator options
        /// </summary>
        /// <param name="options">Options to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateOptions(RoomSimOptions options)
        {
            var json = JsonSerializer.Serialize(options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/roomsim/options", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Head Position

        /// <summary>
        /// Get listener head position
        /// </summary>
        /// <returns>Head position in room</returns>
        public async Task<RoomPosition> GetHeadPosition()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/head-position");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomPosition>(content) ?? new RoomPosition();
        }

        /// <summary>
        /// Set listener head position
        /// </summary>
        /// <param name="position">Head position to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetHeadPosition(RoomPosition position)
        {
            var json = JsonSerializer.Serialize(position);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/head-position", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Microphone Position Offsets

        /// <summary>
        /// Get microphone position offsets
        /// </summary>
        /// <returns>Mic position offsets</returns>
        public async Task<MicPositionOffsets> GetMicPositionOffsets()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/mic-posn-offsets");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MicPositionOffsets>(content) ?? new MicPositionOffsets();
        }

        /// <summary>
        /// Set microphone position offsets
        /// </summary>
        /// <param name="offsets">Offsets to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetMicPositionOffsets(MicPositionOffsets offsets)
        {
            var json = JsonSerializer.Serialize(offsets);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/mic-posn-offsets", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update microphone position offsets
        /// </summary>
        /// <param name="offsets">Offsets to update</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UpdateMicPositionOffsets(MicPositionOffsets offsets)
        {
            var json = JsonSerializer.Serialize(offsets);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/roomsim/mic-posn-offsets", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Sources

        /// <summary>
        /// Get active sources list
        /// </summary>
        /// <returns>List of active source names</returns>
        public async Task<List<string>> GetSources()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/sources");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SourcesList>(content);
            return result?.Sources ?? new List<string>();
        }

        /// <summary>
        /// Set active sources
        /// </summary>
        /// <param name="sources">List of source names to activate</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetSources(List<string> sources)
        {
            var sourcesList = new SourcesList { Sources = sources };
            var json = JsonSerializer.Serialize(sourcesList);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/sources", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Microphone Positions

        /// <summary>
        /// Get available microphone positions
        /// </summary>
        /// <returns>List of mic position names</returns>
        public async Task<List<string>> GetMicPositions()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/mic-positions");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // API returns list directly
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Frequency Response

        /// <summary>
        /// Get combined frequency response for all sources at a specified microphone position
        /// </summary>
        /// <param name="micPosition">The microphone position (e.g., "Main", "Left", "Right", "Front", "Behind", "Above", "Below"). Defaults to "Main".</param>
        /// <returns>Frequency response data</returns>
        public async Task<FrequencyResponseData> GetFrequencyResponse(string micPosition = "Main")
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/frequency-response?micposition={Uri.EscapeDataString(micPosition)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FrequencyResponseData>(content) ?? new FrequencyResponseData();
        }

        /// <summary>
        /// Get frequency response for specific source at a specified microphone position
        /// </summary>
        /// <param name="sourceName">Name of source</param>
        /// <param name="micPosition">The microphone position (e.g., "Main", "Left", "Right", "Front", "Behind", "Above", "Below"). Defaults to "Main".</param>
        /// <returns>Frequency response data for source</returns>
        public async Task<FrequencyResponseData> GetSourceFrequencyResponse(string sourceName, string micPosition = "Main")
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/{sourceName}/frequency-response?micposition={Uri.EscapeDataString(micPosition)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FrequencyResponseData>(content) ?? new FrequencyResponseData();
        }

        #endregion

        #region Source Position

        /// <summary>
        /// Get position of specific source
        /// </summary>
        /// <param name="sourceName">Name of source</param>
        /// <returns>Source position</returns>
        public async Task<RoomPosition> GetSourcePosition(string sourceName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/{sourceName}/position");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RoomPosition>(content) ?? new RoomPosition();
        }

        /// <summary>
        /// Set position of specific source
        /// </summary>
        /// <param name="sourceName">Name of source</param>
        /// <param name="position">Position to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetSourcePosition(string sourceName, RoomPosition position)
        {
            var json = JsonSerializer.Serialize(position);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/{sourceName}/position", content);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Source Configuration

        /// <summary>
        /// Get configuration of specific source
        /// </summary>
        /// <param name="sourceName">Name of source</param>
        /// <returns>Source configuration</returns>
        public async Task<SourceConfiguration> GetSourceConfiguration(string sourceName)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/roomsim/{sourceName}/configuration");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SourceConfiguration>(content) ?? new SourceConfiguration();
        }

        /// <summary>
        /// Set configuration of specific source
        /// </summary>
        /// <param name="sourceName">Name of source</param>
        /// <param name="configuration">Configuration to set</param>
        /// <returns>True if successful</returns>
        public async Task<bool> SetSourceConfiguration(string sourceName, SourceConfiguration configuration)
        {
            var json = JsonSerializer.Serialize(configuration);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/roomsim/{sourceName}/configuration", content);
            return response.IsSuccessStatusCode;
        }

        #endregion
    }
}
