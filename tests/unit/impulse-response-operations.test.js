/**
 * Phase 1 Test Suite: Impulse Response Operations
 * 
 * @implements REQ-F-006 - Impulse Response Analysis
 * @implements REQ-F-006.1 - Impulse Response Data Retrieval
 * @implements REQ-F-006.2 - Impulse Response Processing
 * @traces StR-003 - Time-domain analysis requirements
 * 
 * Following IEEE 1012-2016 V&V standards and test-gap-filler methodology
 * Coverage Target: 40% (Phase 1 of 4-phase testing strategy)
 * 
 * Priority: P0 - CRITICAL (Core time-domain analysis)
 * Risk Level: HIGH (Essential acoustic measurement functionality)
 */

import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { REWApi } from '../../src/js/rewApi.js';
import { testFixtures } from '../fixtures/impulse-response-fixtures.js';

describe('Impulse Response Operations - REQ-F-006 (Phase 1)', () => {
  let api;
  let mockFetch;
  
  beforeEach(() => {
    // Arrange: Fresh API instance for each test
    api = new REWApi();
    
    // Mock fetch for HTTP operations
    mockFetch = jest.fn();
    global.fetch = mockFetch;
    
    // Reset all mocks
    jest.clearAllMocks();
  });
  
  afterEach(() => {
    // Cleanup: Restore original fetch
    jest.restoreAllMocks();
  });

  describe('Impulse Response Data Retrieval - REQ-F-006.1', () => {
    
    // TEST-IMPULSE-001-01: Get impulse response data successfully
    it('should retrieve impulse response data for measurement', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const expectedImpulseResponse = testFixtures.impulseResponse.validData;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedImpulseResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getImpulseResponse(measurementId);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/impulse-response`,
        expect.objectContaining({ method: 'GET' })
      );
      expect(result).toEqual(expectedImpulseResponse);
      expect(result.timeData).toBeDefined();
      expect(result.sampleRate).toBeDefined();
      expect(result.length).toBeDefined();
      expect(Array.isArray(result.timeData)).toBe(true);
      expect(typeof result.sampleRate).toBe('number');
      expect(typeof result.length).toBe('number');
    });
    
    // TEST-IMPULSE-001-02: Handle non-existent measurement
    it('should handle impulse response request for non-existent measurement', async () => {
      // Arrange
      const nonExistentId = 'non-existent-id';
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement not found' }),
        { status: 404, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getImpulseResponse(nonExistentId)).rejects.toThrow();
    });
    
    // TEST-IMPULSE-001-03: Validate impulse response data structure
    it('should validate impulse response data structure', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const impulseResponse = testFixtures.impulseResponse.validData;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(impulseResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getImpulseResponse(measurementId);
      
      // Assert
      expect(result).toHaveProperty('timeData');
      expect(result).toHaveProperty('sampleRate');
      expect(result).toHaveProperty('length');
      expect(result).toHaveProperty('peakTime');
      expect(result).toHaveProperty('peakAmplitude');
      expect(result.timeData.length).toBe(result.length);
      expect(typeof result.peakTime).toBe('number');
      expect(typeof result.peakAmplitude).toBe('number');
    });
    
    // TEST-IMPULSE-001-04: Get windowed impulse response
    it('should retrieve windowed impulse response data', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const windowType = 'hanning';
      const windowLength = 1024;
      const expectedWindowed = testFixtures.impulseResponse.windowed;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedWindowed),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getWindowedImpulseResponse(measurementId, windowType, windowLength);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/impulse-response/windowed`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ windowType, windowLength })
        })
      );
      expect(result.windowApplied).toBe(true);
      expect(result.windowType).toBe(windowType);
      expect(result.windowLength).toBe(windowLength);
    });
    
    // TEST-IMPULSE-001-05: Handle corrupted impulse response data
    it('should handle corrupted impulse response data', async () => {
      // Arrange
      const measurementId = 'meas-corrupted';
      const corruptedData = testFixtures.impulseResponse.corrupted;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(corruptedData),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getImpulseResponse(measurementId)).rejects.toThrow();
    });
  });

  describe('Impulse Response Processing - REQ-F-006.2', () => {
    
    // TEST-IMPULSE-002-01: Calculate RT60 reverberation time
    it('should calculate RT60 reverberation time', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const frequencyBands = ['125', '250', '500', '1000', '2000', '4000'];
      const expectedRT60 = testFixtures.impulseResponse.rt60;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedRT60),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateRT60(measurementId, frequencyBands);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/impulse-response/rt60`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ frequencyBands })
        })
      );
      expect(result).toHaveProperty('rt60Values');
      expect(result).toHaveProperty('frequencyBands');
      expect(result).toHaveProperty('averageRT60');
      expect(Array.isArray(result.rt60Values)).toBe(true);
      expect(result.rt60Values.length).toBe(frequencyBands.length);
    });
    
    // TEST-IMPULSE-002-02: Calculate EDT (Early Decay Time)
    it('should calculate EDT (Early Decay Time)', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const frequencyBands = ['125', '250', '500', '1000', '2000', '4000'];
      const expectedEDT = testFixtures.impulseResponse.edt;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedEDT),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateEDT(measurementId, frequencyBands);
      
      // Assert
      expect(result).toHaveProperty('edtValues');
      expect(result).toHaveProperty('frequencyBands');
      expect(result).toHaveProperty('averageEDT');
      expect(Array.isArray(result.edtValues)).toBe(true);
      expect(result.edtValues.length).toBe(frequencyBands.length);
    });
    
    // TEST-IMPULSE-002-03: Calculate C80 (Clarity)
    it('should calculate C80 clarity values', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const frequencyBands = ['125', '250', '500', '1000', '2000', '4000'];
      const expectedC80 = testFixtures.impulseResponse.c80;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedC80),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateC80(measurementId, frequencyBands);
      
      // Assert
      expect(result).toHaveProperty('c80Values');
      expect(result).toHaveProperty('frequencyBands');
      expect(result).toHaveProperty('averageC80');
      expect(Array.isArray(result.c80Values)).toBe(true);
      expect(result.c80Values.length).toBe(frequencyBands.length);
      
      // C80 values should be in typical range (-5 to +10 dB)
      result.c80Values.forEach(value => {
        expect(value).toBeGreaterThanOrEqual(-10);
        expect(value).toBeLessThanOrEqual(15);
      });
    });
    
    // TEST-IMPULSE-002-04: Calculate D50 (Definition)
    it('should calculate D50 definition values', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const frequencyBands = ['125', '250', '500', '1000', '2000', '4000'];
      const expectedD50 = testFixtures.impulseResponse.d50;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedD50),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateD50(measurementId, frequencyBands);
      
      // Assert
      expect(result).toHaveProperty('d50Values');
      expect(result).toHaveProperty('frequencyBands');
      expect(result).toHaveProperty('averageD50');
      expect(Array.isArray(result.d50Values)).toBe(true);
      expect(result.d50Values.length).toBe(frequencyBands.length);
      
      // D50 values should be between 0 and 1 (percentage)
      result.d50Values.forEach(value => {
        expect(value).toBeGreaterThanOrEqual(0);
        expect(value).toBeLessThanOrEqual(1);
      });
    });
    
    // TEST-IMPULSE-002-05: Calculate Center Time (Ts)
    it('should calculate Center Time (Ts)', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const frequencyBands = ['125', '250', '500', '1000', '2000', '4000'];
      const expectedTs = testFixtures.impulseResponse.ts;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedTs),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateCenterTime(measurementId, frequencyBands);
      
      // Assert
      expect(result).toHaveProperty('tsValues');
      expect(result).toHaveProperty('frequencyBands');
      expect(result).toHaveProperty('averageTs');
      expect(Array.isArray(result.tsValues)).toBe(true);
      expect(result.tsValues.length).toBe(frequencyBands.length);
      
      // Ts values should be positive (milliseconds)
      result.tsValues.forEach(value => {
        expect(value).toBeGreaterThan(0);
        expect(value).toBeLessThan(1000); // Should be less than 1 second
      });
    });
  });

  describe('Impulse Response Analysis - REQ-F-006.3', () => {
    
    // TEST-IMPULSE-003-01: Detect early reflections
    it('should detect early reflections in impulse response', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const threshold = -20; // dB below peak
      const timeWindow = 80; // ms
      const expectedReflections = testFixtures.impulseResponse.earlyReflections;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedReflections),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.detectEarlyReflections(measurementId, threshold, timeWindow);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/impulse-response/early-reflections`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ threshold, timeWindow })
        })
      );
      expect(result).toHaveProperty('reflections');
      expect(result).toHaveProperty('analysisParameters');
      expect(Array.isArray(result.reflections)).toBe(true);
      
      if (result.reflections.length > 0) {
        const reflection = result.reflections[0];
        expect(reflection).toHaveProperty('time');
        expect(reflection).toHaveProperty('amplitude');
        expect(reflection).toHaveProperty('delay');
        expect(typeof reflection.time).toBe('number');
        expect(typeof reflection.amplitude).toBe('number');
        expect(typeof reflection.delay).toBe('number');
      }
    });
    
    // TEST-IMPULSE-003-02: Calculate room volume estimation
    it('should estimate room volume from impulse response', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const expectedVolume = testFixtures.impulseResponse.roomVolume;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedVolume),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.estimateRoomVolume(measurementId);
      
      // Assert
      expect(result).toHaveProperty('estimatedVolume');
      expect(result).toHaveProperty('confidence');
      expect(result).toHaveProperty('method');
      expect(typeof result.estimatedVolume).toBe('number');
      expect(typeof result.confidence).toBe('number');
      expect(result.estimatedVolume).toBeGreaterThan(0);
      expect(result.confidence).toBeGreaterThanOrEqual(0);
      expect(result.confidence).toBeLessThanOrEqual(1);
    });
    
    // TEST-IMPULSE-003-03: Calculate IACC (Inter-Aural Cross Correlation)
    it('should calculate IACC for stereo measurements', async () => {
      // Arrange
      const measurementId = 'meas-stereo-123';
      const expectedIACC = testFixtures.impulseResponse.iacc;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedIACC),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateIACC(measurementId);
      
      // Assert
      expect(result).toHaveProperty('iaccEarly');
      expect(result).toHaveProperty('iaccLate');
      expect(result).toHaveProperty('iaccE3');
      expect(result).toHaveProperty('frequencyBands');
      expect(typeof result.iaccEarly).toBe('number');
      expect(typeof result.iaccLate).toBe('number');
      expect(typeof result.iaccE3).toBe('number');
      
      // IACC values should be between -1 and 1
      expect(result.iaccEarly).toBeGreaterThanOrEqual(-1);
      expect(result.iaccEarly).toBeLessThanOrEqual(1);
      expect(result.iaccLate).toBeGreaterThanOrEqual(-1);
      expect(result.iaccLate).toBeLessThanOrEqual(1);
    });
    
    // TEST-IMPULSE-003-04: Generate comprehensive impulse response report
    it('should generate comprehensive impulse response report', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const reportOptions = {
        includeRT60: true,
        includeEDT: true,
        includeClarity: true,
        includeEarlyReflections: true,
        frequencyBands: ['125', '250', '500', '1000', '2000', '4000']
      };
      const expectedReport = testFixtures.impulseResponse.report;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedReport),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.generateImpulseResponseReport(measurementId, reportOptions);
      
      // Assert
      expect(result).toHaveProperty('summary');
      expect(result).toHaveProperty('rt60');
      expect(result).toHaveProperty('edt');
      expect(result).toHaveProperty('clarity');
      expect(result).toHaveProperty('earlyReflections');
      expect(result).toHaveProperty('recommendations');
      expect(result.summary).toHaveProperty('overallScore');
      expect(result.summary).toHaveProperty('acousticRating');
      expect(Array.isArray(result.recommendations)).toBe(true);
    });
  });

  describe('Impulse Response Export and Comparison', () => {
    
    // TEST-IMPULSE-004-01: Export impulse response data
    it('should export impulse response data in specified format', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const format = 'wav';
      const expectedExport = testFixtures.impulseResponse.exportData.wav;
      mockFetch.mockResolvedValueOnce(new Response(
        expectedExport,
        { 
          status: 200, 
          headers: { 
            'Content-Type': 'audio/wav',
            'Content-Disposition': 'attachment; filename="impulse-response.wav"'
          } 
        }
      ));
      
      // Act
      const result = await api.exportImpulseResponse(measurementId, format);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/impulse-response/export`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ format })
        })
      );
      expect(result).toBeDefined();
    });
    
    // TEST-IMPULSE-004-02: Compare impulse responses
    it('should compare two impulse responses', async () => {
      // Arrange
      const measurementId1 = 'meas-123';
      const measurementId2 = 'meas-456';
      const expectedComparison = testFixtures.impulseResponse.comparison;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedComparison),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.compareImpulseResponses(measurementId1, measurementId2);
      
      // Assert
      expect(result).toHaveProperty('rt60Comparison');
      expect(result).toHaveProperty('clarityComparison');
      expect(result).toHaveProperty('reflectionComparison');
      expect(result).toHaveProperty('overallSimilarity');
      expect(typeof result.overallSimilarity).toBe('number');
      expect(result.overallSimilarity).toBeGreaterThanOrEqual(0);
      expect(result.overallSimilarity).toBeLessThanOrEqual(1);
    });
  });

  describe('Edge Cases and Performance', () => {
    
    // TEST-IMPULSE-005-01: Handle very long impulse responses
    it('should handle very long impulse responses efficiently', async () => {
      // Arrange
      const measurementId = 'meas-long';
      const longImpulse = testFixtures.impulseResponse.longDuration;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(longImpulse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const startTime = Date.now();
      const result = await api.getImpulseResponse(measurementId);
      const endTime = Date.now();
      
      // Assert
      expect(endTime - startTime).toBeLessThan(5000); // Should complete within 5 seconds
      expect(result.timeData.length).toBeGreaterThan(100000);
      expect(result.length).toBe(result.timeData.length);
    });
    
    // TEST-IMPULSE-005-02: Handle very short impulse responses
    it('should handle very short impulse responses', async () => {
      // Arrange
      const measurementId = 'meas-short';
      const shortImpulse = testFixtures.impulseResponse.shortDuration;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(shortImpulse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getImpulseResponse(measurementId);
      
      // Assert
      expect(result.timeData.length).toBeLessThan(1000);
      expect(result.duration).toBeLessThan(0.1); // Less than 100ms
    });
    
    // TEST-IMPULSE-005-03: Handle impulse responses with very low signal levels
    it('should handle impulse responses with very low signal levels', async () => {
      // Arrange
      const measurementId = 'meas-quiet';
      const quietImpulse = testFixtures.impulseResponse.lowLevel;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(quietImpulse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getImpulseResponse(measurementId);
      
      // Assert
      expect(result.peakAmplitude).toBeLessThan(0.001); // Very quiet signal
      expect(result.snr).toBeLessThan(40); // Low signal-to-noise ratio
    });
    
    // TEST-IMPULSE-005-04: Handle concurrent impulse response operations
    it('should handle concurrent impulse response operations', async () => {
      // Arrange
      const measurementIds = ['meas-1', 'meas-2', 'meas-3'];
      const responses = measurementIds.map((id, index) => ({
        ...testFixtures.impulseResponse.validData,
        measurementId: id,
        timestamp: new Date(Date.now() + index * 1000).toISOString()
      }));
      
      responses.forEach(response => {
        mockFetch.mockResolvedValueOnce(new Response(
          JSON.stringify(response),
          { status: 200, headers: { 'Content-Type': 'application/json' } }
        ));
      });
      
      // Act
      const promises = measurementIds.map(id => api.getImpulseResponse(id));
      const results = await Promise.all(promises);
      
      // Assert
      expect(results).toHaveLength(3);
      expect(mockFetch).toHaveBeenCalledTimes(3);
      results.forEach((result, index) => {
        expect(result.measurementId).toBe(measurementIds[index]);
      });
    });
    
    // TEST-IMPULSE-005-05: Validate impulse response processing limits
    it('should respect processing limits and timeouts', async () => {
      // Arrange
      const measurementId = 'meas-processing-limit';
      const heavyProcessing = {
        frequencyBands: ['31.5', '63', '125', '250', '500', '1000', '2000', '4000', '8000', '16000'],
        analysisDepth: 'comprehensive',
        includeAllMetrics: true
      };
      
      // Simulate a slower response for heavy processing
      mockFetch.mockImplementationOnce(() => 
        new Promise(resolve => 
          setTimeout(() => resolve(new Response(
            JSON.stringify(testFixtures.impulseResponse.rt60),
            { status: 200, headers: { 'Content-Type': 'application/json' } }
          )), 2000)
        )
      );
      
      // Act
      const startTime = Date.now();
      const result = await api.calculateRT60(measurementId, heavyProcessing.frequencyBands);
      const endTime = Date.now();
      
      // Assert
      expect(endTime - startTime).toBeGreaterThan(1500); // Should take some time for heavy processing
      expect(endTime - startTime).toBeLessThan(10000); // But not too long
      expect(result).toBeDefined();
    });
  });
});