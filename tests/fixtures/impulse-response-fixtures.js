/**
 * Test Fixtures for Impulse Response Operations Testing
 * 
 * @implements TEST-FIX-003 - Impulse Response test data fixtures
 * @traces REQ-F-006 - Impulse Response Analysis
 * 
 * Following IEEE 1012-2016 V&V standards for test data management
 * Phase 1 test fixtures covering critical impulse response scenarios
 */

export const testFixtures = {
  impulseResponse: {
    // Valid impulse response data
    validData: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      length: 24000, // 0.5 seconds at 48kHz
      duration: 0.5,
      peakTime: 0.0021, // 2.1ms peak time
      peakAmplitude: 0.85,
      snr: 65.3, // Signal-to-noise ratio in dB
      dynamicRange: 120.5,
      timeData: (() => {
        // Generate realistic impulse response
        const data = [];
        const sampleRate = 48000;
        const length = 24000;
        
        for (let i = 0; i < length; i++) {
          const time = i / sampleRate;
          let amplitude = 0;
          
          // Direct sound peak at 2.1ms
          if (i === Math.floor(0.0021 * sampleRate)) {
            amplitude = 0.85;
          }
          
          // Early reflections
          const reflectionTimes = [0.005, 0.012, 0.018, 0.024, 0.035];
          const reflectionAmplitudes = [0.12, 0.08, 0.15, 0.06, 0.09];
          
          reflectionTimes.forEach((refTime, idx) => {
            if (Math.abs(time - refTime) < 0.0001) {
              amplitude += reflectionAmplitudes[idx];
            }
          });
          
          // Exponential decay with some irregularity
          if (time > 0.0021) {
            const baseDecay = Math.exp(-time * 8); // RT60 ≈ 0.75s
            const irregularity = 1 + 0.1 * Math.sin(time * 500) * Math.exp(-time * 2);
            amplitude += baseDecay * irregularity * 0.1 * (Math.random() - 0.5);
          }
          
          data.push(parseFloat(amplitude.toFixed(6)));
        }
        return data;
      })(),
      metadata: {
        sweepType: 'log',
        sweepLength: 10,
        startFreq: 20,
        endFreq: 20000,
        windowFunction: 'rectangular',
        calibrated: true
      }
    },

    // Windowed impulse response
    windowed: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      windowApplied: true,
      windowType: 'hanning',
      windowLength: 1024,
      windowStartTime: 0.0,
      sampleRate: 48000,
      length: 1024,
      timeData: (() => {
        // Generate windowed data
        const data = [];
        const windowLength = 1024;
        
        for (let i = 0; i < windowLength; i++) {
          // Hanning window
          const window = 0.5 * (1 - Math.cos(2 * Math.PI * i / (windowLength - 1)));
          
          // Apply window to impulse response
          let amplitude = 0;
          if (i === 100) amplitude = 0.85; // Peak
          if (i > 100) amplitude = 0.1 * Math.exp(-(i - 100) / 200);
          
          data.push(parseFloat((amplitude * window).toFixed(6)));
        }
        return data;
      })(),
      originalLength: 24000
    },

    // RT60 analysis results
    rt60: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      frequencyBands: ['125', '250', '500', '1000', '2000', '4000'],
      rt60Values: [0.85, 0.78, 0.72, 0.68, 0.61, 0.55], // seconds
      averageRT60: 0.70,
      t20Values: [0.82, 0.76, 0.70, 0.66, 0.59, 0.53],
      t30Values: [0.84, 0.77, 0.71, 0.67, 0.60, 0.54],
      linearityR2: [0.95, 0.97, 0.98, 0.96, 0.94, 0.92], // R² values for decay linearity
      analysis: {
        room_type: 'medium_room',
        acoustic_rating: 'good',
        optimal_rt60_range: [0.4, 0.8],
        recommendations: [
          'RT60 values are within optimal range for medium rooms',
          'Consider slight absorption at 125Hz to reduce bass reverberation',
          'Excellent consistency across mid-frequencies'
        ]
      }
    },

    // EDT (Early Decay Time) analysis
    edt: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      frequencyBands: ['125', '250', '500', '1000', '2000', '4000'],
      edtValues: [0.72, 0.65, 0.58, 0.55, 0.48, 0.42], // seconds
      averageEDT: 0.57,
      edtToRt60Ratio: [0.85, 0.83, 0.81, 0.81, 0.79, 0.76],
      analysis: {
        subjective_reverberation: 'well_balanced',
        early_reflection_control: 'good',
        recommendations: [
          'EDT/RT60 ratios indicate good diffusion',
          'Early reflections are well controlled',
          'Room provides good subjective impression of reverberation'
        ]
      }
    },

    // C80 (Clarity) analysis
    c80: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      frequencyBands: ['125', '250', '500', '1000', '2000', '4000'],
      c80Values: [1.2, 2.8, 4.5, 5.2, 6.8, 7.1], // dB
      averageC80: 4.6,
      speechIntelligibility: 'excellent',
      musicClarity: 'very_good',
      analysis: {
        intelligibility_rating: 'excellent',
        music_performance_rating: 'very_good',
        recommendations: [
          'C80 values excellent for speech (>2dB achieved)',
          'Music clarity very good across all frequencies',
          'No acoustic treatment needed for clarity'
        ]
      }
    },

    // D50 (Definition) analysis
    d50: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      frequencyBands: ['125', '250', '500', '1000', '2000', '4000'],
      d50Values: [0.52, 0.64, 0.74, 0.78, 0.83, 0.85], // ratios (0-1)
      averageD50: 0.73,
      speechIntelligibility: 'very_good',
      definition_rating: 'excellent',
      analysis: {
        speech_definition: 'very_good',
        overall_clarity: 'excellent',
        recommendations: [
          'D50 values excellent for speech (>0.5 achieved)',
          'Definition increases appropriately with frequency',
          'Room provides excellent speech intelligibility'
        ]
      }
    },

    // Center Time (Ts) analysis
    ts: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      frequencyBands: ['125', '250', '500', '1000', '2000', '4000'],
      tsValues: [125, 98, 78, 68, 52, 45], // milliseconds
      averageTs: 77.7,
      reverberance: 'moderate',
      warmth_index: 1.60, // Ts(125+250)/Ts(500+1000+2000+4000)
      analysis: {
        reverberance_rating: 'moderate',
        warmth_rating: 'warm',
        intimacy_rating: 'good',
        recommendations: [
          'Center times indicate moderate reverberance',
          'Warmth index suggests pleasant warm character',
          'Good intimacy for medium-sized room'
        ]
      }
    },

    // Early reflections analysis
    earlyReflections: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      analysisParameters: {
        threshold: -20, // dB below peak
        timeWindow: 80, // ms
        minReflectionGap: 2 // ms
      },
      reflections: [
        {
          time: 5.2, // ms
          amplitude: -12.3, // dB relative to peak
          delay: 3.1, // ms from direct sound
          source: 'ceiling',
          strength: 'medium',
          audibility: 'noticeable'
        },
        {
          time: 12.8,
          amplitude: -18.7,
          delay: 10.7,
          source: 'side_wall_left',
          strength: 'weak',
          audibility: 'barely_noticeable'
        },
        {
          time: 18.5,
          amplitude: -14.2,
          delay: 16.4,
          source: 'rear_wall',
          strength: 'medium',
          audibility: 'noticeable'
        },
        {
          time: 24.1,
          amplitude: -21.5,
          delay: 22.0,
          source: 'side_wall_right',
          strength: 'weak',
          audibility: 'barely_noticeable'
        },
        {
          time: 35.7,
          amplitude: -16.8,
          delay: 33.6,
          source: 'floor',
          strength: 'medium',
          audibility: 'noticeable'
        }
      ],
      summary: {
        totalReflections: 5,
        strongReflections: 0,
        mediumReflections: 3,
        weakReflections: 2,
        lateralEnergyFraction: 0.18,
        reflectionDensity: 0.062 // reflections per ms
      },
      recommendations: [
        'Consider diffusion at ceiling first reflection point',
        'Side wall reflections are well controlled',
        'Rear wall reflection timing is beneficial for spaciousness',
        'Overall early reflection pattern is good for listening room'
      ]
    },

    // Room volume estimation
    roomVolume: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      estimatedVolume: 145.7, // cubic meters
      confidence: 0.82, // 0-1 scale
      method: 'sabine_formula',
      calculations: {
        rt60Average: 0.70,
        estimatedAbsorption: 0.18,
        surfaceAreaEstimate: 285.4
      },
      alternativeMethods: {
        eyring_formula: {
          volume: 138.2,
          confidence: 0.79
        },
        geometric_estimation: {
          volume: 152.3,
          confidence: 0.75
        }
      },
      uncertainty: {
        lowerBound: 128.5,
        upperBound: 162.9,
        standardError: 8.6
      }
    },

    // IACC (Inter-Aural Cross Correlation) for stereo measurements
    iacc: {
      measurementId: 'meas-stereo-123',
      timestamp: '2024-01-15T10:30:00Z',
      iaccEarly: 0.25, // 0-80ms
      iaccLate: 0.08,  // 80ms-end
      iaccE3: 0.31,    // 0-80ms with 1/3 octave smoothing
      frequencyBands: {
        '125': { iaccEarly: 0.35, iaccLate: 0.12 },
        '250': { iaccEarly: 0.28, iaccLate: 0.09 },
        '500': { iaccEarly: 0.22, iaccLate: 0.07 },
        '1000': { iaccEarly: 0.18, iaccLate: 0.06 },
        '2000': { iaccEarly: 0.15, iaccLate: 0.05 },
        '4000': { iaccEarly: 0.12, iaccLate: 0.04 }
      },
      spatialImpression: {
        apparentSourceWidth: 'wide',
        listenerEnvelopment: 'good',
        spaciousness: 'excellent'
      },
      analysis: {
        spatial_quality: 'excellent',
        stereo_separation: 'very_good',
        recommendations: [
          'IACC values indicate excellent spatial impression',
          'Good stereo separation across all frequencies',
          'Room provides natural spaciousness without excessive correlation'
        ]
      }
    },

    // Comprehensive impulse response report
    report: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T13:00:00Z',
      summary: {
        overallScore: 8.2, // 0-10 scale
        acousticRating: 'very_good', // excellent, very_good, good, fair, poor
        roomType: 'listening_room',
        roomSize: 'medium',
        acousticCharacter: 'neutral_warm'
      },
      rt60: {
        average: 0.70,
        rating: 'optimal',
        consistency: 'good'
      },
      edt: {
        average: 0.57,
        rating: 'excellent',
        balance: 'well_balanced'
      },
      clarity: {
        c80Average: 4.6,
        d50Average: 0.73,
        speechIntelligibility: 'excellent',
        musicClarity: 'very_good'
      },
      earlyReflections: {
        count: 5,
        control: 'good',
        lateralEnergy: 0.18,
        spatialImpression: 'excellent'
      },
      recommendations: [
        'Room acoustics are very good for listening applications',
        'Consider minor bass absorption to optimize low frequency response',
        'Early reflection pattern supports good spatial impression',
        'No urgent acoustic treatment required',
        'Room is well-suited for both music and speech'
      ],
      detailedAnalysis: {
        strengths: [
          'Excellent clarity and definition',
          'Well-controlled early reflections',
          'Good frequency consistency',
          'Appropriate reverberation time'
        ],
        areas_for_improvement: [
          'Slight bass reverberation excess',
          'Minor ceiling reflection timing'
        ],
        treatment_suggestions: [
          'Optional: Bass trap in corner to reduce 125Hz RT60',
          'Optional: Ceiling diffusion panel above listening position'
        ]
      }
    },

    // Export data formats
    exportData: {
      wav: new ArrayBuffer(48000 * 2 * 0.5), // 0.5 seconds of stereo 16-bit WAV data
      
      csv: `Time,Amplitude
0.000000,0.000000
0.000021,0.000000
0.000042,0.000000
0.002083,0.850000
0.002104,0.420000
0.002125,0.180000
0.005208,0.120000
0.012500,0.080000
0.018750,0.150000
0.024000,0.060000
0.035700,0.090000
0.100000,0.025000
0.200000,0.012000
0.300000,0.008000
0.400000,0.005000
0.500000,0.003000`,
      
      json: {
        metadata: {
          measurementId: 'meas-123',
          exportDate: '2024-01-15T12:00:00Z',
          format: 'json',
          sampleRate: 48000,
          length: 24000,
          duration: 0.5
        },
        data: {
          timeAxis: [0.0, 0.000021, 0.000042, 0.002083, 0.002104], // truncated for example
          amplitude: [0.0, 0.0, 0.0, 0.85, 0.42] // truncated for example
        }
      }
    },

    // Impulse response comparison
    comparison: {
      measurement1: 'meas-123',
      measurement2: 'meas-456',
      timestamp: '2024-01-15T14:00:00Z',
      rt60Comparison: {
        measurement1Avg: 0.70,
        measurement2Avg: 0.85,
        difference: 0.15,
        significantDifference: true,
        bandComparison: {
          '125': { diff: 0.12, significant: true },
          '250': { diff: 0.08, significant: false },
          '500': { diff: 0.18, significant: true },
          '1000': { diff: 0.15, significant: true },
          '2000': { diff: 0.22, significant: true },
          '4000': { diff: 0.18, significant: true }
        }
      },
      clarityComparison: {
        c80Difference: 1.8, // dB
        d50Difference: 0.12,
        speechIntelligibilityDiff: 'measurement1_better',
        clarityRating: 'measurement1_superior'
      },
      reflectionComparison: {
        measurement1Reflections: 5,
        measurement2Reflections: 8,
        lateralEnergyDiff: -0.05,
        reflectionControlRating: 'measurement1_better'
      },
      overallSimilarity: 0.73, // 0-1 scale
      primaryDifferences: [
        'Measurement 2 has longer reverberation time',
        'Measurement 1 has better clarity and definition',
        'Measurement 2 has more early reflections',
        'Both measurements have similar spatial characteristics'
      ],
      recommendations: [
        'Measurement 1 is better suited for critical listening',
        'Measurement 2 might benefit from acoustic treatment',
        'Consider the differences in room usage requirements'
      ]
    },

    // Long duration impulse response for performance testing
    longDuration: {
      measurementId: 'meas-long',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      length: 480000, // 10 seconds
      duration: 10.0,
      peakTime: 0.0021,
      peakAmplitude: 0.85,
      timeData: (() => {
        // Generate long impulse response with realistic decay
        const data = [];
        const sampleRate = 48000;
        const length = 480000;
        
        for (let i = 0; i < length; i++) {
          const time = i / sampleRate;
          let amplitude = 0;
          
          // Direct sound
          if (i === Math.floor(0.0021 * sampleRate)) {
            amplitude = 0.85;
          }
          
          // Long reverberation tail
          if (time > 0.0021) {
            amplitude = 0.1 * Math.exp(-time * 0.5) * (Math.random() - 0.5);
          }
          
          data.push(parseFloat(amplitude.toFixed(6)));
        }
        return data;
      })()
    },

    // Short duration impulse response
    shortDuration: {
      measurementId: 'meas-short',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      length: 4800, // 0.1 seconds
      duration: 0.1,
      peakTime: 0.0021,
      peakAmplitude: 0.85,
      timeData: (() => {
        const data = [];
        const length = 4800;
        
        for (let i = 0; i < length; i++) {
          const time = i / 48000;
          let amplitude = 0;
          
          if (i === 100) amplitude = 0.85; // Peak
          if (i > 100) amplitude = 0.1 * Math.exp(-(i - 100) / 500);
          
          data.push(parseFloat(amplitude.toFixed(6)));
        }
        return data;
      })()
    },

    // Low level impulse response
    lowLevel: {
      measurementId: 'meas-quiet',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      length: 24000,
      duration: 0.5,
      peakTime: 0.0021,
      peakAmplitude: 0.0008, // Very low signal level
      snr: 25.3, // Poor signal-to-noise ratio
      timeData: (() => {
        const data = [];
        const length = 24000;
        
        for (let i = 0; i < length; i++) {
          let amplitude = 0;
          
          // Very quiet signal
          if (i === 100) amplitude = 0.0008;
          if (i > 100) amplitude = 0.0001 * Math.exp(-(i - 100) / 200);
          
          // Add noise
          amplitude += (Math.random() - 0.5) * 0.0002;
          
          data.push(parseFloat(amplitude.toFixed(6)));
        }
        return data;
      })()
    },

    // Corrupted data for error testing
    corrupted: {
      measurementId: 'meas-corrupted',
      timeData: [1, 2, null, 'invalid', undefined], // Invalid data types
      sampleRate: 'not_a_number', // Invalid sample rate
      length: -1000, // Invalid length
      peakTime: 'invalid_time',
      peakAmplitude: null
    }
  }
};