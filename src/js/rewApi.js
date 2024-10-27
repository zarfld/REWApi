const fetch = require('node-fetch');

class REWApi {
    async get(endpoint) {
        const response = await fetch(endpoint);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    async put(endpoint, data) {
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
    async getFrequencyResponse() {
        return await this.get("http://localhost:4735/frequencyresponse");
    }

    // Methods for handling impulse response
    async getImpulseResponse() {
        return await this.get("http://localhost:4735/impulseresponse");
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
}

module.exports = REWApi;
