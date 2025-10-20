// Jest setup for TypeScript tests
import 'cross-fetch/polyfill';

// Set longer timeout for HTTP requests
// jest.setTimeout(30000); // Will be set in individual test files

// Global test configuration
global.console = {
  ...console,
  // Uncomment to suppress console logs during tests
  // log: jest.fn(),
  // debug: jest.fn(),
  // info: jest.fn(),
  // warn: jest.fn(),
  // error: jest.fn(),
};

// Test environment configuration
const TEST_CONFIG = {
  REW_HOST: process.env.REW_HOST || 'localhost',
  REW_PORT: process.env.REW_PORT || '4735',
  REW_TIMEOUT: parseInt(process.env.REW_TIMEOUT || '30000'),
  SKIP_REAL_API_TESTS: process.env.SKIP_REAL_API_TESTS === 'true'
};

// Make config available globally
(global as any).TEST_CONFIG = TEST_CONFIG;

export {};