/**
 * Test Setup and Global Configuration
 * 
 * @implements TEST-SETUP-001 - Global test environment setup
 * @traces REQ-F-004, REQ-F-005, REQ-F-006 - Core API functionality testing
 * 
 * Following IEEE 1012-2016 V&V standards for test environment setup
 * Phase 1 test setup for critical P0 functionality
 */

import { jest } from '@jest/globals';

// Global test timeout
jest.setTimeout(30000);

// Mock global fetch if not available
if (!global.fetch) {
  global.fetch = jest.fn();
}

// Mock console methods to reduce noise in tests
global.console = {
  ...console,
  // Uncomment to silence logs during testing
  // log: jest.fn(),
  // debug: jest.fn(),
  // info: jest.fn(),
  warn: jest.fn(),
  error: jest.fn(),
};

// Global test utilities
global.testUtils = {
  // Helper to create mock Response objects
  createMockResponse: (data, status = 200, headers = {}) => {
    return new Response(
      typeof data === 'string' ? data : JSON.stringify(data),
      {
        status,
        headers: {
          'Content-Type': 'application/json',
          ...headers
        }
      }
    );
  },

  // Helper to create mock fetch responses
  mockFetchSuccess: (data) => {
    return Promise.resolve(global.testUtils.createMockResponse(data));
  },

  mockFetchError: (status, error) => {
    return Promise.resolve(global.testUtils.createMockResponse(
      { error },
      status
    ));
  },

  mockFetchReject: (error) => {
    return Promise.reject(new Error(error));
  },

  // Helper to wait for async operations
  waitFor: (ms) => new Promise(resolve => setTimeout(resolve, ms)),

  // Helper to generate test data
  generateTestMeasurement: (overrides = {}) => {
    return {
      id: 'test-meas-' + Date.now(),
      name: 'Test Measurement',
      timestamp: new Date().toISOString(),
      type: 'impulse_response',
      sampleRate: 48000,
      sweepLength: 10,
      channels: ['L', 'R'],
      status: 'completed',
      metadata: {
        room: 'Test Room',
        position: 'Test Position',
        microphone: 'Test Microphone'
      },
      ...overrides
    };
  }
};

// Global test constants
global.testConstants = {
  API_BASE_URL: 'http://localhost:4735',
  DEFAULT_TIMEOUT: 10000,
  SAMPLE_RATES: [44100, 48000, 96000, 192000],
  FREQUENCY_BANDS: ['125', '250', '500', '1000', '2000', '4000'],
  TEST_FREQUENCIES: [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000]
};

// Setup and teardown hooks
beforeEach(() => {
  // Clear all mocks before each test
  jest.clearAllMocks();
  
  // Reset fetch mock
  if (global.fetch && global.fetch.mockReset) {
    global.fetch.mockReset();
  }
});

afterEach(() => {
  // Cleanup after each test
  jest.restoreAllMocks();
});

// Global error handler for unhandled promise rejections
process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason);
});

// Global error handler for uncaught exceptions
process.on('uncaughtException', (error) => {
  console.error('Uncaught Exception:', error);
});

// Mock REW API base configuration
jest.mock('../src/js/rewApi.js', () => {
  return {
    REWApi: jest.fn().mockImplementation(() => ({
      // Mock implementation will be overridden in individual tests
      getMeasurements: jest.fn(),
      addMeasurement: jest.fn(),
      deleteMeasurement: jest.fn(),
      deleteAllMeasurements: jest.fn(),
      issueMeasurementCommand: jest.fn(),
      getFrequencyResponse: jest.fn(),
      getSmoothingFrequencyResponse: jest.fn(),
      getFrequencyResponseStatistics: jest.fn(),
      compareFrequencyResponses: jest.fn(),
      exportFrequencyResponse: jest.fn(),
      applyFrequencyResponseCorrection: jest.fn(),
      findFrequencyResponsePeaks: jest.fn(),
      findFrequencyResponseDips: jest.fn(),
      calculateCurveFitting: jest.fn(),
      generateFrequencyResponseReport: jest.fn(),
      getImpulseResponse: jest.fn(),
      getWindowedImpulseResponse: jest.fn(),
      calculateRT60: jest.fn(),
      calculateEDT: jest.fn(),
      calculateC80: jest.fn(),
      calculateD50: jest.fn(),
      calculateCenterTime: jest.fn(),
      detectEarlyReflections: jest.fn(),
      estimateRoomVolume: jest.fn(),
      calculateIACC: jest.fn(),
      generateImpulseResponseReport: jest.fn(),
      exportImpulseResponse: jest.fn(),
      compareImpulseResponses: jest.fn()
    }))
  };
}, { virtual: true });

// Test environment validation
describe('Test Environment Setup', () => {
  it('should have proper test environment configured', () => {
    expect(process.env.NODE_ENV).toBe('test');
    expect(global.testUtils).toBeDefined();
    expect(global.testConstants).toBeDefined();
    expect(global.fetch).toBeDefined();
  });

  it('should have test utilities available', () => {
    expect(typeof global.testUtils.createMockResponse).toBe('function');
    expect(typeof global.testUtils.mockFetchSuccess).toBe('function');
    expect(typeof global.testUtils.mockFetchError).toBe('function');
    expect(typeof global.testUtils.waitFor).toBe('function');
    expect(typeof global.testUtils.generateTestMeasurement).toBe('function');
  });

  it('should have test constants defined', () => {
    expect(global.testConstants.API_BASE_URL).toBe('http://localhost:4735');
    expect(Array.isArray(global.testConstants.SAMPLE_RATES)).toBe(true);
    expect(Array.isArray(global.testConstants.FREQUENCY_BANDS)).toBe(true);
    expect(global.testConstants.SAMPLE_RATES.length).toBeGreaterThan(0);
  });
});