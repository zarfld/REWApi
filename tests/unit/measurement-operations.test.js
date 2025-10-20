/**
 * Phase 1 Test Suite: Core Measurement Operations
 * 
 * @implements REQ-F-004 - Measurement Operations
 * @implements REQ-F-004.1 - Measurement Retrieval  
 * @implements REQ-F-004.2 - Measurement Commands
 * @traces StR-001 - Core measurement functionality requirements
 * 
 * Following IEEE 1012-2016 V&V standards and test-gap-filler methodology
 * Coverage Target: 40% (Phase 1 of 4-phase testing strategy)
 * 
 * Priority: P0 - CRITICAL (Core business logic)
 * Risk Level: HIGH (Essential REW functionality)
 */

import { describe, it, expect, beforeEach, afterEach, jest } from '@jest/globals';
import REWApi from '../../src/js/rewApi.js';
import { testFixtures } from '../fixtures/measurement-fixtures.js';

describe('Core Measurement Operations - REQ-F-004 (Phase 1)', () => {
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

  describe('Measurement Retrieval - REQ-F-004.1', () => {
    
    // TEST-MEAS-001-01: Happy path measurement retrieval
    it('should get all measurements successfully', async () => {
      // Arrange
      const expectedMeasurements = testFixtures.measurements.validList;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(expectedMeasurements),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getMeasurements();
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:4735/measurements',
        expect.objectContaining({ method: 'GET' })
      );
      expect(result).toEqual(expectedMeasurements);
      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBeGreaterThan(0);
    });
    
    // TEST-MEAS-001-02: Empty measurement list handling
    it('should handle empty measurement list gracefully', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify([]),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getMeasurements();
      
      // Assert
      expect(result).toEqual([]);
      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBe(0);
    });
    
    // TEST-MEAS-001-03: Server error handling
    it('should handle server errors appropriately', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Internal server error' }),
        { status: 500, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getMeasurements()).rejects.toThrow();
    });
    
    // TEST-MEAS-001-04: Network failure handling
    it('should handle network failures', async () => {
      // Arrange
      mockFetch.mockRejectedValueOnce(new Error('Network error'));
      
      // Act & Assert
      await expect(api.getMeasurements()).rejects.toThrow('Network error');
    });
    
    // TEST-MEAS-001-05: Invalid JSON response handling
    it('should handle invalid JSON responses', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce(new Response(
        'Invalid JSON{',
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.getMeasurements()).rejects.toThrow();
    });
    
    // TEST-MEAS-001-06: Measurement data structure validation
    it('should validate measurement data structure', async () => {
      // Arrange
      const measurements = testFixtures.measurements.validList;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(measurements),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getMeasurements();
      
      // Assert
      expect(result).toBeDefined();
      if (result.length > 0) {
        const measurement = result[0];
        expect(measurement).toHaveProperty('id');
        expect(measurement).toHaveProperty('name');
        expect(measurement).toHaveProperty('timestamp');
        expect(typeof measurement.id).toBe('string');
        expect(typeof measurement.name).toBe('string');
      }
    });
  });

  describe('Measurement Creation - REQ-F-004.2', () => {
    
    // TEST-MEAS-002-01: Successful measurement creation
    it('should add new measurement with valid data', async () => {
      // Arrange
      const newMeasurement = testFixtures.measurements.validNew;
      const createdMeasurement = { ...newMeasurement, id: 'meas-123', timestamp: new Date().toISOString() };
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(createdMeasurement),
        { status: 201, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.addMeasurement(newMeasurement);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:4735/measurements',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify(newMeasurement)
        })
      );
      expect(result).toEqual(createdMeasurement);
      expect(result.id).toBeDefined();
      expect(result.timestamp).toBeDefined();
    });
    
    // TEST-MEAS-002-02: Invalid measurement data validation
    it('should reject invalid measurement data', async () => {
      // Arrange
      const invalidMeasurement = testFixtures.measurements.invalid;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Invalid measurement data' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.addMeasurement(invalidMeasurement)).rejects.toThrow();
    });
    
    // TEST-MEAS-002-03: Duplicate measurement name handling
    it('should handle duplicate measurement names', async () => {
      // Arrange
      const duplicateMeasurement = testFixtures.measurements.duplicate;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement name already exists' }),
        { status: 409, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.addMeasurement(duplicateMeasurement)).rejects.toThrow();
    });
    
    // TEST-MEAS-002-04: Required field validation
    it('should validate required fields', async () => {
      // Arrange
      const incompleteMeasurement = testFixtures.measurements.incomplete;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Missing required fields' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.addMeasurement(incompleteMeasurement)).rejects.toThrow();
    });
  });

  describe('Measurement Deletion - REQ-F-004.3', () => {
    
    // TEST-MEAS-003-01: Successful measurement deletion
    it('should delete measurement by ID', async () => {
      // Arrange
      const measurementId = 'meas-123';
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ success: true, message: 'Measurement deleted' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.deleteMeasurement(measurementId);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}`,
        expect.objectContaining({ method: 'DELETE' })
      );
      expect(result.success).toBe(true);
    });
    
    // TEST-MEAS-003-02: Non-existent measurement deletion
    it('should handle deletion of non-existent measurement', async () => {
      // Arrange
      const nonExistentId = 'non-existent-id';
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement not found' }),
        { status: 404, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.deleteMeasurement(nonExistentId)).rejects.toThrow();
    });
    
    // TEST-MEAS-003-03: Invalid measurement ID format
    it('should validate measurement ID format', async () => {
      // Arrange
      const invalidId = '';
      
      // Act & Assert
      await expect(api.deleteMeasurement(invalidId)).rejects.toThrow();
    });
    
    // TEST-MEAS-003-04: Delete all measurements
    it('should delete all measurements', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ success: true, deletedCount: 5 }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.deleteAllMeasurements();
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:4735/measurements',
        expect.objectContaining({ method: 'DELETE' })
      );
      expect(result.success).toBe(true);
      expect(typeof result.deletedCount).toBe('number');
    });
  });

  describe('Measurement Commands - REQ-F-004.4', () => {
    
    // TEST-MEAS-004-01: Issue measurement start command
    it('should issue measurement start command', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const command = 'start';
      const parameters = { sweepLength: 10, amplitude: -12 };
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ success: true, status: 'started' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.issueMeasurementCommand(measurementId, command, parameters);
      
      // Assert
      expect(mockFetch).toHaveBeenCalledWith(
        `http://localhost:4735/measurements/${measurementId}/command`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          }),
          body: JSON.stringify({ command, parameters })
        })
      );
      expect(result.success).toBe(true);
      expect(result.status).toBe('started');
    });
    
    // TEST-MEAS-004-02: Issue measurement stop command
    it('should issue measurement stop command', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const command = 'stop';
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ success: true, status: 'stopped' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.issueMeasurementCommand(measurementId, command);
      
      // Assert
      expect(result.success).toBe(true);
      expect(result.status).toBe('stopped');
    });
    
    // TEST-MEAS-004-03: Invalid command validation
    it('should validate measurement commands', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const invalidCommand = 'invalid-command';
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Invalid command' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(
        api.issueMeasurementCommand(measurementId, invalidCommand)
      ).rejects.toThrow();
    });
    
    // TEST-MEAS-004-04: Command parameter validation
    it('should validate command parameters', async () => {
      // Arrange
      const measurementId = 'meas-123';
      const command = 'start';
      const invalidParameters = { sweepLength: -1 }; // Invalid negative value
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Invalid parameters: sweepLength must be positive' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(
        api.issueMeasurementCommand(measurementId, command, invalidParameters)
      ).rejects.toThrow();
    });
    
    // TEST-MEAS-004-05: Command on non-existent measurement
    it('should handle commands on non-existent measurements', async () => {
      // Arrange
      const nonExistentId = 'non-existent';
      const command = 'start';
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement not found' }),
        { status: 404, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(
        api.issueMeasurementCommand(nonExistentId, command)
      ).rejects.toThrow();
    });
  });

  describe('Edge Cases and Boundary Conditions', () => {
    
    // TEST-MEAS-005-01: Large measurement dataset handling
    it('should handle large measurement datasets', async () => {
      // Arrange
      const largeMeasurementList = testFixtures.measurements.large;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(largeMeasurementList),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const result = await api.getMeasurements();
      
      // Assert
      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBe(largeMeasurementList.length);
    });
    
    // TEST-MEAS-005-02: Concurrent measurement operations
    it('should handle concurrent measurement operations', async () => {
      // Arrange
      const measurement1 = testFixtures.measurements.validNew;
      const measurement2 = { ...testFixtures.measurements.validNew, name: 'Concurrent Test 2' };
      
      mockFetch
        .mockResolvedValueOnce(new Response(
          JSON.stringify({ ...measurement1, id: 'meas-1' }),
          { status: 201, headers: { 'Content-Type': 'application/json' } }
        ))
        .mockResolvedValueOnce(new Response(
          JSON.stringify({ ...measurement2, id: 'meas-2' }),
          { status: 201, headers: { 'Content-Type': 'application/json' } }
        ));
      
      // Act
      const promises = [
        api.addMeasurement(measurement1),
        api.addMeasurement(measurement2)
      ];
      const results = await Promise.all(promises);
      
      // Assert
      expect(results).toHaveLength(2);
      expect(results[0].id).toBe('meas-1');
      expect(results[1].id).toBe('meas-2');
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });
    
    // TEST-MEAS-005-03: Very long measurement names
    it('should handle boundary conditions for measurement names', async () => {
      // Arrange
      const longName = 'A'.repeat(1000); // Very long name
      const measurementWithLongName = {
        ...testFixtures.measurements.validNew,
        name: longName
      };
      
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify({ error: 'Measurement name too long' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act & Assert
      await expect(api.addMeasurement(measurementWithLongName)).rejects.toThrow();
    });
    
    // TEST-MEAS-005-04: Special characters in measurement names
    it('should handle special characters in measurement names', async () => {
      // Arrange
      const specialCharNames = [
        'Test with spaces',
        'Test-with-hyphens',
        'Test_with_underscores',
        'Test (with parentheses)',
        'Test [with brackets]',
        'Test {with braces}',
        'Test/with/slashes',
        'Test\\with\\backslashes'
      ];
      
      for (const name of specialCharNames) {
        const measurement = { ...testFixtures.measurements.validNew, name };
        mockFetch.mockResolvedValueOnce(new Response(
          JSON.stringify({ ...measurement, id: `meas-${Date.now()}` }),
          { status: 201, headers: { 'Content-Type': 'application/json' } }
        ));
        
        // Act & Assert
        const result = await api.addMeasurement(measurement);
        expect(result.name).toBe(name);
      }
    });
  });

  describe('Performance and Timeout Handling', () => {
    
    // TEST-MEAS-006-01: Request timeout handling
    it('should handle request timeouts', async () => {
      // Arrange
      mockFetch.mockImplementationOnce(() => 
        new Promise((_, reject) => 
          setTimeout(() => reject(new Error('Request timeout')), 100)
        )
      );
      
      // Act & Assert
      await expect(api.getMeasurements()).rejects.toThrow('Request timeout');
    });
    
    // TEST-MEAS-006-02: Performance measurement for large operations
    it('should complete operations within reasonable time limits', async () => {
      // Arrange
      const largeMeasurementList = testFixtures.measurements.large;
      mockFetch.mockResolvedValueOnce(new Response(
        JSON.stringify(largeMeasurementList),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      ));
      
      // Act
      const startTime = Date.now();
      const result = await api.getMeasurements();
      const endTime = Date.now();
      
      // Assert
      expect(endTime - startTime).toBeLessThan(1000); // Should complete within 1 second
      expect(result).toBeDefined();
    });
  });
});