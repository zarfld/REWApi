#ifndef REWAPI_H
#define REWAPI_H

#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <pplx/pplxtasks.h>
#include <vector>
#include <string>

class REWApi
{
public:
    pplx::task<utility::string_t> GetAsync(const utility::string_t& endpoint);
    pplx::task<utility::string_t> PutAsync(const utility::string_t& endpoint, const web::json::value& content);
    pplx::task<utility::string_t> PostAsync(const utility::string_t& endpoint, const web::json::value& content);

    // Methods for handling application settings
    pplx::task<utility::string_t> GetApplicationSettingsAsync();
    pplx::task<utility::string_t> UpdateApplicationSettingsAsync(const web::json::value& content);

    // Methods for handling audio settings
    pplx::task<utility::string_t> GetAudioSettingsAsync();
    pplx::task<utility::string_t> UpdateAudioSettingsAsync(const web::json::value& content);

    // Methods for handling measurements
    pplx::task<utility::string_t> GetMeasurementsAsync();
    pplx::task<utility::string_t> AddMeasurementAsync(const web::json::value& content);

    // Methods for handling frequency response
    pplx::task<utility::string_t> GetFrequencyResponseAsync(const utility::string_t& id, const utility::string_t& unit = U("SPL"), const utility::string_t& smoothing = U("default"), int ppo = 0);

    // Methods for handling impulse response
    pplx::task<utility::string_t> GetImpulseResponseAsync(const utility::string_t& id, const utility::string_t& unit = U("Percent"), bool windowed = false, bool normalised = true);

    // Methods for handling EQ and filters
    pplx::task<utility::string_t> GetEQSettingsAsync();
    pplx::task<utility::string_t> UpdateEQSettingsAsync(const web::json::value& content);

    // Methods for handling SPL meter
    pplx::task<utility::string_t> GetSPLMeterAsync();

    // Methods for handling RTA
    pplx::task<utility::string_t> GetRTAAsync();

    // Methods for handling room simulator
    pplx::task<utility::string_t> GetRoomSimulatorAsync();

    // Method to set graph update inhibition
    void SetGraphUpdateInhibition(bool inhibit);

    // Methods for encoding and decoding arrays
    std::string encodeArray(const std::vector<float>& array);
    std::vector<float> decodeArray(const std::string& base64Encoded);

    // Method to add a subscription
    pplx::task<utility::string_t> SubscribeAsync(const web::json::value& content);

    // Method to remove a subscription
    pplx::task<utility::string_t> UnsubscribeAsync(const web::json::value& content);

    // Method to get the list of subscribers
    pplx::task<utility::string_t> GetSubscribersAsync();

    // Methods for handling errors and warnings
    pplx::task<utility::string_t> GetErrorsAsync();
    pplx::task<utility::string_t> GetLastErrorAsync();
    pplx::task<utility::string_t> GetWarningsAsync();
    pplx::task<utility::string_t> GetLastWarningAsync();
    pplx::task<utility::string_t> SubscribeToErrorsAsync(const web::json::value& content);
    pplx::task<utility::string_t> SubscribeToWarningsAsync(const web::json::value& content);

    // Methods for enabling and disabling blocking mode
    pplx::task<utility::string_t> SetBlockingModeAsync(bool enable);

    // Methods for handling graph update inhibition
    pplx::task<utility::string_t> SetGraphUpdateInhibitionAsync(bool inhibit);

    // Methods for handling audio driver selection
    pplx::task<utility::string_t> GetAudioDriverAsync();
    pplx::task<utility::string_t> SetAudioDriverAsync(const web::json::value& content);

    // Methods for handling sample rate
    pplx::task<utility::string_t> GetSampleRateAsync();
    pplx::task<utility::string_t> SetSampleRateAsync(const web::json::value& content);

    // Methods for handling input calibration
    pplx::task<utility::string_t> GetInputCalibrationAsync();
    pplx::task<utility::string_t> SetInputCalibrationAsync(const web::json::value& content);

    // Methods for handling output calibration
    pplx::task<utility::string_t> GetOutputCalibrationAsync();
    pplx::task<utility::string_t> SetOutputCalibrationAsync(const web::json::value& content);

    // Methods for handling Java driver items
    pplx::task<utility::string_t> GetJavaInputDeviceAsync();
    pplx::task<utility::string_t> SetJavaInputDeviceAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaInputAsync();
    pplx::task<utility::string_t> SetJavaInputAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaOutputDeviceAsync();
    pplx::task<utility::string_t> SetJavaOutputDeviceAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaOutputAsync();
    pplx::task<utility::string_t> SetJavaOutputAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaInputChannelAsync();
    pplx::task<utility::string_t> SetJavaInputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaOutputChannelAsync();
    pplx::task<utility::string_t> SetJavaOutputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaRefInputChannelAsync();
    pplx::task<utility::string_t> SetJavaRefInputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaLastInputChannelAsync();
    pplx::task<utility::string_t> SetJavaLastInputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaRefOutputChannelAsync();
    pplx::task<utility::string_t> SetJavaRefOutputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaNumInputChannelsAsync();

    pplx::task<utility::string_t> GetJavaOutputChannelsAsync();

    pplx::task<utility::string_t> GetJavaRefOutputChannelsAsync();

    pplx::task<utility::string_t> GetJavaOutputChannelMappingAsync();
    pplx::task<utility::string_t> SetJavaOutputChannelMappingAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetJavaStereoOnlyAsync();
    pplx::task<utility::string_t> SetJavaStereoOnlyAsync(const web::json::value& content);

    // Methods for handling ASIO driver items
    pplx::task<utility::string_t> GetASIODeviceAsync();
    pplx::task<utility::string_t> SetASIODeviceAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIOInputAsync();
    pplx::task<utility::string_t> SetASIOInputAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIOOutputAsync();
    pplx::task<utility::string_t> SetASIOOutputAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIODevicesAsync();
    pplx::task<utility::string_t> GetASIOInputsAsync();
    pplx::task<utility::string_t> GetASIOOutputsAsync();

    pplx::task<utility::string_t> GetASIOTimingReferenceOutputChannelAsync();
    pplx::task<utility::string_t> SetASIOTimingReferenceOutputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIORefInputChannelAsync();
    pplx::task<utility::string_t> SetASIORefInputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIOLastInputChannelAsync();
    pplx::task<utility::string_t> SetASIOLastInputChannelAsync(const web::json::value& content);

    pplx::task<utility::string_t> SetASIOSecondaryOutputEnableAsync(const web::json::value& content);

    pplx::task<utility::string_t> GetASIOSecondaryOutputAsync();
    pplx::task<utility::string_t> SetASIOSecondaryOutputAsync(const web::json::value& content);

    pplx::task<utility::string_t> SendASIOCommandAsync(const web::json::value& content);

    // Methods for handling input levels
    pplx::task<utility::string_t> StartMonitoringInputLevelsAsync();
    pplx::task<utility::string_t> StopMonitoringInputLevelsAsync();
    pplx::task<utility::string_t> SubscribeInputLevelsAsync(const web::json::value& content);
    pplx::task<utility::string_t> UnsubscribeInputLevelsAsync(const web::json::value& content);
    pplx::task<utility::string_t> GetLastInputLevelsAsync();

    // Methods for handling selected measurement
    pplx::task<utility::string_t> SelectMeasurementByUUIDAsync(const web::json::value& content);
    pplx::task<utility::string_t> SelectMeasurementByIndexAsync(const web::json::value& content);
    pplx::task<utility::string_t> GetSelectedMeasurementUUIDAsync();
    pplx::task<utility::string_t> GetSelectedMeasurementIndexAsync();

    // Methods for handling measurement commands
    pplx::task<utility::string_t> GetMeasurementCommandsAsync();
    pplx::task<utility::string_t> IssueMeasurementCommandAsync(const web::json::value& content);
    pplx::task<utility::string_t> IssueMeasurementCommandWithParamsAsync(const utility::string_t& id, const utility::string_t& command, const web::json::value& parameters);

    // Method to update a measurement's name and/or notes
    pplx::task<utility::string_t> UpdateMeasurementAsync(const utility::string_t& id, const web::json::value& content);

    // Methods for handling IR window settings
    pplx::task<utility::string_t> GetIRWindowSettingsAsync(const utility::string_t& id);
    pplx::task<utility::string_t> UpdateIRWindowSettingsAsync(const utility::string_t& id, const web::json::value& content);

    // Methods for handling REW View preferences
    pplx::task<utility::string_t> GetREWViewPreferencesAsync();

    // Method to get distortion data
    pplx::task<utility::string_t> GetDistortionAsync(const utility::string_t& id, const utility::string_t& unit = U("percent"), int ppo = 3);

    // Method to generate waterfall
    pplx::task<utility::string_t> GenerateWaterfallAsync(const utility::string_t& mode, int slices, const web::json::value& parameters);

    // Method to generate spectrogram
    pplx::task<utility::string_t> GenerateSpectrogramAsync(const utility::string_t& mode, int slices, const web::json::value& parameters);

    // Method to process measurements
    pplx::task<utility::string_t> ProcessMeasurementsAsync(const web::json::value& content);

    // Method to handle the Smooth command
    pplx::task<utility::string_t> SmoothAsync(const utility::string_t& id, const utility::string_t& smoothing = U("1/3"));

    // Methods for handling groups and their measurements
    pplx::task<utility::string_t> GetGroupsAsync();
    pplx::task<utility::string_t> GetGroupAsync(const utility::string_t& uuid);
    pplx::task<utility::string_t> CreateGroupAsync(const web::json::value& content);
    pplx::task<utility::string_t> UpdateGroupAsync(const utility::string_t& uuid, const web::json::value& content);
    pplx::task<utility::string_t> DeleteGroupAsync(const utility::string_t& uuid);
    pplx::task<utility::string_t> DeleteAllGroupsAsync();
    pplx::task<utility::string_t> GetGroupMeasurementsAsync(const utility::string_t& uuid);
    pplx::task<utility::string_t> AddMeasurementToGroupAsync(const utility::string_t& uuid, const web::json::value& content);
    pplx::task<utility::string_t> RemoveMeasurementFromGroupAsync(const utility::string_t& uuid, const utility::string_t& measurementUuid);

    // Method to handle group change subscriptions
    pplx::task<utility::string_t> SubscribeToGroupChangesAsync(const web::json::value& content);

    // Method to handle group change unsubscriptions
    pplx::task<utility::string_t> UnsubscribeFromGroupChangesAsync(const web::json::value& content);

    // Method to get the list of group subscribers
    pplx::task<utility::string_t> GetGroupSubscribersAsync();

private:
    bool graphUpdateInhibited;
    bool blockingModeEnabled;
};

#endif // REWAPI_H
