import fetch from 'node-fetch';

class REWApi {
    private graphUpdateInhibited: boolean = false;

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
        return await response.json();
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
        return await this.post("http://localhost:4735/measurements", data);
    }

    // Methods for handling frequency response
    async getFrequencyResponse(): Promise<any> {
        return await this.get("http://localhost:4735/frequencyresponse");
    }

    // Methods for handling impulse response
    async getImpulseResponse(): Promise<any> {
        return await this.get("http://localhost:4735/impulseresponse");
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
        for (let i = 0; i < array.length; i++) {
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
}

export default REWApi;
