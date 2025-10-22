using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// Service for controlling REW measurements
    /// Provides access to measurement execution, configuration, and settings
    /// NOTE: Requires REW Pro upgrade license to execute measurements via API
    /// </summary>
    public class MeasureService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public MeasureService(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        #region Commands

        /// <summary>
        /// Get list of available measure commands
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of command strings</returns>
        public async Task<List<string>> GetCommands(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/commands";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Send a measurement command
        /// Common commands: "Start SPL measurement", "Start impedance measurement", 
        /// "Start impedance cal", "Cancel measurement"
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if command was accepted</returns>
        public async Task<bool> SendCommand(string command, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/command";
            var json = JsonSerializer.Serialize(new { command });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Start an SPL measurement
        /// Convenience wrapper for SendCommand("Start SPL measurement")
        /// </summary>
        public async Task<bool> StartSPLMeasurement(CancellationToken cancellationToken = default)
        {
            return await SendCommand("Start SPL measurement", cancellationToken);
        }

        /// <summary>
        /// Start an impedance measurement
        /// Convenience wrapper for SendCommand("Start impedance measurement")
        /// </summary>
        public async Task<bool> StartImpedanceMeasurement(CancellationToken cancellationToken = default)
        {
            return await SendCommand("Start impedance measurement", cancellationToken);
        }

        /// <summary>
        /// Start impedance calibration
        /// Convenience wrapper for SendCommand("Start impedance cal")
        /// </summary>
        public async Task<bool> StartImpedanceCalibration(CancellationToken cancellationToken = default)
        {
            return await SendCommand("Start impedance cal", cancellationToken);
        }

        /// <summary>
        /// Cancel current measurement in progress
        /// Convenience wrapper for SendCommand("Cancel measurement")
        /// </summary>
        public async Task<bool> CancelMeasurement(CancellationToken cancellationToken = default)
        {
            return await SendCommand("Cancel measurement", cancellationToken);
        }

        #endregion

        #region Naming Configuration

        /// <summary>
        /// Get current measurement naming configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Naming configuration with next measurement name preview</returns>
        public async Task<MeasureNaming> GetNaming(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/naming";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureNaming>(content) ?? new MeasureNaming();
        }

        /// <summary>
        /// Update measurement naming configuration (full replacement)
        /// </summary>
        /// <param name="naming">Complete naming configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated naming configuration with preview of next name</returns>
        public async Task<MeasureNaming> SetNaming(MeasureNaming naming, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/naming";
            var json = JsonSerializer.Serialize(naming);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureNaming>(responseContent) ?? naming;
        }

        /// <summary>
        /// Update measurement naming configuration (partial update)
        /// </summary>
        /// <param name="naming">Partial naming configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated naming configuration with preview of next name</returns>
        public async Task<MeasureNaming> UpdateNaming(MeasureNaming naming, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/naming";
            var json = JsonSerializer.Serialize(naming);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureNaming>(responseContent) ?? naming;
        }

        /// <summary>
        /// Get available naming options
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of naming option strings</returns>
        public async Task<List<string>> GetNamingOptions(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/naming/naming-options";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get available date/time format strings
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of format strings</returns>
        public async Task<List<string>> GetDateTimeFormats(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/naming/date-time-formats";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Notes

        /// <summary>
        /// Get notes that will be included in next measurement
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Measurement notes</returns>
        public async Task<MeasureNotes> GetNotes(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/notes";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple string, not a complex object
            var notes = JsonSerializer.Deserialize<string>(content) ?? string.Empty;
            return new MeasureNotes { Notes = notes };
        }

        /// <summary>
        /// Set notes to include in next measurement
        /// </summary>
        /// <param name="notes">Notes to set</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if notes were set successfully</returns>
        public async Task<bool> SetNotes(MeasureNotes notes, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/notes";
            var json = JsonSerializer.Serialize(notes);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set notes text (convenience method)
        /// </summary>
        /// <param name="notesText">Notes text</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if notes were set successfully</returns>
        public async Task<bool> SetNotes(string notesText, CancellationToken cancellationToken = default)
        {
            return await SetNotes(new MeasureNotes { Notes = notesText }, cancellationToken);
        }

        #endregion

        #region Level Configuration

        /// <summary>
        /// Get current measurement output level
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Measurement level with unit</returns>
        public async Task<MeasureLevel> GetLevel(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/level";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureLevel>(content) ?? new MeasureLevel();
        }

        /// <summary>
        /// Set measurement output level
        /// </summary>
        /// <param name="level">Level configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if level was set successfully</returns>
        public async Task<bool> SetLevel(MeasureLevel level, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/level";
            var json = JsonSerializer.Serialize(level);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set measurement output level (convenience method)
        /// </summary>
        /// <param name="level">Level value</param>
        /// <param name="unit">Unit (dBFS, dBu, dBV, SPL)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if level was set successfully</returns>
        public async Task<bool> SetLevel(double level, string unit = "dBFS", CancellationToken cancellationToken = default)
        {
            return await SetLevel(new MeasureLevel { Level = level, Unit = unit }, cancellationToken);
        }

        /// <summary>
        /// Get available units for measurement level
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of unit strings</returns>
        public async Task<List<string>> GetLevelUnits(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/level/units";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Protection Options

        /// <summary>
        /// Get measurement protection options
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Protection options</returns>
        public async Task<MeasureProtectionOptions> GetProtectionOptions(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/protection-options";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureProtectionOptions>(content) ?? new MeasureProtectionOptions();
        }

        /// <summary>
        /// Update protection options (full replacement)
        /// </summary>
        /// <param name="options">Complete protection options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if options were updated successfully</returns>
        public async Task<bool> SetProtectionOptions(MeasureProtectionOptions options, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/protection-options";
            var json = JsonSerializer.Serialize(options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update protection options (partial update)
        /// </summary>
        /// <param name="options">Partial protection options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if options were updated successfully</returns>
        public async Task<bool> UpdateProtectionOptions(MeasureProtectionOptions options, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/protection-options";
            var json = JsonSerializer.Serialize(options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Sweep Configuration

        /// <summary>
        /// Get sweep configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sweep configuration</returns>
        public async Task<SweepConfiguration> GetSweepConfiguration(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sweep/configuration";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SweepConfiguration>(content) ?? new SweepConfiguration();
        }

        /// <summary>
        /// Update sweep configuration (full replacement)
        /// </summary>
        /// <param name="config">Complete sweep configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was updated successfully</returns>
        public async Task<bool> SetSweepConfiguration(SweepConfiguration config, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sweep/configuration";
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Update sweep configuration (partial update)
        /// </summary>
        /// <param name="config">Partial sweep configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if configuration was updated successfully</returns>
        public async Task<bool> UpdateSweepConfiguration(SweepConfiguration config, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sweep/configuration";
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get sweep repetitions count
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sweep repetitions configuration</returns>
        public async Task<SweepRepetitions> GetSweepRepetitions(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sweep/repetitions";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple integer, not a complex object
            var repetitions = JsonSerializer.Deserialize<int>(content);
            return new SweepRepetitions { Repetitions = repetitions };
        }

        /// <summary>
        /// Set sweep repetitions count
        /// </summary>
        /// <param name="repetitions">Repetitions configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if repetitions were set successfully</returns>
        public async Task<bool> SetSweepRepetitions(SweepRepetitions repetitions, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sweep/repetitions";
            var json = JsonSerializer.Serialize(repetitions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set sweep repetitions count (convenience method)
        /// </summary>
        /// <param name="count">Number of repetitions (1-16)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if repetitions were set successfully</returns>
        public async Task<bool> SetSweepRepetitions(int count, CancellationToken cancellationToken = default)
        {
            return await SetSweepRepetitions(new SweepRepetitions { Repetitions = count }, cancellationToken);
        }

        #endregion

        #region Timing Configuration

        /// <summary>
        /// Get timing reference configuration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Timing reference configuration</returns>
        public async Task<TimingReference> GetTimingReference(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/timing/reference";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple string, not a complex object
            var reference = JsonSerializer.Deserialize<string>(content) ?? string.Empty;
            return new TimingReference { Reference = reference };
        }

        /// <summary>
        /// Set timing reference configuration
        /// </summary>
        /// <param name="reference">Timing reference configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if reference was set successfully</returns>
        public async Task<bool> SetTimingReference(TimingReference reference, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/timing/reference";
            var json = JsonSerializer.Serialize(reference);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get timing offset
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Timing offset configuration</returns>
        public async Task<TimingOffset> GetTimingOffset(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/timing/offset";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple double, not a complex object
            var offset = JsonSerializer.Deserialize<double>(content);
            return new TimingOffset { Offset = offset };
        }

        /// <summary>
        /// Set timing offset
        /// </summary>
        /// <param name="offset">Timing offset configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if offset was set successfully</returns>
        public async Task<bool> SetTimingOffset(TimingOffset offset, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/timing/offset";
            var json = JsonSerializer.Serialize(offset);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set timing offset (convenience method)
        /// </summary>
        /// <param name="offsetMs">Offset in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if offset was set successfully</returns>
        public async Task<bool> SetTimingOffset(double offsetMs, CancellationToken cancellationToken = default)
        {
            return await SetTimingOffset(new TimingOffset { Offset = offsetMs }, cancellationToken);
        }

        #endregion

        #region Playback Configuration

        /// <summary>
        /// Get playback mode
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Playback mode configuration</returns>
        public async Task<PlaybackMode> GetPlaybackMode(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/playback-mode";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple string, not a complex object
            var mode = JsonSerializer.Deserialize<string>(content) ?? string.Empty;
            return new PlaybackMode { Mode = mode };
        }

        /// <summary>
        /// Set playback mode
        /// </summary>
        /// <param name="mode">Playback mode configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if mode was set successfully</returns>
        public async Task<bool> SetPlaybackMode(PlaybackMode mode, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/playback-mode";
            var json = JsonSerializer.Serialize(mode);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available playback mode choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of playback mode strings</returns>
        public async Task<List<string>> GetPlaybackModeChoices(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/playback-mode/choices";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Set file playback stimulus path
        /// </summary>
        /// <param name="stimulus">File path configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if path was set successfully</returns>
        public async Task<bool> SetFilePlaybackStimulus(FilePlaybackStimulus stimulus, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/file-playback-stimulus";
            var json = JsonSerializer.Serialize(stimulus);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Set file playback stimulus path (convenience method)
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if path was set successfully</returns>
        public async Task<bool> SetFilePlaybackStimulus(string path, CancellationToken cancellationToken = default)
        {
            return await SetFilePlaybackStimulus(new FilePlaybackStimulus { Path = path }, cancellationToken);
        }

        #endregion

        #region Measurement Mode Configuration

        /// <summary>
        /// Get measurement mode
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Measurement mode configuration</returns>
        public async Task<MeasurementMode> GetMeasurementMode(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/measurement-mode";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple string, not a complex object
            var mode = JsonSerializer.Deserialize<string>(content) ?? string.Empty;
            return new MeasurementMode { Mode = mode };
        }

        /// <summary>
        /// Set measurement mode
        /// </summary>
        /// <param name="mode">Measurement mode configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if mode was set successfully</returns>
        public async Task<bool> SetMeasurementMode(MeasurementMode mode, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/measurement-mode";
            var json = JsonSerializer.Serialize(mode);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available measurement mode choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of measurement mode strings</returns>
        public async Task<List<string>> GetMeasurementModeChoices(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/measurement-mode/choices";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        /// <summary>
        /// Get number of repetitions for repeated/ramped measurements
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Measurement repetitions configuration</returns>
        public async Task<MeasurementRepetitions> GetNumberOfRepetitions(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/number-of-repetitions";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple integer, not a complex object
            var repetitions = JsonSerializer.Deserialize<int>(content);
            return new MeasurementRepetitions { Repetitions = repetitions };
        }

        /// <summary>
        /// Set number of repetitions for repeated/ramped measurements
        /// </summary>
        /// <param name="repetitions">Repetitions configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if repetitions were set successfully</returns>
        public async Task<bool> SetNumberOfRepetitions(MeasurementRepetitions repetitions, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/number-of-repetitions";
            var json = JsonSerializer.Serialize(repetitions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get start level for ramped measurements
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Start level configuration</returns>
        public async Task<MeasureLevel> GetStartLevel(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/start-level";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureLevel>(content) ?? new MeasureLevel();
        }

        /// <summary>
        /// Set start level for ramped measurements
        /// </summary>
        /// <param name="level">Start level configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if level was set successfully</returns>
        public async Task<bool> SetStartLevel(MeasureLevel level, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/start-level";
            var json = JsonSerializer.Serialize(level);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get end level for ramped measurements
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>End level configuration</returns>
        public async Task<MeasureLevel> GetEndLevel(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/end-level";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MeasureLevel>(content) ?? new MeasureLevel();
        }

        /// <summary>
        /// Set end level for ramped measurements
        /// </summary>
        /// <param name="level">End level configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if level was set successfully</returns>
        public async Task<bool> SetEndLevel(MeasureLevel level, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/end-level";
            var json = JsonSerializer.Serialize(level);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get sequential channels list
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sequential channels configuration</returns>
        public async Task<SequentialChannels> GetSequentialChannels(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sequential-channels";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SequentialChannels>(content) ?? new SequentialChannels();
        }

        /// <summary>
        /// Set sequential channels list
        /// </summary>
        /// <param name="channels">Sequential channels configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if channels were set successfully</returns>
        public async Task<bool> SetSequentialChannels(SequentialChannels channels, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sequential-channels";
            var json = JsonSerializer.Serialize(channels);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get available sequential channel choices
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of channel choice strings</returns>
        public async Task<List<string>> GetSequentialChoices(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/sequential-choices";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
        }

        #endregion

        #region Additional Settings

        /// <summary>
        /// Get start delay before measurement
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Start delay configuration</returns>
        public async Task<StartDelay> GetStartDelay(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/start-delay";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple integer, not a complex object
            var delay = JsonSerializer.Deserialize<int>(content);
            return new StartDelay { Delay = delay };
        }

        /// <summary>
        /// Set start delay before measurement
        /// </summary>
        /// <param name="delay">Start delay configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if delay was set successfully</returns>
        public async Task<bool> SetStartDelay(StartDelay delay, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/start-delay";
            var json = JsonSerializer.Serialize(delay);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get whether second output is inverted
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Invert second output configuration</returns>
        public async Task<InvertSecondOutput> GetInvertSecondOutput(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/invert-second-output";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple boolean, not a complex object
            var invert = JsonSerializer.Deserialize<bool>(content);
            return new InvertSecondOutput { Invert = invert };
        }

        /// <summary>
        /// Set whether second output is inverted
        /// </summary>
        /// <param name="invert">Invert second output configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if setting was updated successfully</returns>
        public async Task<bool> SetInvertSecondOutput(InvertSecondOutput invert, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/invert-second-output";
            var json = JsonSerializer.Serialize(invert);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get whether to fill silence with dither
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fill silence with dither configuration</returns>
        public async Task<FillSilenceWithDither> GetFillSilenceWithDither(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/fill-silence-with-dither";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple boolean, not a complex object
            var fill = JsonSerializer.Deserialize<bool>(content);
            return new FillSilenceWithDither { Fill = fill };
        }

        /// <summary>
        /// Set whether to fill silence with dither
        /// </summary>
        /// <param name="fill">Fill silence with dither configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if setting was updated successfully</returns>
        public async Task<bool> SetFillSilenceWithDither(FillSilenceWithDither fill, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/fill-silence-with-dither";
            var json = JsonSerializer.Serialize(fill);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get whether to capture noise floor before measurement
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Capture noise floor configuration</returns>
        public async Task<CaptureNoiseFloor> GetCaptureNoiseFloor(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/capture-noise-floor";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns a simple boolean, not a complex object
            var capture = JsonSerializer.Deserialize<bool>(content);
            return new CaptureNoiseFloor { Capture = capture };
        }

        /// <summary>
        /// Set whether to capture noise floor before measurement
        /// </summary>
        /// <param name="capture">Capture noise floor configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if setting was updated successfully</returns>
        public async Task<bool> SetCaptureNoiseFloor(CaptureNoiseFloor capture, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/measure/capture-noise-floor";
            var json = JsonSerializer.Serialize(capture);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        #endregion
    }
}
