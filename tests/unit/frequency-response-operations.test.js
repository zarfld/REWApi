/**
 * Phase 1 Test Suite: Frequency Response Operations
 * 
 * @implements REQ-F-005 - Frequency Response Analysis
 * @implements REQ-F-005.1 - Frequency Response Data Retrieval
 * @implements REQ-F-005.2 - Frequency Response Processing
 * @traces StR-002 - Audio analysis and measurement requirements
 * 
 * Following IEEE 1012-2016 V&V standards and test-gap-filler methodology
 * Coverage Target: 40% (Phase 1 of 4-phase testing strategy)
 * 
 * Priority: P0 - CRITICAL (Core audio analysis)
 * Risk Level: HIGH (Essential REW measurement functionality)
 */

import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import { REWApi } from '../../src/js/rewApi.js';
import { testFixtures } from '../fixtures/frequency-response-fixtures.js';

describe('Frequency Response Operations - REQ-F-005 (Phase 1)', () => {
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

  describe('Frequency Response Data Retrieval - REQ-F-005.1', () => {
    
    // TEST-FREQ-001-01: Get frequency response data successfully
    it('should retrieve frequency response data for measurement', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const expectedFreqResponse = testFixtures.frequencyResponse.validData;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedFreqResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponse(measurementId);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response`,
        expect.objectContaining({ method: 'GET' })
      );
      expect(result).toEqual(expectedFreqResponse);
      expect(result.frequencies).toBeDefined();
      expect(result.magnitude).toBeDefined();
      expect(result.phase).toBeDefined();
      expect(Array.isArray(result.frequencies)).toBe(true);
      expect(Array.isArray(result.magnitude)).toBe(true);
      expect(Array.isArray(result.phase)).toBe(true);
    });
    
    // TEST-FREQ-001-02: Handle non-existent measurement
    it('should handle frequency response request for non-existent measurement', async () => {
      // Arrange
      const nonExistentId = 'non-existent-id';
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement not found' }),
        { status: 404, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getFrequencyResponse(nonExistentId)).rejects.toThrow();
    });
    
    // TEST-FREQ-001-03: Validate frequency response data structure
    it('should validate frequency response data structure', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const freqResponse = testFixtures.frequencyResponse.validData;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(freqResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponse(measurementId);
      
      // Assert
      expect(result).toHaveProperty('frequencies');
      expect(result).toHaveProperty('magnitude');
      expect(result).toHaveProperty('phase');
      expect(result).toHaveProperty('sampleRate');
      expect(result).toHaveProperty('fftSize');
      expect(result.frequencies.length).toBe(result.magnitude.length);
      expect(result.frequencies.length).toBe(result.phase.length);
      expect(typeof result.sampleRate).toBe('number');
      expect(typeof result.fftSize).toBe('number');
    });
    
    // TEST-FREQ-001-04: Handle incomplete frequency response data
    it('should handle incomplete frequency response data', async () => {
      // Arrange
      const measurementId = 'meas-incomplete';
      const incompleteData = testFixtures.frequencyResponse.incomplete;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(incompleteData),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getFrequencyResponse(measurementId)).rejects.toThrow();
    });
    
    // TEST-FREQ-001-05: Get frequency response with specific parameters
    it('should retrieve frequency response with frequency range parameters', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const startFreq = 20;
      const endFreq = 20000;
      const expectedFreqResponse = testFixtures.frequencyResponse.ranged;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedFreqResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponse(measurementId, { startFreq, endFreq });
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response?startFreq=${startFreq}&endFreq=${endFreq}`,
        expect.objectContaining({ method: 'GET' })
      );
      expect(result.frequencies[0]).toBeGreaterThanOrEqual(startFreq);
      expect(result.frequencies[result.frequencies.length - 1]).toBeLessThanOrEqual(endFreq);
    });
  });

  describe('Frequency Response Processing - REQ-F-005.2', () => {
    
    // TEST-FREQ-002-01: Apply smoothing to frequency response
    it('should apply smoothing to frequency response data', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const smoothingFactor = 6; // 1/6 octave smoothing
      const expectedSmoothed = testFixtures.frequencyResponse.smoothed;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedSmoothed),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getSmoothingFrequencyResponse(measurementId, smoothingFactor);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response/smooth`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ smoothingFactor })
        })
      );
      expect(result.smoothingApplied).toBe(true);
      expect(result.smoothingFactor).toBe(smoothingFactor);
    });
    
    // TEST-FREQ-002-02: Calculate frequency response statistics
    it('should calculate frequency response statistics', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const expectedStats = testFixtures.frequencyResponse.statistics;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedStats),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponseStatistics(measurementId);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response/statistics`,
        expect.objectContaining({ method: 'GET' })
      );
      expect(result).toHaveProperty('averageLevel');
      expect(result).toHaveProperty('peakLevel');
      expect(result).toHaveProperty('valleyLevel');
      expect(result).toHaveProperty('dynamicRange');
      expect(result).toHaveProperty('flatnessScore');
      expect(typeof result.averageLevel).toBe('number');
      expect(typeof result.peakLevel).toBe('number');
      expect(typeof result.valleyLevel).toBe('number');
      expect(typeof result.dynamicRange).toBe('number');
      expect(typeof result.flatnessScore).toBe('number');
    });
    
    // TEST-FREQ-002-03: Compare frequency responses
    it('should compare two frequency responses', async () => {
      // Arrange
      const measurementId1 = 'meas-123';
      const measurementId2 = 'meas-456';
      const expectedComparison = testFixtures.frequencyResponse.comparison;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedComparison),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.compareFrequencyResponses(measurementId1, measurementId2);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:4735/frequency-response/compare',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ 
            measurement1: measurementId1, 
            measurement2: measurementId2 
          })
        })
      );
      expect(result).toHaveProperty('difference');
      expect(result).toHaveProperty('correlation');
      expect(result).toHaveProperty('similarities');
      expect(Array.isArray(result.difference.magnitude)).toBe(true);
      expect(Array.isArray(result.difference.phase)).toBe(true);
    });
    
    // TEST-FREQ-002-04: Export frequency response data
    it('should export frequency response data in specified format', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const format = 'csv';
      const expectedExport = testFixtures.frequencyResponse.exportData.csv;
      mockFetch.mockResolvedValueOnce(new Response(
        expectedExport,
        { 
          status: 200, 
          headers: { 
            'Content-Type': 'text/csv',
            'Content-Disposition': 'attachment; filename="frequency-response.csv"'
          } 
        }
      ));
      
      // Act
      const result = await api.exportFrequencyResponse(measurementId, format);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response/export`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ format })
        })
      );
      expect(typeof result).toBe('string');
      expect(result).toContain('Frequency,Magnitude,Phase');
    });
    
    // TEST-FREQ-002-05: Apply frequency response correction
    it('should apply frequency response correction', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const correctionCurve = testFixtures.frequencyResponse.correctionCurve;
      const expectedCorrected = testFixtures.frequencyResponse.corrected;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedCorrected),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.applyFrequencyResponseCorrection(measurementId, correctionCurve);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response/correct`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ correctionCurve })
        })
      );
      expect(result.correctionApplied).toBe(true);
      expect(result.originalMagnitude).toBeDefined();
      expect(result.correctedMagnitude).toBeDefined();
    });
  });

  describe('Frequency Response Analysis - REQ-F-005.3', () => {
    
    // TEST-FREQ-003-01: Identify frequency response peaks
    it('should identify peaks in frequency response', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const threshold = 3; // dB threshold for peak detection
      const expectedPeaks = testFixtures.frequencyResponse.peaks;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedPeaks),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.findFrequencyResponsePeaks(measurementId, threshold);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/frequency-response/peaks`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ threshold })
        })
      );
      expect(Array.isArray(result.peaks)).toBe(true);
      if (result.peaks.length > 0) {
        const peak = result.peaks[0];
        expect(peak).toHaveProperty('frequency');
        expect(peak).toHaveProperty('magnitude');
        expect(peak).toHaveProperty('qFactor');
        expect(typeof peak.frequency).toBe('number');
        expect(typeof peak.magnitude).toBe('number');
      }
    });
    
    // TEST-FREQ-003-02: Identify frequency response dips
    it('should identify dips in frequency response', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const threshold = -3; // dB threshold for dip detection
      const expectedDips = testFixtures.frequencyResponse.dips;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedDips),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.findFrequencyResponseDips(measurementId, threshold);
      
      // Assert
      expect(Array.isArray(result.dips)).toBe(true);
      if (result.dips.length > 0) {
        const dip = result.dips[0];
        expect(dip).toHaveProperty('frequency');
        expect(dip).toHaveProperty('magnitude');
        expect(dip).toHaveProperty('qFactor');
        expect(dip.magnitude).toBeLessThan(0); // Dips should be negative
      }
    });
    
    // TEST-FREQ-003-03: Calculate frequency response curve fitting
    it('should calculate curve fitting for frequency response', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const targetCurve = testFixtures.frequencyResponse.targetCurve;
      const expectedFitting = testFixtures.frequencyResponse.curveFitting;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedFitting),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.calculateCurveFitting(measurementId, targetCurve);
      
      // Assert
      expect(result).toHaveProperty('rSquared');
      expect(result).toHaveProperty('rmse');
      expect(result).toHaveProperty('fittedCurve');
      expect(result).toHaveProperty('residuals');
      expect(typeof result.rSquared).toBe('number');
      expect(typeof result.rmse).toBe('number');
      expect(result.rSquared).toBeGreaterThanOrEqual(0);
      expect(result.rSquared).toBeLessThanOrEqual(1);
    });
    
    // TEST-FREQ-003-04: Generate frequency response report
    it('should generate comprehensive frequency response report', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const reportOptions = {
        includeStatistics: true,
        includePeaksAndDips: true,
        includeSmoothing: true,
        smoothingFactor: 6
      };
      const expectedReport = testFixtures.frequencyResponse.report;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedReport),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.generateFrequencyResponseReport(measurementId, reportOptions);
      
      // Assert
      expect(result).toHaveProperty('summary');
      expect(result).toHaveProperty('statistics');
      expect(result).toHaveProperty('peaks');
      expect(result).toHaveProperty('dips');
      expect(result).toHaveProperty('recommendations');
      expect(result.summary).toHaveProperty('overallScore');
      expect(result.summary).toHaveProperty('flatnessRating');
      expect(Array.isArray(result.recommendations)).toBe(true);
    });
  });

  describe('Edge Cases and Performance', () => {
    
    // TEST-FREQ-004-01: Handle very large frequency response datasets
    it('should handle large frequency response datasets efficiently', async () => {
      // Arrange
      const measurementId = 'meas-large';
      const largeDataset = testFixtures.frequencyResponse.largeDataset;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(largeDataset),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const startTime = Date.now();
      const result = await api.getFrequencyResponse(measurementId);
      const endTime = Date.now();
      
      // Assert
      expect(endTime - startTime).toBeLessThan(5000); // Should complete within 5 seconds
      expect(result.frequencies.length).toBeGreaterThan(10000);
      expect(result.magnitude.length).toBe(result.frequencies.length);
      expect(result.phase.length).toBe(result.frequencies.length);
    });
    
    // TEST-FREQ-004-02: Handle extreme frequency ranges
    it('should handle extreme frequency ranges', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const extremeRange = { startFreq: 0.1, endFreq: 1000000 }; // Very wide range
      const expectedResponse = testFixtures.frequencyResponse.extremeRange;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedResponse),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponse(measurementId, extremeRange);
      
      // Assert
      expect(result.frequencies[0]).toBeGreaterThanOrEqual(extremeRange.startFreq);
      expect(result.frequencies[result.frequencies.length - 1]).toBeLessThanOrEqual(extremeRange.endFreq);
    });
    
    // TEST-FREQ-004-03: Handle corrupted frequency response data
    it('should handle corrupted frequency response data', async () => {
      // Arrange
      const measurementId = 'meas-corrupted';
      const corruptedData = testFixtures.frequencyResponse.corrupted;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(corruptedData),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getFrequencyResponse(measurementId)).rejects.toThrow();
    });
    
    // TEST-FREQ-004-04: Handle concurrent frequency response operations
    it('should handle concurrent frequency response operations', async () => {
      // Arrange
      const measurementIds = ['meas-1', 'meas-2', 'meas-3'];
      const responses = measurementIds.map((id, index) => ({
        ...testFixtures.frequencyResponse.validData,
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
      const promises = measurementIds.map(id => api.getFrequencyResponse(id));
      const results = await Promise.all(promises);
      
      // Assert
      expect(results).toHaveLength(3);
      expect(mockFetch).toHaveBeenCalledTimes(3);
      results.forEach((result, index) => {
        expect(result.measurementId).toBe(measurementIds[index]);
      });
    });
    
    // TEST-FREQ-004-05: Validate frequency response data integrity
    it('should validate frequency response data integrity', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const validData = testFixtures.frequencyResponse.validData;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(validData),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getFrequencyResponse(measurementId);
      
      // Assert
      // Check that frequency values are monotonically increasing
      for (let i = 1; i < result.frequencies.length; i++) {
        expect(result.frequencies[i]).toBeGreaterThan(result.frequencies[i - 1]);
      }
      
      // Check that magnitude values are within reasonable range (-200 to +50 dB)
      result.magnitude.forEach(mag => {
        expect(mag).toBeGreaterThanOrEqual(-200);
        expect(mag).toBeLessThanOrEqual(50);
      });
      
      // Check that phase values are within expected range (-180 to +180 degrees)
      result.phase.forEach(phase => {
        expect(phase).toBeGreaterThanOrEqual(-180);
        expect(phase).toBeLessThanOrEqual(180);
      });
    });
  });
});