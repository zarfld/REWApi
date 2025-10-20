/**
 * Jest Configuration for REW API Phase 1 Test Suite
 * 
 * @implements TEST-CONFIG-001 - Test environment configuration
 * @traces REQ-F-004, REQ-F-005, REQ-F-006 - Core API functionality testing
 * 
 * Following IEEE 1012-2016 V&V standards for test configuration
 * Phase 1 test configuration covering critical P0 functionality
 */

export default {
  // Test environment
  testEnvironment: 'node',
  
  // Test file patterns
  testMatch: [
    '**/unit/**/*.test.js',
    '**/integration/**/*.test.js'
  ],
  
  // Module file extensions
  moduleFileExtensions: ['js', 'json'],
  
  // Transform configuration for ES modules
  transform: {
    '^.+\\.js$': 'babel-jest'
  },
  
  // Setup files
  setupFilesAfterEnv: ['<rootDir>/setup.js'],
  
  // Coverage configuration
  collectCoverage: true,
  collectCoverageFrom: [
    '../src/**/*.js',
    '!../src/**/*.test.js',
    '!../src/**/fixtures/**',
    '!node_modules/**'
  ],
  
  // Coverage thresholds (Phase 1 target: 40%)
  coverageThreshold: {
    global: {
      branches: 40,
      functions: 40,
      lines: 40,
      statements: 40
    },
    // Specific thresholds for core modules
    '../src/js/rewApi.js': {
      branches: 50,
      functions: 50,
      lines: 50,
      statements: 50
    }
  },
  
  // Coverage reporters
  coverageReporters: [
    'text',
    'lcov',
    'html',
    'json-summary'
  ],
  
  // Coverage directory
  coverageDirectory: 'build/artifacts/coverage',
  
  // Test timeout (30 seconds for integration tests)
  testTimeout: 30000,
  
  // Verbose output for debugging
  verbose: true,
  
  // Clear mocks between tests
  clearMocks: true,
  
  // Restore mocks after each test
  restoreMocks: true,
  
  // Error handling
  errorOnDeprecated: true,
  
  // Module name mapping for test fixtures
  moduleNameMapper: {
    '^@fixtures/(.*)$': '<rootDir>/fixtures/$1',
    '^@src/(.*)$': '<rootDir>/../src/$1',
    '^@tests/(.*)$': '<rootDir>/$1'
  },
  
  // Globals
  globals: {
    'process.env.NODE_ENV': 'test',
    'process.env.REW_API_BASE_URL': 'http://localhost:4735'
  },
  
  // Test results processor
  testResultsProcessor: 'jest-sonar-reporter',
  
  // Reporters for CI/CD integration
  reporters: [
    'default',
    ['jest-junit', {
      outputDirectory: 'build/artifacts/reports',
      outputName: 'test-results.xml',
      suiteName: 'REW API Phase 1 Test Suite',
      classNameTemplate: '{classname}',
      titleTemplate: '{title}',
      ancestorSeparator: ' › ',
      usePathForSuiteName: true
    }]
  ],
  
  // Snapshot serializers
  snapshotSerializers: [],
  
  // Watch plugins
  watchPlugins: [
    'jest-watch-typeahead/filename',
    'jest-watch-typeahead/testname'
  ],
  
  // Force exit after tests complete
  forceExit: true,
  
  // Detect open handles
  detectOpenHandles: true,
  
  // Maximum number of worker processes
  maxWorkers: 4,
  
  // Cache directory
  cacheDirectory: 'build/artifacts/jest-cache'
};