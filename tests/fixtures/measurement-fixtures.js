/**
 * Test Fixtures for Measurement Operations Testing
 * 
 * @implements TEST-FIX-001 - Measurement test data fixtures
 * @traces REQ-F-004 - Measurement Operations
 * 
 * Following IEEE 1012-2016 V&V standards for test data management
 * Phase 1 test fixtures covering critical measurement scenarios
 */

export const testFixtures = {
  measurements: {
    // Valid measurement list for testing retrieval
    validList: [
      {
        id: 'meas-001',
        name: 'Living Room Measurement',
        timestamp: '2024-01-15T10:30:00Z',
        type: 'impulse_response',
        sampleRate: 48000,
        sweepLength: 10,
        channels: ['L', 'R'],
        status: 'completed',
        metadata: {
          room: 'Living Room',
          position: 'Center',
          microphone: 'Calibrated USB Mic'
        }
      },
      {
        id: 'meas-002',
        name: 'Kitchen SPL Test',
        timestamp: '2024-01-15T11:15:00Z',
        type: 'spl_meter',
        sampleRate: 48000,
        duration: 60,
        channels: ['Mono'],
        status: 'completed',
        metadata: {
          room: 'Kitchen',
          position: 'Island',
          microphone: 'Built-in Laptop Mic'
        }
      },
      {
        id: 'meas-003',
        name: 'Bedroom RTA Analysis',
        timestamp: '2024-01-15T14:20:00Z',
        type: 'rta',
        sampleRate: 48000,
        duration: 120,
        channels: ['L'],
        status: 'in_progress',
        metadata: {
          room: 'Bedroom',
          position: 'Listening Position',
          microphone: 'External USB Mic'
        }
      }
    ],

    // Valid new measurement for creation testing
    validNew: {
      name: 'Test Measurement',
      type: 'impulse_response',
      sampleRate: 48000,
      sweepLength: 10,
      channels: ['L', 'R'],
      metadata: {
        room: 'Test Room',
        position: 'Test Position',
        microphone: 'Test Microphone'
      }
    },

    // Invalid measurement data for validation testing
    invalid: {
      name: '', // Empty name
      type: 'unknown_type', // Invalid type
      sampleRate: -1, // Invalid sample rate
      sweepLength: 0, // Invalid sweep length
      channels: [], // Empty channels array
      metadata: null // Invalid metadata
    },

    // Duplicate measurement for conflict testing
    duplicate: {
      name: 'Living Room Measurement', // Same as existing
      type: 'impulse_response',
      sampleRate: 48000,
      sweepLength: 10,
      channels: ['L', 'R'],
      metadata: {
        room: 'Living Room',
        position: 'Center',
        microphone: 'Calibrated USB Mic'
      }
    },

    // Incomplete measurement for required field testing
    incomplete: {
      name: 'Incomplete Test',
      // Missing required fields: type, sampleRate, channels
      metadata: {
        room: 'Test Room'
      }
    },

    // Large dataset for performance testing
    large: (() => {
      const measurements = [];
      for (let i = 1; i <= 1000; i++) {
        measurements.push({
          id: `meas-${String(i).padStart(4, '0')}`,
          name: `Measurement ${i}`,
          timestamp: new Date(Date.now() - i * 60000).toISOString(),
          type: i % 3 === 0 ? 'impulse_response' : i % 3 === 1 ? 'spl_meter' : 'rta',
          sampleRate: 48000,
          sweepLength: 10,
          channels: i % 2 === 0 ? ['L', 'R'] : ['Mono'],
          status: i % 4 === 0 ? 'completed' : i % 4 === 1 ? 'in_progress' : i % 4 === 2 ? 'failed' : 'pending',
          metadata: {
            room: `Room ${Math.ceil(i / 10)}`,
            position: `Position ${i % 5 + 1}`,
            microphone: `Microphone ${i % 3 + 1}`
          }
        });
      }
      return measurements;
    })(),

    // Edge case measurements
    edgeCases: {
      // Minimum valid measurement
      minimal: {
        name: 'A',
        type: 'spl_meter',
        sampleRate: 8000,
        duration: 1,
        channels: ['Mono']
      },

      // Maximum complexity measurement
      maximal: {
        name: 'Very Complex Multi-Channel High-Resolution Long-Duration Measurement with Extensive Metadata',
        type: 'impulse_response',
        sampleRate: 192000,
        sweepLength: 60,
        channels: ['L', 'R', 'C', 'LFE', 'LS', 'RS', 'LB', 'RB'],
        metadata: {
          room: 'Professional Studio Room A with Advanced Acoustic Treatment',
          position: 'Primary Engineer Listening Position with Precision Alignment',
          microphone: 'Calibrated Professional Measurement Microphone Model XYZ-2024',
          equipment: {
            interface: 'High-End Audio Interface',
            preamp: 'Transparent Microphone Preamplifier',
            cables: 'Low-Noise Balanced XLR Cables'
          },
          environment: {
            temperature: 22.5,
            humidity: 45,
            pressure: 1013.25
          },
          settings: {
            windowFunction: 'Hanning',
            fftSize: 65536,
            overlap: 50,
            averaging: 'exponential'
          }
        }
      },

      // Special characters in names
      specialChars: [
        { name: 'Test with spaces', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test-with-hyphens', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test_with_underscores', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test (with parentheses)', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test [with brackets]', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test {with braces}', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test/with/slashes', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] },
        { name: 'Test\\with\\backslashes', type: 'spl_meter', sampleRate: 48000, duration: 10, channels: ['Mono'] }
      ]
    }
  },

  // Command test data
  commands: {
    valid: {
      start: {
        command: 'start',
        parameters: {
          sweepLength: 10,
          amplitude: -12,
          sweepType: 'log',
          startFreq: 20,
          endFreq: 20000
        }
      },
      stop: {
        command: 'stop',
        parameters: {}
      },
      pause: {
        command: 'pause',
        parameters: {}
      },
      resume: {
        command: 'resume',
        parameters: {}
      },
      reset: {
        command: 'reset',
        parameters: {}
      }
    },

    invalid: {
      unknownCommand: {
        command: 'unknown_command',
        parameters: {}
      },
      invalidParameters: {
        command: 'start',
        parameters: {
          sweepLength: -1, // Invalid negative value
          amplitude: 10, // Too high amplitude
          startFreq: -100, // Invalid negative frequency
          endFreq: 1000000 // Unrealistic high frequency
        }
      },
      missingParameters: {
        command: 'start',
        parameters: {} // Missing required parameters
      }
    }
  },

  // Response templates
  responses: {
    success: {
      measurementAdded: {
        success: true,
        message: 'Measurement added successfully',
        data: {
          id: 'meas-new-123',
          timestamp: '2024-01-15T15:30:00Z'
        }
      },
      measurementDeleted: {
        success: true,
        message: 'Measurement deleted successfully'
      },
      allMeasurementsDeleted: {
        success: true,
        message: 'All measurements deleted successfully',
        deletedCount: 5
      },
      commandExecuted: {
        success: true,
        message: 'Command executed successfully',
        status: 'started'
      }
    },

    errors: {
      invalidData: {
        error: 'Invalid measurement data',
        details: 'Required fields are missing or invalid'
      },
      duplicateName: {
        error: 'Measurement name already exists',
        details: 'Please choose a different name'
      },
      notFound: {
        error: 'Measurement not found',
        details: 'The specified measurement ID does not exist'
      },
      invalidCommand: {
        error: 'Invalid command',
        details: 'The specified command is not supported'
      },
      invalidParameters: {
        error: 'Invalid command parameters',
        details: 'One or more parameters are invalid'
      },
      serverError: {
        error: 'Internal server error',
        details: 'An unexpected error occurred on the server'
      },
      networkError: {
        error: 'Network error',
        details: 'Unable to connect to the REW API server'
      }
    }
  },

  // HTTP response configurations
  http: {
    headers: {
      json: {
        'Content-Type': 'application/json'
      },
      cors: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization'
      }
    },

    statusCodes: {
      success: {
        ok: 200,
        created: 201,
        accepted: 202,
        noContent: 204
      },
      clientError: {
        badRequest: 400,
        unauthorized: 401,
        forbidden: 403,
        notFound: 404,
        conflict: 409,
        unprocessableEntity: 422
      },
      serverError: {
        internalServerError: 500,
        badGateway: 502,
        serviceUnavailable: 503,
        gatewayTimeout: 504
      }
    }
  },

  // Performance benchmarks
  performance: {
    timeouts: {
      fast: 100,      // Operations that should complete quickly
      normal: 1000,   // Normal operation timeout
      slow: 5000,     // Slow operations (large datasets)
      timeout: 30000  // Maximum timeout before failure
    },

    limits: {
      maxMeasurements: 10000,      // Maximum measurements in list
      maxNameLength: 255,          // Maximum measurement name length
      maxMetadataSize: 10240,      // Maximum metadata size in bytes
      maxConcurrentOps: 10,        // Maximum concurrent operations
      maxResponseSize: 52428800    // Maximum response size (50MB)
    }
  }
};