import fetch from 'node-fetch';

class REWApi {
    private graphUpdateInhibited: boolean = false;
    private blockingModeEnabled: boolean = false;

    setGraphUpdateInhibition(inhibit: boolean) {
        this.graphUpdateInhibited = inhibit;
    }

    async get(endpoint: string): Promise<any> {
        if (this.graphUpdateInhibited) return null;
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint: string, data: any): Promise<any> {
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

    async post(endpoint: string, data: any): Promise<any> {
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
        if (this.blockingModeEnabled) {
            await response.json();
        }
        return await response.json();
    }

    async issueMeasurementCommandWithParams(id: string, command: string, parameters: any): Promise<any> {
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

        if (command === "Smooth") {
            if (!parameters.hasOwnProperty("smoothing") || typeof parameters["smoothing"] !== "string") {
                return "Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.";
            }
        }

        if (command === "Offset t=0") {
            if (!parameters.hasOwnProperty("offset") || typeof parameters["offset"] !== "string") {
                return "Error: Missing or invalid 'offset' parameter for 'Offset t=0' command. Expected a string.";
            }
            if (!parameters.hasOwnProperty("unit") || typeof parameters["unit"] !== "string") {
                return "Error: Missing or invalid 'unit' parameter for 'Offset t=0' command. Expected a string.";
            }
        }

        if (command === "Generate RT60") {
            if (!parameters.hasOwnProperty("octaveFrac") || typeof parameters["octaveFrac"] !== "number") {
                return "Error: Missing or invalid 'octaveFrac' parameter for 'Generate RT60' command. Expected a number.";
            }
            if (!parameters.hasOwnProperty("filterOrder") || typeof parameters["filterOrder"] !== "number") {
                return "Error: Missing or invalid 'filterOrder' parameter for 'Generate RT60' command. Expected a number.";
            }
            if (!parameters.hasOwnProperty("zeroPhaseFiltered") || typeof parameters["zeroPhaseFiltered"] !== "boolean") {
                return "Error: Missing or invalid 'zeroPhaseFiltered' parameter for 'Generate RT60' command. Expected a boolean.";
            }
            if (!parameters.hasOwnProperty("reverseFiltered") || typeof parameters["reverseFiltered"] !== "boolean") {
                return "Error: Missing or invalid 'reverseFiltered' parameter for 'Generate RT60' command. Expected a boolean.";
            }
        }

        const content = {
            command: command,
            parameters: parameters
        };
        const endpoint = `/measurements/${id}/command`;
        return await this.post(endpoint, content).then(response => {
            return this._notifySubscribers(endpoint, content).then(() => response);
        });
    }

    async generateWaterfall(mode: string, slices: number, parameters: any): Promise<any> {
        // Validate required parameters for "Fourier" mode
        if (mode === "Fourier") {
            const requiredParams = ["left window type", "right window type", "window width ms", "time range ms", "rise time ms", "use csd mode"];
            for (const param of requiredParams) {
                if (!parameters.hasOwnProperty(param)) {
                    return `Error: Missing required parameter '${param}' for 'Fourier' mode.`;
                }
            }
        }
        // Validate required parameters for "Burst decay" mode
        else if (mode === "Burst decay") {
            const requiredParams = ["bandwidth", "periods"];
            for (const param of requiredParams) {
                if (!parameters.hasOwnProperty(param)) {
                    return `Error: Missing required parameter '${param}' for 'Burst decay' mode.`;
                }
            }
        } else {
            return "Error: Invalid mode. Expected 'Fourier' or 'Burst decay'.";
        }

        const content = {
            command: "Generate waterfall",
            parameters: {
                mode: mode,
                slices: slices,
                ...parameters
            }
        };
        return await this.post("http://localhost:4735/waterfalls", content).then(response => {
            return this._notifySubscribers("/waterfalls", content).then(() => response);
        });
    }

    async generateSpectrogram(mode: string, slices: number, parameters: any): Promise<any> {
        const content = {
            command: "Generate spectrogram",
            parameters: {
                mode: mode,
                slices: slices,
                ...parameters
            }
        };
        return await this.post("http://localhost:4735/spectrograms", content).then(response => {
            return this._notifySubscribers("/spectrograms", content).then(() => response);
        });
    }

    async processMeasurements(data: any): Promise<any> {
        // Check if the processName is "Align SPL"
        if (data.hasOwnProperty("processName") && data.processName === "Align SPL") {
            // Validate required parameters
            if (!data.hasOwnProperty("parameters") || !data.parameters.hasOwnProperty("targetdB") || !data.parameters.hasOwnProperty("frequencyHz") || !data.parameters.hasOwnProperty("spanOctaves")) {
                return "Error: Missing required parameters 'targetdB', 'frequencyHz', and/or 'spanOctaves'.";
            }

            // Validate measurementIndices field
            if (!data.hasOwnProperty("measurementIndices") || !Array.isArray(data.measurementIndices)) {
                return "Error: Missing or invalid 'measurementIndices' field. Must be an array.";
            }
        }

        // Check if the processName is "Arithmetic"
        if (data.hasOwnProperty("processName") && data.processName === "Arithmetic") {
            // Validate required parameters
            if (!data.hasOwnProperty("parameters") || !data.parameters.hasOwnProperty("function")) {
                return "Error: Missing required parameter 'function'.";
            }

            // Validate optional parameters
            if (data.parameters.hasOwnProperty("maxGain") && typeof data.parameters.maxGain !== "number") {
                return "Error: Invalid value for parameter 'maxGain'. Expected a number.";
            }
            if (data.parameters.hasOwnProperty("lowerLimit") && typeof data.parameters.lowerLimit !== "number") {
                return "Error: Invalid value for parameter 'lowerLimit'. Expected a number.";
            }
            if (data.parameters.hasOwnProperty("upperLimit") && typeof data.parameters.upperLimit !== "number") {
                return "Error: Invalid value for parameter 'upperLimit'. Expected a number.";
            }
            if (data.parameters.hasOwnProperty("mergeFrequency") && typeof data.parameters.mergeFrequency !== "number") {
                return "Error: Invalid value for parameter 'mergeFrequency'. Expected a number.";
            }
            if (data.parameters.hasOwnProperty("mergeBlend") && typeof data.parameters.mergeBlend !== "boolean") {
                return "Error: Invalid value for parameter 'mergeBlend'. Expected a boolean.";
            }
            if (data.parameters.hasOwnProperty("targetLevel") && typeof data.parameters.targetLevel !== "number") {
                return "Error: Invalid value for parameter 'targetLevel'. Expected a number.";
            }
            if (data.parameters.hasOwnProperty("autoTarget") && typeof data.parameters.autoTarget !== "boolean") {
                return "Error: Invalid value for parameter 'autoTarget'. Expected a boolean.";
            }
            if (data.parameters.hasOwnProperty("excludeNotches") && typeof data.parameters.excludeNotches !== "boolean") {
                return "Error: Invalid value for parameter 'excludeNotches'. Expected a boolean.";
            }

            // Validate measurementIndices field
            if (!data.hasOwnProperty("measurementIndices") || !Array.isArray(data.measurementIndices)) {
                return "Error: Missing or invalid 'measurementIndices' field. Must be an array.";
            }
        }

        return await this.post("http://localhost:4735/measurements/process-measurements", data).then(response => {
            return this._notifySubscribers("/measurements/process-measurements", data).then(() => response);
        });
    }

    // Methods for handling application settings
    async getApplicationSettings(): Promise<any> {
        return await this.get("http://localhost:4735/application/settings");
    }

    async updateApplicationSettings(data: any): Promise<any> {
        return await this.put("http://localhost:4735/application/settings", data);
    }

    // Methods for handling audio settings
    async getAudioSettings(): Promise<any> {
        return await this.get("http://localhost:4735/audio/settings");
    }

    async updateAudioSettings(data: any): Promise<any> {
        return await this.put("http://localhost:4735/audio/settings", data);
    }

    // Methods for handling measurements
    async getMeasurements(): Promise<any> {
        return await this.get("http://localhost:4735/measurements");
    }

    async addMeasurement(data: any): Promise<any> {
        return await this.post("http://localhost:4735/measurements", data).then(response => {
            return this._notifySubscribers("/measurements", data).then(() => response);
        });
    }

    async deleteMeasurement(id: string): Promise<any> {
        const response = await fetch(`http://localhost:4735/measurements/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const responseData = await response.json();
        return this._notifySubscribers(`/measurements/${id}`, {}).then(() => responseData);
    }

    async deleteAllMeasurements(): Promise<any> {
        const response = await fetch("http://localhost:4735/measurements", {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const responseData = await response.json();
        return this._notifySubscribers("/measurements", {}).then(() => responseData);
    }

    // Methods for handling frequency response
    async getFrequencyResponse(id: string, unit: string = "SPL", smoothing: string = "default", ppo: number = 0): Promise<any> {
        let endpoint = `/measurements/${id}/frequency-response?unit=${unit}&smoothing=${smoothing}`;
        if (ppo > 0) {
            endpoint += `&ppo=${ppo}`;
        }
        const response = await this.get(endpoint);
        if (ppo > 0 && smoothing === "default") {
            // Apply smoothing to ppo/2 if greater smoothing has not already been applied
            // This is a placeholder for the actual smoothing logic
        }
        return response;
    }

    async getFrequencyResponseUnits(): Promise<any> {
        return await this.get("/measurements/frequency-response/units");
    }

    async getFrequencyResponseSmoothingChoices(): Promise<any> {
        return await this.get("/measurements/frequency-response/smoothing-choices");
    }

    // Methods for handling impulse response
    async getImpulseResponse(id: string, unit: string = "Percent", windowed: boolean = false, normalised: boolean = true): Promise<any> {
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
    async getEQSettings(): Promise<any> {
        return await this.get("http://localhost:4735/eq/settings");
    }

    async updateEQSettings(data: any): Promise<any> {
        return await this.put("http://localhost:4735/eq/settings", data);
    }

    // Methods for handling SPL meter
    async getSPLMeter(): Promise<any> {
        return await this.get("http://localhost:4735/splmeter");
    }

    // Methods for handling RTA
    async getRTA(): Promise<any> {
        return await this.get("http://localhost:4735/rta");
    }

    // Methods for handling room simulator
    async getRoomSimulator(): Promise<any> {
        return await this.get("http://localhost:4735/roomsimulator");
    }

    // Method for handling shutdown command
    async shutdown(): Promise<any> {
        return await this.post("http://localhost:4735/application/command", { command: "shutdown" });
    }

    encodeArray(array: Float32Array): string {
        const buffer = new ArrayBuffer(array.length * 4);
        const view = new DataView(buffer);
        for (let i = 0; i < array.length; i++) {
            view.setFloat32(i * 4, array[i], true);
        }
        return Buffer.from(buffer).toString('base64');
    }

    decodeArray(base64Encoded: string): Float32Array {
        const buffer = Buffer.from(base64Encoded, 'base64');
        const view = new DataView(buffer.buffer);
        const array = new Float32Array(buffer.length / 4);
        for (let i = 0; array.length; i++) {
            array[i] = view.getFloat32(i * 4, true);
        }
        return array;
    }

    // Method to add a subscription
    async subscribe(data: any): Promise<any> {
        return await this.post("http://localhost:4735/subscribe", data);
    }

    // Method to remove a subscription
    async unsubscribe(data: any): Promise<any> {
        return await this.post("http://localhost:4735/unsubscribe", data);
    }

    // Method to get the list of subscribers
    async getSubscribers(): Promise<any> {
        return await this.get("http://localhost:4735/subscribers");
    }

    // Methods for handling errors and warnings
    async getErrors(): Promise<any> {
        return await this.get("http://localhost:4735/application/errors");
    }

    async getLastError(): Promise<any> {
        return await this.get("http://localhost:4735/application/last-error");
    }

    async getWarnings(): Promise<any> {
        return await this.get("http://localhost:4735/application/warnings");
    }

    async getLastWarning(): Promise<any> {
        return await this.get("http://localhost:4735/application/last-warning");
    }

    // Methods for enabling and disabling blocking mode
    async setBlockingMode(enable: boolean): Promise<any> {
        this.blockingModeEnabled = enable;
        const content = { blocking: enable };
        return await this.post("http://localhost:4735/application/blocking", content);
    }

    // Methods for handling graph update inhibition
    async setGraphUpdateInhibitionAsync(inhibit: boolean): Promise<any> {
        const content = { inhibit: inhibit };
        return await this.post("http://localhost:4735/application/inhibit-graph-updates", content);
    }

    // Methods for handling audio driver selection
    async getAudioDriver(): Promise<any> {
        return await this.get("http://localhost:4735/audio/driver");
    }

    async setAudioDriver(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/driver", data);
    }

    // Methods for handling sample rate
    async getSampleRate(): Promise<any> {
        return await this.get("http://localhost:4735/audio/samplerate");
    }

    async setSampleRate(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/samplerate", data);
    }

    // Methods for handling input calibration
    async getInputCalibration(): Promise<any> {
        return await this.get("http://localhost:4735/audio/input-cal");
    }

    async setInputCalibration(data: any): Promise<any> {
        return await this.put("http://localhost:4735/audio/input-cal", data);
    }

    // Methods for handling output calibration
    async getOutputCalibration(): Promise<any> {
        return await this.get("http://localhost:4735/audio/output-cal");
    }

    async setOutputCalibration(data: any): Promise<any> {
        return await this.put("http://localhost:4735/audio/output-cal", data);
    }

    // Methods for handling Java driver items
    async getJavaInputDevice(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/input-device");
    }

    async setJavaInputDevice(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/input-device", data);
    }

    async getJavaInput(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/input");
    }

    async setJavaInput(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/input", data);
    }

    async getJavaOutputDevice(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/output-device");
    }

    async setJavaOutputDevice(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/output-device", data);
    }

    async getJavaOutput(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/output");
    }

    async setJavaOutput(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/output", data);
    }

    async getJavaInputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/input-channel");
    }

    async setJavaInputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/input-channel", data);
    }

    async getJavaOutputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/output-channel");
    }

    async setJavaOutputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/output-channel", data);
    }

    async getJavaRefInputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/ref-input-channel");
    }

    async setJavaRefInputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/ref-input-channel", data);
    }

    async getJavaLastInputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/last-input-channel");
    }

    async setJavaLastInputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/last-input-channel", data);
    }

    async getJavaRefOutputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/ref-output-channel");
    }

    async setJavaRefOutputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/ref-output-channel", data);
    }

    async getJavaNumInputChannels(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/num-input-channels");
    }

    async getJavaOutputChannels(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/output-channels");
    }

    async getJavaRefOutputChannels(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/ref-output-channels");
    }

    async getJavaOutputChannelMapping(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/output-channel-mapping");
    }

    async setJavaOutputChannelMapping(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/output-channel-mapping", data);
    }

    async getJavaStereoOnly(): Promise<any> {
        return await this.get("http://localhost:4735/audio/java/stereo-only");
    }

    async setJavaStereoOnly(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/java/stereo-only", data);
    }

    // Methods for handling ASIO driver items
    async getASIODevice(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/device");
    }

    async setASIODevice(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/device", data);
    }

    async getASIOInput(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/input");
    }

    async setASIOInput(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/input", data);
    }

    async getASIOOutput(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/output");
    }

    async setASIOOutput(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/output", data);
    }

    async getASIODevices(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/devices");
    }

    async getASIOInputs(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/inputs");
    }

    async getASIOOutputs(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/outputs");
    }

    async getASIOTimingReferenceOutputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/timing-reference-output-channel");
    }

    async setASIOTimingReferenceOutputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/timing-reference-output-channel", data);
    }

    async getASIORefInputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/ref-input-channel");
    }

    async setASIORefInputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/ref-input-channel", data);
    }

    async getASIOLastInputChannel(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/last-input-channel");
    }

    async setASIOLastInputChannel(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/last-input-channel", data);
    }

    async setASIOSecondaryOutputEnable(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/secondary-output-enable", data);
    }

    async getASIOSecondaryOutput(): Promise<any> {
        return await this.get("http://localhost:4735/audio/asio/secondary-output");
    }

    async setASIOSecondaryOutput(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/secondary-output", data);
    }

    async sendASIOCommand(data: any): Promise<any> {
        return await this.post("http://localhost:4735/audio/asio/command", data);
    }

    // Methods for handling input levels
    async startMonitoringInputLevels(): Promise<any> {
        const content = { command: "start" };
        return await this.post("http://localhost:4735/input-levels/command", content);
    }

    async stopMonitoringInputLevels(): Promise<any> {
        const content = { command: "stop" };
        return await this.post("http://localhost:4735/input-levels/command", content);
    }

    async subscribeInputLevels(data: any): Promise<any> {
        return await this.post("http://localhost:4735/input-levels/subscribe", data);
    }

    async unsubscribeInputLevels(data: any): Promise<any> {
        return await this.post("http://localhost:4735/input-levels/unsubscribe", data);
    }

    async getLastInputLevels(): Promise<any> {
        return await this.get("http://localhost:4735/input-levels/last-levels");
    }

    async getInputLevelUnits(): Promise<any> {
        return await this.get("http://localhost:4735/input-levels/units");
    }

    // Methods for handling selected measurement
    async selectMeasurementByUUID(data: any): Promise<any> {
        return await this.post("http://localhost:4735/measurements/selected-uuid", data);
    }

    async selectMeasurementByIndex(data: any): Promise<any> {
        return await this.post("http://localhost:4735/measurements/selected", data);
    }

    async getSelectedMeasurementUUID(): Promise<any> {
        return await this.get("http://localhost:4735/measurements/selected-uuid");
    }

    async getSelectedMeasurementIndex(): Promise<any> {
        return await this.get("http://localhost:4735/measurements/selected");
    }

    // Methods for handling measurement commands
    async getMeasurementCommands(): Promise<any> {
        const commands = [
            "Load",
            "Save all",
            "Save",
            "Generate minimum phase version",
            "Generate excess phase version",
            "Generate Mic in box correction",
            "Make response copy",
            "Merge cal data to IR",
            "Trim IR to windows",
            "Smooth",
            "Generate waterfalls",
            "Generate spectrograms",
            "Estimate IR delay",
            "Offset t=0",
            "Add SPL offset",
            "Invert",
            "Generate RT60"
        ];
        return commands;
    }

    async issueMeasurementCommand(data: any): Promise<any> {
        const command = data.command;
        const parameters = data.parameters;

        if (command === "Load") {
            for (const param of parameters) {
                if (typeof param !== "string") {
                    return "Error: Invalid parameter for 'Load' command. Expected a string.";
                }
            }
        } else if (command === "Save all") {
            if (parameters.length < 1 || parameters.length > 2) {
                return "Error: Invalid number of parameters for 'Save all' command. Expected 1 or 2 parameters.";
            }
            if (typeof parameters[0] !== "string") {
                return "Error: Invalid parameter for 'Save all' command. Expected a string for the file path.";
            }
            if (parameters.length === 2 && typeof parameters[1] !== "string") {
                return "Error: Invalid parameter for 'Save all' command. Expected a string for the note.";
            }
        } else if (command === "Save") {
            if (!parameters.hasOwnProperty("filename") || typeof parameters["filename"] !== "string") {
                return "Error: Missing or invalid 'filename' parameter for 'Save' command. Expected a string.";
            }
        } else if (command === "Smooth") {
            if (!parameters.hasOwnProperty("smoothing") || typeof parameters["smoothing"] !== "string") {
                return "Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.";
            }
        } else {
            return "Error: Unknown command.";
        }

        return await this.post("http://localhost:4735/measurements/command", data).then(response => {
            return this._notifySubscribers("/measurements/command", data).then(() => response);
        });
    }

    // Method to update a measurement's name and/or notes
    // This method uses a MeasurementSummary object with the title and/or notes field set to the new value
    async updateMeasurement(id: string, data: any): Promise<any> {
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
        const responseData = await response.json();
        return this._notifySubscribers(`/measurements/${id}`, data).then(() => responseData);
    }

    // Method to get IR window settings
    async getIRWindowSettings(id: string): Promise<any> {
        return await this.get(`http://localhost:4735/measurements/${id}/ir-windows`);
    }

    // Method to update IR window settings
    async updateIRWindowSettings(id: string, data: any): Promise<any> {
        return await this.put(`http://localhost:4735/measurements/${id}/ir-windows`, data);
    }

    // Method to get REW View preferences
    async getREWViewPreferences(): Promise<any> {
        return await this.get("http://localhost:4735/application/settings");
    }

    // Method to get distortion data
    async getDistortion(id: string, unit: string = "percent", ppo: number = 3): Promise<any> {
        let endpoint = `/measurements/${id}/distortion?unit=${unit}&ppo=${ppo}`;
        return await this.get(endpoint);
    }

    // Method to handle the Smooth command
    async smooth(id: string, smoothing: string = "1/3"): Promise<any> {
        const content = {
            command: "Smooth",
            parameters: {
                smoothing: smoothing
            }
        };
        const endpoint = `/measurements/${id}/command`;
        return await this.post(endpoint, content).then(response => {
            return this._notifySubscribers(endpoint, content).then(() => response);
        });
    }

    // Methods for handling groups and their measurements
    async getGroups(): Promise<any> {
        return await this.get("http://localhost:4735/groups");
    }

    async getGroup(uuid: string): Promise<any> {
        return await this.get(`http://localhost:4735/groups/${uuid}`);
    }

    async createGroup(data: any): Promise<any> {
        return await this.post("http://localhost:4735/groups", data).then(response => {
            return this._notifySubscribers("/groups", data).then(() => response);
        });
    }

    async updateGroup(uuid: string, data: any): Promise<any> {
        return await this.put(`http://localhost:4735/groups/${uuid}`, data).then(response => {
            return this._notifySubscribers(`/groups/${uuid}`, data).then(() => response);
        });
    }

    async deleteGroup(uuid: string): Promise<any> {
        const response = await fetch(`http://localhost:4735/groups/${uuid}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const responseData = await response.json();
        return this._notifySubscribers(`/groups/${uuid}`, {}).then(() => responseData);
    }

    async deleteAllGroups(): Promise<any> {
        const response = await fetch("http://localhost:4735/groups", {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const responseData = await response.json();
        return this._notifySubscribers("/groups", {}).then(() => responseData);
    }

    async getGroupMeasurements(uuid: string): Promise<any> {
        return await this.get(`http://localhost:4735/groups/${uuid}/measurements`);
    }

    async addMeasurementToGroup(uuid: string, data: any): Promise<any> {
        return await this.post(`http://localhost:4735/groups/${uuid}/measurements`, data).then(response => {
            return this._notifySubscribers(`/groups/${uuid}/measurements`, data).then(() => response);
        });
    }

    async removeMeasurementFromGroup(uuid: string, measurementUuid: string): Promise<any> {
        const response = await fetch(`http://localhost:4735/groups/${uuid}/measurements/${measurementUuid}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const responseData = await response.json();
        return this._notifySubscribers(`/groups/${uuid}/measurements/${measurementUuid}`, {}).then(() => responseData);
    }

    // Method to handle the Offset t=0 command
    async offsetTZero(offset: string, unit: string): Promise<any> {
        const content = {
            command: "Offset t=0",
            parameters: {
                offset: offset,
                unit: unit
            }
        };
        return await this.post("http://localhost:4735/offset-t-zero", content).then(response => {
            return this._notifySubscribers("/offset-t-zero", content).then(() => response);
        });
    }

    // Method to handle group change subscriptions
    async subscribeToGroupChanges(data: any): Promise<any> {
        return await this.post("http://localhost:4735/groups/subscribe", data);
    }

    // Method to handle group change unsubscriptions
    async unsubscribeFromGroupChanges(data: any): Promise<any> {
        return await this.post("http://localhost:4735/groups/unsubscribe", data);
    }

    // Method to get the list of group subscribers
    async getGroupSubscribers(): Promise<any> {
        return await this.get("http://localhost:4735/groups/subscribers");
    }

    // Method to handle error subscriptions
    async subscribeToErrors(data: any): Promise<any> {
        return await this.post("http://localhost:4735/application/subscribe-errors", data);
    }

    // Method to handle warning subscriptions
    async subscribeToWarnings(data: any): Promise<any> {
        return await this.post("http://localhost:4735/application/subscribe-warnings", data);
    }

    // Private method to notify subscribers
    private async _notifySubscribers(endpoint: string, content: any): Promise<void> {
        const subscribers = await this.getSubscribers();
        const tasks = subscribers.map((subscriber: any) => {
            const url = subscriber.url;
            return this.post(url + endpoint, content);
        });
        await Promise.all(tasks);
    }
}

export default REWApi;
