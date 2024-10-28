const fetch = require('node-fetch');

class REWApi {
    constructor() {
        this.graphUpdateInhibited = false;
    }

    setGraphUpdateInhibition(inhibit) {
        this.graphUpdateInhibited = inhibit;
    }

    async get(endpoint) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint, data) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async post(endpoint, data) {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async issueMeasurementCommandWithParams(id, command, parameters) {
        // Validate boolean parameters
        const booleanParams = ["include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data"];
        for (const param of booleanParams) {
            if (parameters.hasOwnProperty(param) && typeof parameters[param] !== "boolean") {
                return `Error: Invalid value for parameter '${param}'. Expected a boolean value.`;
            }
        }

        // Validate numeric parameters
        if (parameters.hasOwnProperty("lf tail slope") && parameters["lf tail slope"] < 0) {
            return "Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0.";
        }
        if (parameters.hasOwnProperty("hf tail slope") && parameters["hf tail slope"] > 0) {
            return "Error: Invalid value for parameter 'hf tail slope'. Must be ≤ 0.";
        }

        // Validate required parameters
        if (parameters["append lf tail"]) {
            if (!parameters.hasOwnProperty("lf tail start") || !parameters.hasOwnProperty("lf tail slope")) {
                return "Error: Missing required parameters 'lf tail start' and/or 'lf tail slope'.";
            }
        }
        if (parameters["append hf tail"]) {
            if (!parameters.hasOwnProperty("hf tail start") || !parameters.hasOwnProperty("hf tail slope") || !parameters.hasOwnProperty("frequency warping")) {
                return "Error: Missing required parameters 'hf tail start', 'hf tail slope', and/or 'frequency warping'.";
            }
        }
        if (!parameters["append lf tail"] || !parameters["append hf tail"]) {
            if (!parameters.hasOwnProperty("replicate data")) {
                return "Error: Missing required parameter 'replicate data'.";
            }
        }

        const content = {
            command: command,
            parameters: parameters
        };
        const endpoint = `/measurements/${id}/command`;
        return await this.post(endpoint, content);
    }

    async generateWaterfall(mode, slices, parameters) {
        const content = {
            command: "Generate waterfall",
            parameters: {
                mode: mode,
                slices: slices,
                ...parameters
            }
        };
        return await this.post("http://localhost:4735/waterfalls", content);
    }

    async generateSpectrogram(mode, slices, parameters) {
        const content = {
            command: "Generate spectrogram",
            parameters: {
                mode: mode,
                slices: slices,
                ...parameters
            }
        };
        return await this.post("http://localhost:4735/spectrograms", content);
    }

    async processMeasurements(data) {
        return await this.post("http://localhost:4735/measurements/process-measurements", data);
    }

    // Methods for handling application settings
    async getApplicationSettings() {
        return await this.get("http://localhost:4735/application/settings");
    }

    async updateApplicationSettings(data) {
        return await this.put("http://localhost:4735/application/settings", data);
    }

    // Methods for handling audio settings
    async getAudioSettings() {
        return await this.get("http://localhost:4735/audio/settings");
    }

    async updateAudioSettings(data) {
        return await this.put("http://localhost:4735/audio/settings", data);
    }

    // Methods for handling measurements
    async getMeasurements() {
        return await this.get("http://localhost:4735/measurements");
    }

    async addMeasurement(data) {
        return await this.post("http://localhost:4735/measurements", data);
    }

    // Methods for handling frequency response
    async getFrequencyResponse(id, unit = "SPL", smoothing = "default", ppo = 0) {
        let endpoint = `/measurements/${id}/frequency-response?unit=${unit}&smoothing=${smoothing}`;
        if (ppo > 0) {
            endpoint += `&ppo=${ppo}`;
        }
        return await this.get(endpoint);
    }

    // Methods for handling impulse response
    async getImpulseResponse(id, unit = "Percent", windowed = false, normalised = true) {
        let endpoint = `/measurements/${id}/impulse-response?unit=${unit}`;
        if (windowed) {
            endpoint += `&windowed=true`;
        }
        if (!normalised) {
            endpoint += `&normalised=false`;
        }
        return await this.get(endpoint);
    }

    // Methods for handling EQ and filters
    async getEQSettings() {
        return await this.get("http://localhost:4735/eq/settings");
    }

    async updateEQSettings(data) {
        return await this.put("http://localhost:4735/eq/settings", data);
    }

    // Methods for handling SPL meter
    async getSPLMeter() {
        return await this.get("http://localhost:4735/splmeter");
    }

    // Methods for handling RTA
    async getRTA() {
        return await this.get("http://localhost:4735/rta");
    }

    // Methods for handling room simulator
    async getRoomSimulator() {
        return await this.get("http://localhost:4735/roomsimulator");
    }

    // Method for handling shutdown command
    async shutdown() {
        return await this.post("http://localhost:4735/application/command", { command: "shutdown" });
    }

    encodeArray(array) {
        const buffer = new ArrayBuffer(array.length * 4);
        const view = new DataView(buffer);
        for (let i = 0; i < array.length; i++) {
            view.setFloat32(i * 4, array[i], true);
        }
        return Buffer.from(buffer).toString('base64');
    }

    decodeArray(base64Encoded) {
        const buffer = Buffer.from(base64Encoded, 'base64');
        const view = new DataView(buffer.buffer);
        const array = new Float32Array(buffer.length / 4);
        for (let i = 0; i < array.length; i++) {
            array[i] = view.getFloat32(i * 4, true);
        }
        return array;
    }

    // Method to add a subscription
    async subscribe(data) {
        return await this.post("http://localhost:4735/subscribe", data);
    }

    // Method to remove a subscription
    async unsubscribe(data) {
        return await this.post("http://localhost:4735/unsubscribe", data);
    }

    // Method to get the list of subscribers
    async getSubscribers() {
        return await this.get("http://localhost:4735/subscribers");
    }

    // Methods for handling errors and warnings
    async getErrors() {
        return await this.get("http://localhost:4735/application/errors");
    }

    async getLastError() {
        return await this.get("http://localhost:4735/application/last-error");
    }

    async getWarnings() {
        return await this.get("http://localhost:4735/application/warnings");
    }

    async getLastWarning() {
        return await this.get("http://localhost:4735/application/last-warning");
    }

    // Methods for enabling and disabling blocking mode
    async setBlockingMode(enable) {
        const content = { blocking: enable };
        return await this.post("http://localhost:4735/application/blocking", content);
    }

    // Methods for handling graph update inhibition
    async setGraphUpdateInhibitionAsync(inhibit) {
        const content = { inhibit: inhibit };
        return await this.post("http://localhost:4735/application/inhibit-graph-updates", content);
    }

    // Methods for handling audio driver selection
    async getAudioDriver() {
        return await this.get("http://localhost:4735/audio/driver");
    }

    async setAudioDriver(data) {
        return await this.post("http://localhost:4735/audio/driver", data);
    }

    // Methods for handling sample rate
    async getSampleRate() {
        return await this.get("http://localhost:4735/audio/samplerate");
    }

    async setSampleRate(data) {
        return await this.post("http://localhost:4735/audio/samplerate", data);
    }

    // Methods for handling input calibration
    async getInputCalibration() {
        return await this.get("http://localhost:4735/audio/input-cal");
    }

    async setInputCalibration(data) {
        return await this.put("http://localhost:4735/audio/input-cal", data);
    }

    // Methods for handling output calibration
    async getOutputCalibration() {
        return await this.get("http://localhost:4735/audio/output-cal");
    }

    async setOutputCalibration(data) {
        return await this.put("http://localhost:4735/audio/output-cal", data);
    }

    // Methods for handling Java driver items
    async getJavaInputDevice() {
        return await this.get("http://localhost:4735/audio/java/input-device");
    }

    async setJavaInputDevice(data) {
        return await this.post("http://localhost:4735/audio/java/input-device", data);
    }

    async getJavaInput() {
        return await this.get("http://localhost:4735/audio/java/input");
    }

    async setJavaInput(data) {
        return await this.post("http://localhost:4735/audio/java/input", data);
    }

    async getJavaOutputDevice() {
        return await this.get("http://localhost:4735/audio/java/output-device");
    }

    async setJavaOutputDevice(data) {
        return await this.post("http://localhost:4735/audio/java/output-device", data);
    }

    async getJavaOutput() {
        return await this.get("http://localhost:4735/audio/java/output");
    }

    async setJavaOutput(data) {
        return await this.post("http://localhost:4735/audio/java/output", data);
    }

    async getJavaInputChannel() {
        return await this.get("http://localhost:4735/audio/java/input-channel");
    }

    async setJavaInputChannel(data) {
        return await this.post("http://localhost:4735/audio/java/input-channel", data);
    }

    async getJavaOutputChannel() {
        return await this.get("http://localhost:4735/audio/java/output-channel");
    }

    async setJavaOutputChannel(data) {
        return await this.post("http://localhost:4735/audio/java/output-channel", data);
    }

    async getJavaRefInputChannel() {
        return await this.get("http://localhost:4735/audio/java/ref-input-channel");
    }

    async setJavaRefInputChannel(data) {
        return await this.post("http://localhost:4735/audio/java/ref-input-channel", data);
    }

    async getJavaLastInputChannel() {
        return await this.get("http://localhost:4735/audio/java/last-input-channel");
    }

    async setJavaLastInputChannel(data) {
        return await this.post("http://localhost:4735/audio/java/last-input-channel", data);
    }

    async getJavaRefOutputChannel() {
        return await this.get("http://localhost:4735/audio/java/ref-output-channel");
    }

    async setJavaRefOutputChannel(data) {
        return await this.post("http://localhost:4735/audio/java/ref-output-channel", data);
    }

    async getJavaNumInputChannels() {
        return await this.get("http://localhost:4735/audio/java/num-input-channels");
    }

    async getJavaOutputChannels() {
        return await this.get("http://localhost:4735/audio/java/output-channels");
    }

    async getJavaRefOutputChannels() {
        return await this.get("http://localhost:4735/audio/java/ref-output-channels");
    }

    async getJavaOutputChannelMapping() {
        return await this.get("http://localhost:4735/audio/java/output-channel-mapping");
    }

    async setJavaOutputChannelMapping(data) {
        return await this.post("http://localhost:4735/audio/java/output-channel-mapping", data);
    }

    async getJavaStereoOnly() {
        return await this.get("http://localhost:4735/audio/java/stereo-only");
    }

    async setJavaStereoOnly(data) {
        return await this.post("http://localhost:4735/audio/java/stereo-only", data);
    }

    // Methods for handling ASIO driver items
    async getASIODevice() {
        return await this.get("http://localhost:4735/audio/asio/device");
    }

    async setASIODevice(data) {
        return await this.post("http://localhost:4735/audio/asio/device", data);
    }

    async getASIOInput() {
        return await this.get("http://localhost:4735/audio/asio/input");
    }

    async setASIOInput(data) {
        return await this.post("http://localhost:4735/audio/asio/input", data);
    }

    async getASIOOutput() {
        return await this.get("http://localhost:4735/audio/asio/output");
    }

    async setASIOOutput(data) {
        return await this.post("http://localhost:4735/audio/asio/output", data);
    }

    async getASIODevices() {
        return await this.get("http://localhost:4735/audio/asio/devices");
    }

    async getASIOInputs() {
        return await this.get("http://localhost:4735/audio/asio/inputs");
    }

    async getASIOOutputs() {
        return await this.get("http://localhost:4735/audio/asio/outputs");
    }

    async getASIOTimingReferenceOutputChannel() {
        return await this.get("http://localhost:4735/audio/asio/timing-reference-output-channel");
    }

    async setASIOTimingReferenceOutputChannel(data) {
        return await this.post("http://localhost:4735/audio/asio/timing-reference-output-channel", data);
    }

    async getASIORefInputChannel() {
        return await this.get("http://localhost:4735/audio/asio/ref-input-channel");
    }

    async setASIORefInputChannel(data) {
        return await this.post("http://localhost:4735/audio/asio/ref-input-channel", data);
    }

    async getASIOLastInputChannel() {
        return await this.get("http://localhost:4735/audio/asio/last-input-channel");
    }

    async setASIOLastInputChannel(data) {
        return await this.post("http://localhost:4735/audio/asio/last-input-channel", data);
    }

    async setASIOSecondaryOutputEnable(data) {
        return await this.post("http://localhost:4735/audio/asio/secondary-output-enable", data);
    }

    async getASIOSecondaryOutput() {
        return await this.get("http://localhost:4735/audio/asio/secondary-output");
    }

    async setASIOSecondaryOutput(data) {
        return await this.post("http://localhost:4735/audio/asio/secondary-output", data);
    }

    async sendASIOCommand(data) {
        return await this.post("http://localhost:4735/audio/asio/command", data);
    }

    // Methods for handling input levels
    async startMonitoringInputLevels() {
        const content = { command: "start" };
        return await this.post("http://localhost:4735/input-levels/command", content);
    }

    async stopMonitoringInputLevels() {
        const content = { command: "stop" };
        return await this.post("http://localhost:4735/input-levels/command", content);
    }

    async subscribeInputLevels(data) {
        return await this.post("http://localhost:4735/input-levels/subscribe", data);
    }

    async unsubscribeInputLevels(data) {
        return await this.post("http://localhost:4735/input-levels/unsubscribe", data);
    }

    async getLastInputLevels() {
        return await this.get("http://localhost:4735/input-levels/last-levels");
    }

    // Methods for handling selected measurement
    async selectMeasurementByUUID(data) {
        return await this.post("http://localhost:4735/measurements/selected-uuid", data);
    }

    async selectMeasurementByIndex(data) {
        return await this.post("http://localhost:4735/measurements/selected", data);
    }

    async getSelectedMeasurementUUID() {
        return await this.get("http://localhost:4735/measurements/selected-uuid");
    }

    async getSelectedMeasurementIndex() {
        return await this.get("http://localhost:4735/measurements/selected");
    }

    // Methods for handling measurement commands
    async getMeasurementCommands() {
        return await this.get("http://localhost:4735/measurements/commands");
    }

    async issueMeasurementCommand(data) {
        return await this.post("http://localhost:4735/measurements/command", data);
    }

    // Method to update a measurement's name and/or notes
    async updateMeasurement(id, data) {
        const response = await fetch(`http://localhost:4735/measurements/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    // Methods for handling IR window settings
    async getIRWindowSettings(id) {
        return await this.get(`http://localhost:4735/measurements/${id}/ir-windows`);
    }

    async updateIRWindowSettings(id, data) {
        return await this.put(`http://localhost:4735/measurements/${id}/ir-windows`, data);
    }

    // Methods for handling REW View preferences
    async getREWViewPreferences() {
        return await this.get("http://localhost:4735/application/settings");
    }

    // Method to get distortion data
    async getDistortion(id, unit = "percent", ppo = 3) {
        let endpoint = `/measurements/${id}/distortion?unit=${unit}&ppo=${ppo}`;
        return await this.get(endpoint);
    }
}

module.exports = REWApi;
