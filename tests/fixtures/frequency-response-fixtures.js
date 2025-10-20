/**
 * Test Fixtures for Frequency Response Operations Testing
 * 
 * @implements TEST-FIX-002 - Frequency Response test data fixtures
 * @traces REQ-F-005 - Frequency Response Analysis
 * 
 * Following IEEE 1012-2016 V&V standards for test data management
 * Phase 1 test fixtures covering critical frequency response scenarios
 */

export const testFixtures = {
  frequencyResponse: {
    // Valid frequency response data
    validData: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      fftSize: 8192,
      windowFunction: 'Hanning',
      frequencies: (() => {
        // Generate logarithmic frequency array from 20Hz to 20kHz
        const freqs = [];
        for (let i = 0; i < 4096; i++) {
          const freq = 20 * Math.pow(1000, i / 4095); // 20Hz to 20kHz log scale
          freqs.push(parseFloat(freq.toFixed(2)));
        }
        return freqs;
      })(),
      magnitude: (() => {
        // Generate realistic magnitude response (house curve with room modes)
        const mags = [];
        for (let i = 0; i < 4096; i++) {
          const freq = 20 * Math.pow(1000, i / 4095);
          let mag = 0;
          
          // Bass boost below 100Hz
          if (freq < 100) {
            mag += 3 * (1 - freq / 100);
          }
          
          // Room modes (peaks and dips)
          mag += 5 * Math.sin(freq / 50) * Math.exp(-freq / 200);
          
          // High frequency rolloff above 10kHz
          if (freq > 10000) {
            mag -= 2 * Math.log10(freq / 10000);
          }
          
          // Add some random variation
          mag += (Math.random() - 0.5) * 2;
          
          mags.push(parseFloat(mag.toFixed(2)));
        }
        return mags;
      })(),
      phase: (() => {
        // Generate realistic phase response
        const phases = [];
        for (let i = 0; i < 4096; i++) {
          const freq = 20 * Math.pow(1000, i / 4095);
          
          // Phase typically decreases with frequency
          let phase = -360 * Math.log10(freq / 20) / Math.log10(1000);
          
          // Add phase variations from reflections
          phase += 30 * Math.sin(freq / 100) * Math.exp(-freq / 500);
          
          // Wrap phase to -180 to +180 range
          while (phase > 180) phase -= 360;
          while (phase < -180) phase += 360;
          
          phases.push(parseFloat(phase.toFixed(2)));
        }
        return phases;
      })(),
      coherence: (() => {
        // Generate coherence data (0 to 1)
        const coherence = [];
        for (let i = 0; i < 4096; i++) {
          const freq = 20 * Math.pow(1000, i / 4095);
          
          // High coherence in mid frequencies, lower at extremes
          let coh = 0.9;
          if (freq < 50) coh *= freq / 50;
          if (freq > 15000) coh *= (20000 - freq) / 5000;
          
          // Add some variation
          coh += (Math.random() - 0.5) * 0.1;
          coh = Math.max(0, Math.min(1, coh));
          
          coherence.push(parseFloat(coh.toFixed(3)));
        }
        return coherence;
      })()
    },

    // Frequency range limited data
    ranged: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 48000,
      fftSize: 8192,
      startFreq: 20,
      endFreq: 20000,
      frequencies: (() => {
        const freqs = [];
        for (let i = 0; i < 1024; i++) {
          const freq = 20 + (20000 - 20) * (i / 1023);
          freqs.push(parseFloat(freq.toFixed(2)));
        }
        return freqs;
      })(),
      magnitude: (() => {
        const mags = [];
        for (let i = 0; i < 1024; i++) {
          const mag = Math.sin(i * 0.01) * 10 + (Math.random() - 0.5) * 3;
          mags.push(parseFloat(mag.toFixed(2)));
        }
        return mags;
      })(),
      phase: (() => {
        const phases = [];
        for (let i = 0; i < 1024; i++) {
          const phase = -180 + (360 * i / 1023) + (Math.random() - 0.5) * 20;
          phases.push(parseFloat(phase.toFixed(2)));
        }
        return phases;
      })()
    },

    // Smoothed frequency response
    smoothed: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T10:30:00Z',
      smoothingApplied: true,
      smoothingFactor: 6, // 1/6 octave
      originalData: 'reference-to-original',
      frequencies: [20, 25, 31.5, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000],
      magnitude: [-2.1, -1.8, -1.2, -0.8, -0.3, 0.2, 0.8, 1.2, 1.0, 0.5, -0.2, -0.8, -1.5, -2.0, -1.8, -1.2, -0.5, 0.0, 0.3, 0.1, -0.3, -0.8, -1.5, -2.2, -3.1, -4.2, -5.5, -7.0, -8.8, -10.5, -12.2],
      phase: [0, -5, -12, -18, -25, -32, -38, -45, -52, -58, -65, -72, -78, -85, -92, -98, -105, -112, -118, -125, -132, -138, -145, -152, -158, -165, -172, -178, 175, 168, 162]
    },

    // Frequency response statistics
    statistics: {
      measurementId: 'meas-123',
      frequencyRange: { start: 20, end: 20000 },
      averageLevel: -2.3,
      peakLevel: 8.7,
      peakFrequency: 245,
      valleyLevel: -12.4,
      valleyFrequency: 3200,
      dynamicRange: 21.1,
      flatnessScore: 0.73, // 0-1 scale, 1 = perfectly flat
      standardDeviation: 4.2,
      variance: 17.64,
      rms: 4.85,
      thd: 0.023, // Total Harmonic Distortion
      snr: 85.2, // Signal to Noise Ratio in dB
      bandwidthInfo: {
        halfPowerBandwidth: { low: 35, high: 18500 },
        quarterPowerBandwidth: { low: 28, high: 19200 }
      },
      frequencyBands: {
        bass: { range: [20, 250], averageLevel: 2.1, peakLevel: 8.7 },
        midrange: { range: [250, 4000], averageLevel: -1.2, peakLevel: 3.2 },
        treble: { range: [4000, 20000], averageLevel: -4.8, peakLevel: -0.5 }
      }
    },

    // Comparison between two frequency responses
    comparison: {
      measurement1: 'meas-123',
      measurement2: 'meas-456',
      timestamp: '2024-01-15T11:00:00Z',
      difference: {
        frequencies: [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000],
        magnitude: [1.2, 0.8, -0.5, -1.2, 0.3, 0.8, -0.2, -1.8, -2.5, -3.2],
        phase: [5, 8, -3, -12, 2, 15, -8, -25, 18, 35]
      },
      correlation: 0.87, // Pearson correlation coefficient
      similarities: {
        overallSimilarity: 0.82, // 0-1 scale
        bassResponse: 0.79,
        midrangeResponse: 0.91,
        trebleResponse: 0.76,
        phaseCorrelation: 0.68
      },
      analysis: {
        significantDifferences: [
          { frequency: 245, difference: 8.2, type: 'peak' },
          { frequency: 3200, difference: -6.8, type: 'dip' },
          { frequency: 12000, difference: 4.1, type: 'peak' }
        ],
        recommendations: [
          'Measurement 1 has stronger bass response below 100Hz',
          'Measurement 2 shows better linearity in midrange',
          'Both measurements show similar treble rolloff'
        ]
      }
    },

    // Export data formats
    exportData: {
      csv: `Frequency,Magnitude,Phase
20.00,-2.10,0.00
25.00,-1.80,-5.00
31.50,-1.20,-12.00
40.00,-0.80,-18.00
50.00,-0.30,-25.00
63.00,0.20,-32.00
80.00,0.80,-38.00
100.00,1.20,-45.00
125.00,1.00,-52.00
160.00,0.50,-58.00
200.00,-0.20,-65.00
250.00,-0.80,-72.00
315.00,-1.50,-78.00
400.00,-2.00,-85.00
500.00,-1.80,-92.00
630.00,-1.20,-98.00
800.00,-0.50,-105.00
1000.00,0.00,-112.00
1250.00,0.30,-118.00
1600.00,0.10,-125.00
2000.00,-0.30,-132.00
2500.00,-0.80,-138.00
3150.00,-1.50,-145.00
4000.00,-2.20,-152.00
5000.00,-3.10,-158.00
6300.00,-4.20,-165.00
8000.00,-5.50,-172.00
10000.00,-7.00,-178.00
12500.00,-8.80,175.00
16000.00,-10.50,168.00
20000.00,-12.20,162.00`,
      
      json: {
        metadata: {
          measurementId: 'meas-123',
          exportDate: '2024-01-15T12:00:00Z',
          format: 'json',
          sampleRate: 48000,
          fftSize: 8192
        },
        data: {
          frequencies: [20, 25, 31.5, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3150, 4000, 5000, 6300, 8000, 10000, 12500, 16000, 20000],
          magnitude: [-2.1, -1.8, -1.2, -0.8, -0.3, 0.2, 0.8, 1.2, 1.0, 0.5, -0.2, -0.8, -1.5, -2.0, -1.8, -1.2, -0.5, 0.0, 0.3, 0.1, -0.3, -0.8, -1.5, -2.2, -3.1, -4.2, -5.5, -7.0, -8.8, -10.5, -12.2],
          phase: [0, -5, -12, -18, -25, -32, -38, -45, -52, -58, -65, -72, -78, -85, -92, -98, -105, -112, -118, -125, -132, -138, -145, -152, -158, -165, -172, -178, 175, 168, 162]
        }
      }
    },

    // Correction curve data
    correctionCurve: {
      name: 'Room Correction Curve A',
      type: 'parametric_eq',
      filters: [
        { frequency: 245, gain: -8.2, q: 2.5, type: 'peaking' },
        { frequency: 3200, gain: 6.8, q: 1.8, type: 'peaking' },
        { frequency: 80, gain: -2.1, q: 0.7, type: 'high_shelf' },
        { frequency: 12000, gain: -3.5, q: 0.9, type: 'low_shelf' }
      ],
      targetCurve: 'flat', // or 'house_curve'
      frequencies: [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000],
      correction: [2.1, 1.8, 1.2, 0.8, 0.3, -0.2, -0.8, 1.5, 2.0, 1.8]
    },

    // Corrected frequency response
    corrected: {
      measurementId: 'meas-123',
      correctionApplied: true,
      correctionCurve: 'Room Correction Curve A',
      timestamp: '2024-01-15T12:30:00Z',
      originalMagnitude: [-2.1, -1.8, -1.2, -0.8, -0.3, 0.2, 0.8, 1.2, 1.0, 0.5],
      correctedMagnitude: [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, -0.3, -0.8, -1.3],
      improvement: {
        flatnessScoreBefore: 0.73,
        flatnessScoreAfter: 0.91,
        standardDeviationBefore: 4.2,
        standardDeviationAfter: 1.8,
        maxDeviationBefore: 8.7,
        maxDeviationAfter: 3.2
      }
    },

    // Peaks analysis
    peaks: {
      measurementId: 'meas-123',
      threshold: 3.0, // dB threshold
      peaks: [
        {
          frequency: 245,
          magnitude: 8.7,
          qFactor: 2.8,
          bandwidth: 87.5,
          prominence: 6.3,
          type: 'room_mode',
          severity: 'high'
        },
        {
          frequency: 1250,
          magnitude: 4.2,
          qFactor: 1.5,
          bandwidth: 833.3,
          prominence: 3.1,
          type: 'resonance',
          severity: 'medium'
        },
        {
          frequency: 6300,
          magnitude: 3.8,
          qFactor: 0.9,
          bandwidth: 7000,
          prominence: 2.8,
          type: 'reflection',
          severity: 'low'
        }
      ],
      analysis: {
        totalPeaks: 3,
        highSeverityPeaks: 1,
        mediumSeverityPeaks: 1,
        lowSeverityPeaks: 1,
        recommendations: [
          'Apply narrow notch filter at 245Hz with Q=2.8 and -8.7dB gain',
          'Consider bass trap placement to address 245Hz room mode',
          'Medium priority: Address 1250Hz resonance with moderate EQ'
        ]
      }
    },

    // Dips analysis
    dips: {
      measurementId: 'meas-123',
      threshold: -3.0, // dB threshold
      dips: [
        {
          frequency: 125,
          magnitude: -8.2,
          qFactor: 3.1,
          bandwidth: 40.3,
          prominence: -6.8,
          type: 'cancellation',
          severity: 'high'
        },
        {
          frequency: 3200,
          magnitude: -5.5,
          qFactor: 1.8,
          bandwidth: 1777.8,
          prominence: -4.2,
          type: 'reflection',
          severity: 'medium'
        },
        {
          frequency: 15000,
          magnitude: -4.1,
          qFactor: 0.7,
          bandwidth: 21428.6,
          prominence: -3.3,
          type: 'absorption',
          severity: 'low'
        }
      ],
      analysis: {
        totalDips: 3,
        highSeverityDips: 1,
        mediumSeverityDips: 1,
        lowSeverityDips: 1,
        recommendations: [
          'High priority: Address 125Hz cancellation with room treatment or EQ boost',
          'Medium priority: Boost 3200Hz region with +5.5dB peaking filter',
          'Low priority: Natural treble rolloff at 15kHz is acceptable'
        ]
      }
    },

    // Target curve for comparison
    targetCurve: {
      name: 'Harman Target Curve',
      type: 'house_curve',
      frequencies: [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000],
      magnitude: [2.0, 1.5, 1.0, 0.5, 0.0, 0.0, -1.0, -2.0, -4.0, -8.0],
      description: 'Research-based target curve for optimal listening experience',
      source: 'Harman International Research'
    },

    // Curve fitting results
    curveFitting: {
      measurementId: 'meas-123',
      targetCurve: 'Harman Target Curve',
      rSquared: 0.78, // Coefficient of determination
      rmse: 3.45, // Root Mean Square Error in dB
      mae: 2.67, // Mean Absolute Error in dB
      fittedCurve: {
        frequencies: [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000],
        magnitude: [1.8, 1.2, 0.8, 0.3, -0.2, -0.1, -1.2, -2.3, -4.2, -8.5]
      },
      residuals: [0.2, 0.3, 0.2, 0.2, 0.2, 0.1, 0.2, 0.3, 0.2, 0.5],
      goodnessOfFit: 'good', // excellent, good, fair, poor
      recommendations: [
        'Overall good fit to target curve',
        'Minor deviations in bass region can be corrected with EQ',
        'Treble response closely matches target'
      ]
    },

    // Comprehensive report
    report: {
      measurementId: 'meas-123',
      timestamp: '2024-01-15T13:00:00Z',
      summary: {
        overallScore: 7.3, // 0-10 scale
        flatnessRating: 'good', // excellent, good, fair, poor
        bassResponse: 'above_average',
        midrangeResponse: 'excellent',
        trebleResponse: 'good',
        phaseResponse: 'fair'
      },
      statistics: {
        averageLevel: -2.3,
        dynamicRange: 21.1,
        flatnessScore: 0.73
      },
      peaks: [
        { frequency: 245, magnitude: 8.7, severity: 'high' },
        { frequency: 1250, magnitude: 4.2, severity: 'medium' }
      ],
      dips: [
        { frequency: 125, magnitude: -8.2, severity: 'high' },
        { frequency: 3200, magnitude: -5.5, severity: 'medium' }
      ],
      recommendations: [
        'Apply bass trap at 245Hz to address primary room mode',
        'Use parametric EQ to correct 125Hz null with +8dB boost, Q=3.1',
        'Consider acoustic treatment to improve overall smoothness',
        'Phase response suggests some reflections - check speaker positioning'
      ],
      correctionSuggestions: {
        eqSettings: [
          { frequency: 245, gain: -8.7, q: 2.8, type: 'peaking' },
          { frequency: 125, gain: 8.2, q: 3.1, type: 'peaking' },
          { frequency: 3200, gain: 5.5, q: 1.8, type: 'peaking' }
        ],
        roomTreatment: [
          'Bass trap in corner for 245Hz mode',
          'Diffusion panels on rear wall',
          'Absorption at first reflection points'
        ]
      }
    },

    // Large dataset for performance testing
    largeDataset: (() => {
      const dataset = {
        measurementId: 'meas-large',
        timestamp: '2024-01-15T10:30:00Z',
        sampleRate: 192000, // High sample rate
        fftSize: 131072, // Large FFT size
        frequencies: [],
        magnitude: [],
        phase: [],
        coherence: []
      };
      
      // Generate 65536 data points (half of FFT size)
      for (let i = 0; i < 65536; i++) {
        const freq = (i + 1) * (192000 / 2) / 65536; // Nyquist frequency division
        dataset.frequencies.push(parseFloat(freq.toFixed(2)));
        
        // Generate realistic magnitude
        let mag = -60 + 60 * Math.random(); // -60 to 0 dB range
        if (freq < 20) mag -= 40; // Low frequency rolloff
        if (freq > 20000) mag -= 20 * Math.log10(freq / 20000); // High frequency rolloff
        dataset.magnitude.push(parseFloat(mag.toFixed(2)));
        
        // Generate phase
        const phase = -180 + 360 * Math.random();
        dataset.phase.push(parseFloat(phase.toFixed(2)));
        
        // Generate coherence
        const coherence = 0.5 + 0.5 * Math.random();
        dataset.coherence.push(parseFloat(coherence.toFixed(3)));
      }
      
      return dataset;
    })(),

    // Extreme frequency range
    extremeRange: {
      measurementId: 'meas-extreme',
      timestamp: '2024-01-15T10:30:00Z',
      sampleRate: 384000, // Very high sample rate
      startFreq: 0.1,
      endFreq: 192000,
      frequencies: (() => {
        const freqs = [];
        for (let i = 0; i < 1000; i++) {
          const freq = 0.1 * Math.pow(1920000, i / 999); // 0.1Hz to 192kHz log scale
          freqs.push(parseFloat(freq.toFixed(2)));
        }
        return freqs;
      })(),
      magnitude: (() => {
        const mags = [];
        for (let i = 0; i < 1000; i++) {
          const freq = 0.1 * Math.pow(1920000, i / 999);
          let mag = 0;
          
          // Extreme low frequency response
          if (freq < 1) mag -= 60 * (1 - freq);
          
          // Normal response 1Hz to 20kHz
          if (freq >= 1 && freq <= 20000) {
            mag += Math.sin(Math.log10(freq)) * 5;
          }
          
          // Ultrasonic rolloff
          if (freq > 20000) {
            mag -= 40 * Math.log10(freq / 20000);
          }
          
          mags.push(parseFloat(mag.toFixed(2)));
        }
        return mags;
      })(),
      phase: (() => {
        const phases = [];
        for (let i = 0; i < 1000; i++) {
          const freq = 0.1 * Math.pow(1920000, i / 999);
          let phase = -360 * Math.log10(freq / 0.1) / Math.log10(1920000);
          
          // Wrap phase
          while (phase > 180) phase -= 360;
          while (phase < -180) phase += 360;
          
          phases.push(parseFloat(phase.toFixed(2)));
        }
        return phases;
      })()
    },

    // Corrupted data for error testing
    corrupted: {
      measurementId: 'meas-corrupted',
      frequencies: [20, 50, 100, null, 500], // null value
      magnitude: [1, 2, 3], // Mismatched array length
      phase: ['invalid', 'data', 'types'], // Wrong data types
      sampleRate: 'not_a_number', // Invalid sample rate
      fftSize: -1024 // Invalid FFT size
    },

    // Incomplete data for validation testing
    incomplete: {
      measurementId: 'meas-incomplete',
      frequencies: [20, 50, 100],
      // Missing magnitude and phase arrays
      sampleRate: 48000
    }
  }
};