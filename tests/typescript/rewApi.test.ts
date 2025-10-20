// TypeScript REW API Connection Tests
import REWApi from '../../src/ts/rewApi';

describe('REWApi TypeScript Connection Tests', () => {
    let api: REWApi;
    const CONFIG = (global as any).TEST_CONFIG;

    beforeEach(() => {
        api = new REWApi();
        jest.setTimeout(CONFIG.REW_TIMEOUT);
    });

    test('should create REWApi instance', () => {
        expect(api).toBeInstanceOf(REWApi);
    });

    if (!CONFIG.SKIP_REAL_API_TESTS) {
        test('should connect to REW API and get application settings', async () => {
            try {
                const settings = await api.getApplicationSettings();
                expect(settings).toBeDefined();
                console.log('REW Application Settings received:', Object.keys(settings));
            } catch (error) {
                console.error('Connection failed:', error);
                throw error;
            }
        });

        test('should get measurements list', async () => {
            try {
                const measurements = await api.getMeasurements();
                expect(Array.isArray(measurements)).toBe(true);
                console.log(`Found ${measurements.length} measurements`);
                
                if (measurements.length > 0) {
                    console.log('First measurement:', {
                        title: measurements[0].title,
                        uuid: measurements[0].uuid
                    });
                }
            } catch (error) {
                console.error('Failed to get measurements:', error);
                throw error;
            }
        });

        test('should get audio settings', async () => {
            try {
                const audioSettings = await api.getAudioSettings();
                expect(audioSettings).toBeDefined();
                console.log('Audio Settings keys:', Object.keys(audioSettings));
            } catch (error) {
                console.error('Failed to get audio settings:', error);
                throw error;
            }
        });

        test('should get frequency response for first measurement', async () => {
            try {
                const measurements = await api.getMeasurements();
                if (measurements.length === 0) {
                    console.log('No measurements available for frequency response test');
                    return;
                }

                const firstMeasurement = measurements[0];
                const frequencyResponse = await api.getFrequencyResponse(firstMeasurement.uuid);
                
                expect(frequencyResponse).toBeDefined();
                expect(frequencyResponse.frequencies).toBeDefined();
                expect(frequencyResponse.values).toBeDefined();
                expect(Array.isArray(frequencyResponse.frequencies)).toBe(true);
                expect(Array.isArray(frequencyResponse.values)).toBe(true);
                
                console.log(`Frequency response data points: ${frequencyResponse.frequencies.length}`);
            } catch (error) {
                console.error('Failed to get frequency response:', error);
                throw error;
            }
        });

        test('should get impulse response for first measurement', async () => {
            try {
                const measurements = await api.getMeasurements();
                if (measurements.length === 0) {
                    console.log('No measurements available for impulse response test');
                    return;
                }

                const firstMeasurement = measurements[0];
                const impulseResponse = await api.getImpulseResponse(firstMeasurement.uuid);
                
                expect(impulseResponse).toBeDefined();
                expect(impulseResponse.times).toBeDefined();
                expect(impulseResponse.values).toBeDefined();
                expect(Array.isArray(impulseResponse.times)).toBe(true);
                expect(Array.isArray(impulseResponse.values)).toBe(true);
                
                console.log(`Impulse response data points: ${impulseResponse.times.length}`);
            } catch (error) {
                console.error('Failed to get impulse response:', error);
                throw error;
            }
        });
    } else {
        test('skipping real API tests (SKIP_REAL_API_TESTS=true)', () => {
            console.log('Real API tests skipped via environment variable');
            expect(true).toBe(true);
        });
    }
});

describe('REWApi TypeScript Graph Update Inhibition', () => {
    let api: REWApi;

    beforeEach(() => {
        api = new REWApi();
    });

    test('should handle graph update inhibition', () => {
        // Test initial state
        expect(api['graphUpdateInhibited']).toBe(false);

        // Test setting inhibition
        api.setGraphUpdateInhibition(true);
        expect(api['graphUpdateInhibited']).toBe(true);

        // Test clearing inhibition
        api.setGraphUpdateInhibition(false);
        expect(api['graphUpdateInhibited']).toBe(false);
    });

    test('should return null when graph update is inhibited', async () => {
        api.setGraphUpdateInhibition(true);
        
        const result = await api.get('test-endpoint');
        expect(result).toBe(null);
        
        const putResult = await api.put('test-endpoint', {});
        expect(putResult).toBe(null);
        
        const postResult = await api.post('test-endpoint', {});
        expect(postResult).toBe(null);
    });
});

describe('REWApi TypeScript Data Encoding/Decoding', () => {
    let api: REWApi;

    beforeEach(() => {
        api = new REWApi();
    });

    test('should encode and decode Float32Array correctly', () => {
        const originalArray = new Float32Array([1.0, 2.5, -3.7, 0.0, 1000.123]);
        
        const encoded = api.encodeArray(originalArray);
        expect(typeof encoded).toBe('string');
        expect(encoded.length).toBeGreaterThan(0);
        
        const decoded = api.decodeArray(encoded);
        expect(decoded).toBeInstanceOf(Float32Array);
        expect(decoded.length).toBe(originalArray.length);
        
        // Check values with small tolerance for floating point precision
        for (let i = 0; i < originalArray.length; i++) {
            expect(decoded[i]).toBeCloseTo(originalArray[i], 5);
        }
    });

    test('should handle empty Float32Array', () => {
        const emptyArray = new Float32Array(0);
        
        const encoded = api.encodeArray(emptyArray);
        const decoded = api.decodeArray(encoded);
        
        expect(decoded.length).toBe(0);
    });
});

describe('REWApi TypeScript Measurement Command Validation', () => {
    let api: REWApi;

    beforeEach(() => {
        api = new REWApi();
    });

    test('should validate Smooth command parameters', async () => {
        const validParams = {
            "include cal": true,
            "append lf tail": false,
            "append hf tail": false,
            "replicate data": true,
            "smoothing": "1/3"
        };

        // Mock the post method and _notifySubscribers to avoid actual API calls
        const postSpy = jest.spyOn(api, 'post').mockResolvedValue({ success: true });
        const notifySubscribersSpy = jest.spyOn(api as any, '_notifySubscribers').mockResolvedValue(undefined);

        const result = await api.issueMeasurementCommandWithParams('test-id', 'Smooth', validParams);
        expect(result).toEqual({ success: true });
        
        // Restore original methods
        postSpy.mockRestore();
        notifySubscribersSpy.mockRestore();
    });

    test('should validate and reject invalid Smooth command parameters', async () => {
        const invalidParams = {
            "include cal": "not-a-boolean", // Invalid type
            "smoothing": "1/3"
        };

        const result = await api.issueMeasurementCommandWithParams('test-id', 'Smooth', invalidParams);
        expect(typeof result).toBe('string');
        expect(result).toContain('Error');
        expect(result).toContain('include cal');
    });

    test('should validate Generate RT60 command parameters', async () => {
        const validParams = {
            "include cal": true,
            "append lf tail": false,
            "append hf tail": false,
            "replicate data": true,
            "octaveFrac": 3,
            "filterOrder": 4,
            "zeroPhaseFiltered": true,
            "reverseFiltered": false
        };

        // Mock the post method and _notifySubscribers to avoid actual API calls
        const postSpy = jest.spyOn(api, 'post').mockResolvedValue({ success: true });
        const notifySubscribersSpy = jest.spyOn(api as any, '_notifySubscribers').mockResolvedValue(undefined);

        const result = await api.issueMeasurementCommandWithParams('test-id', 'Generate RT60', validParams);
        expect(result).toEqual({ success: true });
        
        // Restore original methods
        postSpy.mockRestore();
        notifySubscribersSpy.mockRestore();
    });
});