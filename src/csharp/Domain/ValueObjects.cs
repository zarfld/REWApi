using System;
using System.Collections.Generic;

namespace REWApi.Domain
{
    /// <summary>
    /// Configuration for REW sweep measurements
    /// </summary>
    public class SweepConfig
    {
        public double StartFreq { get; set; } = 20;
        public double EndFreq { get; set; } = 20000;
        public double Length { get; set; } = 512; // k samples
        public double Level { get; set; } = -12; // dBFS
        public int Repetitions { get; set; } = 1;
        public double StartDelay { get; set; } = 0;
        public bool CaptureNoiseFloor { get; set; } = false;
        public string? OutputPath { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Options for generating waterfall plots
    /// </summary>
    public class WaterfallOptions
    {
        public WaterfallMode Mode { get; set; } = WaterfallMode.Fourier;
        public int Slices { get; set; } = 101;
        public int PPO { get; set; } = 48; // Points per octave
        public WindowType WindowType { get; set; } = WindowType.Hann;
        public double TimeRangeMs { get; set; } = 500;
    }

    /// <summary>
    /// Options for RT60 analysis
    /// </summary>
    public class RT60Options
    {
        public int OctaveFraction { get; set; } = 3; // 1/3 octave
        public double StartFrequency { get; set; } = 20;
        public double EndFrequency { get; set; } = 20000;
        public RT60Method Method { get; set; } = RT60Method.T20;
        public bool EnableSmoothing { get; set; } = true;
    }

    /// <summary>
    /// Smoothing options for measurements
    /// </summary>
    public class SmoothingOptions
    {
        public double OctaveFraction { get; set; } = 1.0/12.0; // 1/12 octave
        public SmoothingType Type { get; set; } = SmoothingType.Octave;
        public bool ApplyToPhase { get; set; } = false;
    }

    /// <summary>
    /// Audio driver types supported by REW
    /// </summary>
    public enum AudioDriverType
    {
        Java,
        ASIO
    }

    /// <summary>
    /// Waterfall generation modes
    /// </summary>
    public enum WaterfallMode
    {
        Fourier,
        ConstantQ,
        Wavelet
    }

    /// <summary>
    /// Window types for signal processing
    /// </summary>
    public enum WindowType
    {
        Hann,
        Hamming,
        Blackman,
        Kaiser,
        Rectangle
    }

    /// <summary>
    /// RT60 calculation methods
    /// </summary>
    public enum RT60Method
    {
        T20,
        T30,
        T60,
        EDT
    }

    /// <summary>
    /// Smoothing types
    /// </summary>
    public enum SmoothingType
    {
        Octave,
        Linear,
        Psychoacoustic
    }

    /// <summary>
    /// Export formats for measurements
    /// </summary>
    public enum ExportFormat
    {
        MDAT,
        TXT,
        CSV,
        WAV
    }

    /// <summary>
    /// Java audio device configuration
    /// </summary>
    public class JavaAudioConfig
    {
        public string? InputDevice { get; set; }
        public string? Input { get; set; }
        public string? OutputDevice { get; set; }
        public string? Output { get; set; }
        public int InputChannel { get; set; } = 0;
        public int RefInputChannel { get; set; } = 0;
        public int LastInputChannel { get; set; } = 0;
        public string? OutputChannel { get; set; }
        public string? RefOutputChannel { get; set; }
        public List<string> OutputChannelMapping { get; set; } = new();
        public bool StereoOnly { get; set; } = false;
    }

    /// <summary>
    /// ASIO audio device configuration
    /// </summary>
    public class ASIOAudioConfig
    {
        public string? Device { get; set; }
        public string? Input { get; set; }
        public string? Output { get; set; }
        public string? RefOutput { get; set; }
        public string? RefInput { get; set; }
        public string? LastInput { get; set; }
        public bool SecondaryOutputEnabled { get; set; } = false;
        public string? SecondaryOutput { get; set; }
    }

    /// <summary>
    /// Calibration configuration
    /// </summary>
    public class CalibrationConfig
    {
        public bool Enabled { get; set; }
        public string? CalibrationFile { get; set; }
        public double OffsetdB { get; set; } = 0;
        public double DelayMs { get; set; } = 0;
    }

    /// <summary>
    /// Current audio system state
    /// </summary>
    public class AudioState
    {
        public bool AudioEnabled { get; set; }
        public bool AudioEndpointsReady { get; set; }
        public AudioDriverType Driver { get; set; }
        public int SampleRate { get; set; }
        public int[] SupportedSampleRates { get; set; } = Array.Empty<int>();
        public bool Treat32BitAs24Bit { get; set; }
        public JavaAudioConfig? Java { get; set; }
        public ASIOAudioConfig? ASIO { get; set; }
    }

    /// <summary>
    /// Application status information
    /// </summary>
    public class ApplicationStatus
    {
        public string Version { get; set; } = string.Empty;
        public bool BlockingMode { get; set; }
        public bool CommandInProgress { get; set; }
        public string? CurrentCommand { get; set; }
        public bool LoggingEnabled { get; set; }
        public string? LogFilePath { get; set; }
    }

    /// <summary>
    /// Application error information
    /// </summary>
    public class ApplicationError
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    }

    /// <summary>
    /// Application warning information
    /// </summary>
    public class ApplicationWarning
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Java audio devices information
    /// </summary>
    public class JavaDevices
    {
        public List<string> InputDevices { get; set; } = new();
        public List<string> Inputs { get; set; } = new();
        public List<string> OutputDevices { get; set; } = new();
        public List<string> Outputs { get; set; } = new();
        public List<string> OutputChannels { get; set; } = new();
        public List<string> RefOutputChannels { get; set; } = new();
        public int NumInputChannels { get; set; }
    }

    /// <summary>
    /// Measurement summary information
    /// </summary>
    public class MeasurementSummary
    {
        public string UUID { get; set; } = string.Empty;
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime Date { get; set; }
        public GroupInfo? Group { get; set; }
        public bool IsSelected { get; set; }
        public double StartFreq { get; set; }
        public double EndFreq { get; set; }
        public bool Inverted { get; set; }
        public string? REWVersion { get; set; }
        public string? TimingReference { get; set; }
        public double Delay { get; set; }
        public double TimingOffset { get; set; }
        public double SignalToNoisedB { get; set; }
        public double SPLOffsetdB { get; set; }
        public double AlignSPLOffsetdB { get; set; }
    }

    /// <summary>
    /// Group information for measurements
    /// </summary>
    public class GroupInfo
    {
        public string UUID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Filter for listing measurements
    /// </summary>
    public class MeasurementFilter
    {
        public string? GroupId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TitleContains { get; set; }
        public bool? IsSelected { get; set; }
    }

    /// <summary>
    /// Result of measurement operations
    /// </summary>
    public class MeasurementResult
    {
        public string MeasurementId { get; set; } = string.Empty;
        public MeasurementSummary? Summary { get; set; }
        public string? ExportPath { get; set; }
        public ProcessingMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Processing metrics for measurements
    /// </summary>
    public class ProcessingMetrics
    {
        public double SNR { get; set; }
        public double THD { get; set; }
        public double PeakLevel { get; set; }
        public double RMSLevel { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Frequency response data with decoded arrays
    /// </summary>
    public class FrequencyResponseData
    {
        public string Smoothing { get; set; } = string.Empty;
        public double StartFrequency { get; set; }
        public int? PointsPerOctave { get; set; }
        public double? FrequencyStep { get; set; }
        public float[] Magnitudes { get; set; } = Array.Empty<float>();
        public float[] Phases { get; set; } = Array.Empty<float>();
        public double[] Frequencies { get; set; } = Array.Empty<double>();
        public bool IsLogSpaced => PointsPerOctave.HasValue;
    }

    /// <summary>
    /// Impulse response data with decoded arrays
    /// </summary>
    public class ImpulseResponseData
    {
        public double StartTime { get; set; }
        public string TimingReference { get; set; } = string.Empty;
        public double SampleInterval { get; set; }
        public int SampleRate { get; set; }
        public float[] Data { get; set; } = Array.Empty<float>();
        public double Duration => Data.Length * SampleInterval;
    }

    /// <summary>
    /// Waterfall plot result
    /// </summary>
    public class WaterfallResult
    {
        public double[] Frequencies { get; set; } = Array.Empty<double>();
        public double[] Times { get; set; } = Array.Empty<double>();
        public float[,] DecodedSPLData { get; set; } = new float[0,0];
        public WaterfallMode Mode { get; set; }
        public int Slices { get; set; }
        public int PPO { get; set; }
    }

    /// <summary>
    /// RT60 analysis result
    /// </summary>
    public class RT60Result
    {
        public double[] Frequencies { get; set; } = Array.Empty<double>();
        public double[] RT60Values { get; set; } = Array.Empty<double>();
        public double[] T20Values { get; set; } = Array.Empty<double>();
        public double[] T30Values { get; set; } = Array.Empty<double>();
        public double[] EDTValues { get; set; } = Array.Empty<double>();
        public RT60Method Method { get; set; }
        public int OctaveFraction { get; set; }
    }



    /// <summary>
    /// Spectrogram result
    /// </summary>
    public class SpectrogramResult
    {
        public double[] Frequencies { get; set; } = Array.Empty<double>();
        public double[] Times { get; set; } = Array.Empty<double>();
        public float[,] SPLData { get; set; } = new float[0, 0];
        public int FFTSize { get; set; }
        public string WindowType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Distortion analysis data
    /// Returns data as 2D array with column headers as per REW API spec
    /// </summary>
    public class DistortionData
    {
        /// <summary>
        /// Type of distortion data (e.g., "Sweep distortion")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Column headers for the data (e.g., ["Frequency (Hz)", "THD (%)", "THD+N (%)"])
        /// </summary>
        public string[] Headers { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 2D array of distortion data [row][column]
        /// Each row is a frequency point, columns correspond to Headers
        /// </summary>
        public double[][] Data { get; set; } = Array.Empty<double[]>();

        /// <summary>
        /// Unit for distortion values (e.g., "%" or "dBr")
        /// </summary>
        public string Unit { get; set; } = "%";

        /// <summary>
        /// Points per octave (for sweep distortion)
        /// </summary>
        public int PPO { get; set; } = 12;
    }

    /// <summary>
    /// Processing operation result
    /// </summary>
    public class ProcessingResult
    {
        public string ProcessName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Results { get; set; } = new();
        public bool Success { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// Processing options for measurements
    /// </summary>
    public class ProcessingOptions
    {
        public string? Smoothing { get; set; }
        public bool GenerateRT60 { get; set; }
        public int RT60OctaveFraction { get; set; } = 3;
        public ExportFormat ExportFormat { get; set; } = ExportFormat.MDAT;
        public bool IncludeCalibration { get; set; } = true;
        public bool AppendLowFrequencyTail { get; set; }
        public double LowFrequencyTailStart { get; set; } = 1000;
        public double LowFrequencyTailSlope { get; set; } = 6;
        public bool AppendHighFrequencyTail { get; set; }
        public double HighFrequencyTailStart { get; set; } = 10000;
        public double HighFrequencyTailSlope { get; set; } = -6;
        public bool FrequencyWarping { get; set; }
        public bool ReplicateData { get; set; }
    }

    /// <summary>
    /// REW version and capability information
    /// </summary>
    public class REWVersion
    {
        public string Version { get; set; } = string.Empty;
        public string? BuildDate { get; set; }
        public string? APIVersion { get; set; }
        public string? OpenAPIHash { get; set; }
        public bool IsProVersion { get; set; }
        public string[] SupportedFeatures { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Command execution status for diagnostics
    /// </summary>
    public class CommandStatus
    {
        public bool IsExecuting { get; set; }
        public string? CurrentCommand { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan? ElapsedTime { get; set; }
        public int QueuedCommands { get; set; }
    }

    /// <summary>
    /// REW Application Settings Configuration
    /// </summary>
    public class ApplicationSettings
    {
        public string? Theme { get; set; }
        public bool AutoUpdate { get; set; }
        public string? DefaultMeasurementFolder { get; set; }
        public double DefaultSampleRate { get; set; } = 48000;
        public int BufferSize { get; set; } = 1024;
        public bool EnableLogging { get; set; } = true;
        public string? LogLevel { get; set; } = "INFO";
    }

    /// <summary>
    /// REW Audio Device and Settings Configuration
    /// </summary>
    public class AudioSettings
    {
        public string? InputDevice { get; set; }
        public string? OutputDevice { get; set; }
        public double SampleRate { get; set; } = 48000;
        public int BufferSize { get; set; } = 1024;
        public double InputGain { get; set; } = 0.0;
        public double OutputLevel { get; set; } = -12.0;
        public bool EnableCalibration { get; set; } = false;
        public string? CalibrationFile { get; set; }
    }

    /// <summary>
    /// REW Measurement Data Container
    /// </summary>
    public class MeasurementData
    {
        public string? Title { get; set; }
        public DateTime Timestamp { get; set; }
        public double[] Frequency { get; set; } = Array.Empty<double>();
        public double[] Magnitude { get; set; } = Array.Empty<double>();
        public double[] Phase { get; set; } = Array.Empty<double>();
        public double[] Coherence { get; set; } = Array.Empty<double>();
        public string? Notes { get; set; }
        public AudioSettings? AudioSettings { get; set; }
    }

    /// <summary>
    /// REW EQ Settings and Configuration
    /// </summary>
    public class EQSettings
    {
        public bool Enabled { get; set; }
        public string? EQType { get; set; } = "Parametric";
        public int MaxFilters { get; set; } = 20;
        public double TargetCurve { get; set; } = 0.0;
        public string? FilterFile { get; set; }
        public bool AutoEQ { get; set; } = false;
    }

    /// <summary>
    /// Equaliser information (manufacturer and model)
    /// Maps to REW API equaliser objects
    /// </summary>
    public class EqualiserInfo
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }

    /// <summary>
    /// Individual filter setting
    /// Maps to REW API filter objects
    /// </summary>
    public class FilterSetting
    {
        public int Index { get; set; }
        public string Type { get; set; } = "None";
        public bool Enabled { get; set; }
        public bool IsAuto { get; set; }
        public double? Fc { get; set; }  // Center frequency (Hz)
        public double? Gain { get; set; }  // Gain (dB)
        public double? Q { get; set; }  // Quality factor
    }

    /// <summary>
    /// Target settings for EQ matching
    /// Maps to REW API target-settings objects
    /// </summary>
    public class TargetSettings
    {
        public string Shape { get; set; } = "Full range";  // "Full range", "Subwoofer", "LF", "LF + sub", "Other"
        public int BassManagementSlopedBPerOctave { get; set; } = 12;
        public int BassManagementCutoffHz { get; set; } = 80;
        public int LowFreqSlopedBPerOctave { get; set; } = 24;
        public int LowFreqCutoffHz { get; set; } = 10;
        public string LowPassCrossoverType { get; set; } = "L-R2";  // "Butterworth", "L-R2", "L-R4"
        public string HighPassCrossoverType { get; set; } = "L-R2";
        public int LowPassCutoffHz { get; set; } = 1000;
        public int HighPassCutoffHz { get; set; } = 100;
    }

    /// <summary>
    /// Room curve settings for target curve adjustment
    /// Maps to REW API room-curve-settings objects
    /// </summary>
    public class RoomCurveSettings
    {
        public bool AddRoomCurve { get; set; }
        public int LowFreqRiseStartHz { get; set; } = 200;
        public int LowFreqRiseEndHz { get; set; } = 20;
        public double LowFreqRiseSlopedBPerOctave { get; set; } = 1.0;
        public int HighFreqFallStartHz { get; set; } = 1000;
        public double HighFreqFallSlopedBPerOctave { get; set; } = 0.5;
    }

    /// <summary>
    /// EQ Command Result
    /// Returned from executing EQ commands (Match target, Optimize, etc.)
    /// </summary>
    public class EQCommandResult
    {
        /// <summary>Status/progress message from command execution</summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>Operation ID for tracking progress (if long-running)</summary>
        public int? Id { get; set; }
    }

    /// <summary>
    /// Match Target Settings
    /// Controls parameters for the filter matching algorithm when matching to target curves
    /// Maps to REW API /eq/match-target-settings
    /// </summary>
    public class MatchTargetSettings
    {
        /// <summary>Start frequency for matching in Hz (typically 20)</summary>
        public int StartFrequency { get; set; } = 20;
        
        /// <summary>End frequency for matching in Hz (typically 100-20000)</summary>
        public int EndFrequency { get; set; } = 100;
        
        /// <summary>Maximum boost per individual filter in dB (typically 0-12)</summary>
        public int IndividualMaxBoostdB { get; set; } = 9;
        
        /// <summary>Overall maximum boost across all filters in dB (typically 0-6)</summary>
        public int OverallMaxBoostdB { get; set; } = 0;
        
        /// <summary>Flatness target in dB - how close to match the target (typically 1-5)</summary>
        public int FlatnessTargetdB { get; set; } = 3;
        
        /// <summary>Allow narrow Q filters below 200Hz</summary>
        public bool AllowNarrowFiltersBelow200Hz { get; set; } = true;
        
        /// <summary>Vary Q values above 200Hz for better matching</summary>
        public bool VaryQAbove200Hz { get; set; } = false;
        
        /// <summary>Allow use of low shelf filters</summary>
        public bool AllowLowShelf { get; set; } = false;
        
        /// <summary>Minimum gain for low shelf in dB</summary>
        public int LowShelfMin { get; set; } = -6;
        
        /// <summary>Maximum gain for low shelf in dB</summary>
        public int LowShelfMax { get; set; } = 6;
        
        /// <summary>Allow use of high shelf filters</summary>
        public bool AllowHighShelf { get; set; } = false;
        
        /// <summary>Minimum gain for high shelf in dB</summary>
        public int HighShelfMin { get; set; } = -6;
        
        /// <summary>Maximum gain for high shelf in dB</summary>
        public int HighShelfMax { get; set; } = 6;
    }

    /// <summary>
    /// SPL Meter Command Parameters
    /// </summary>
    public class SPLMeterCommandParameters
    {
        public double Duration { get; set; } = 30.0; // seconds
        public string? WeightingFilter { get; set; } = "A"; // A, C, Z
        public string? TimeWeighting { get; set; } = "Fast"; // Fast, Slow
        public bool LogData { get; set; } = true;
        public string? OutputFile { get; set; }
    }

    /// <summary>
    /// Real-Time Analyzer Command Parameters
    /// </summary>
    public class RTACommandParameters
    {
        public double Duration { get; set; } = 60.0; // seconds
        public int FFTSize { get; set; } = 8192;
        public string? WindowType { get; set; } = "Hanning";
        public double UpdateRate { get; set; } = 10.0; // Hz
        public bool ShowPeakHold { get; set; } = true;
        public string? OutputFile { get; set; }
    }

    /// <summary>
    /// Impulse Response Window settings for a measurement
    /// Used to configure IR windowing parameters in REW
    /// </summary>
    public class IRWindows
    {
        /// <summary>
        /// Type of the left window (e.g., "Tukey 0.25", "Hann", "Rectangular")
        /// </summary>
        public string LeftWindowType { get; set; } = string.Empty;

        /// <summary>
        /// Type of the right window (e.g., "Tukey 0.25", "Hann", "Rectangular")
        /// </summary>
        public string RightWindowType { get; set; } = string.Empty;

        /// <summary>
        /// Width of the left window in milliseconds
        /// </summary>
        public double LeftWindowWidthms { get; set; }

        /// <summary>
        /// Width of the right window in milliseconds
        /// </summary>
        public double RightWindowWidthms { get; set; }

        /// <summary>
        /// Reference time in milliseconds (timing reference point)
        /// </summary>
        public double RefTimems { get; set; }

        /// <summary>
        /// Whether to add Frequency Dependent Windowing (FDW)
        /// </summary>
        public bool AddFDW { get; set; }

        /// <summary>
        /// Width of the FDW in cycles or octaves
        /// Note: Can be in cycles or octaves depending on REW View preferences
        /// </summary>
        public double FdwWidthCycles { get; set; }

        /// <summary>
        /// Domain behavior: Set symmetric windowing (same settings for left and right)
        /// </summary>
        public void SetSymmetricWindowing(string windowType, double widthMs)
        {
            LeftWindowType = RightWindowType = windowType;
            LeftWindowWidthms = RightWindowWidthms = widthMs;
        }

        /// <summary>
        /// Domain behavior: Validate window settings
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(LeftWindowType) 
                && !string.IsNullOrEmpty(RightWindowType)
                && LeftWindowWidthms >= 0 
                && RightWindowWidthms >= 0;
        }
    }

    #region SPL Meter Domain Objects

    /// <summary>
    /// SPL Meter configuration value object with domain behavior
    /// ISO/IEC/IEEE 29148:2018 - Requirements Engineering
    /// Maps to REW API /spl-meter/{id}/configuration
    /// </summary>
    public class SPLMeterConfiguration
    {
        /// <summary>
        /// Display mode (only affects meter display, not data returned)
        /// Options: "SPL", etc. (get from /spl-meter/modes)
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Frequency weighting
        /// Options: "A", "C", "Z" (get from /spl-meter/weightings)
        /// </summary>
        public string Weighting { get; set; }

        /// <summary>
        /// Time weighting filter
        /// Options: "Fast", "Slow", "Impulse" (get from /spl-meter/filters)
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// High-pass filter active flag
        /// </summary>
        public bool HighPassActive { get; set; }

        /// <summary>
        /// Rolling Leq calculation active flag
        /// </summary>
        public bool RollingLeqActive { get; set; }

        /// <summary>
        /// Rolling Leq time window in minutes
        /// </summary>
        public int RollingLeqMinutes { get; set; }

        #region Domain Behavior

        /// <summary>
        /// Domain behavior: Validate configuration
        /// Ensures all required fields are set with valid values
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Mode)
                && !string.IsNullOrEmpty(Weighting)
                && !string.IsNullOrEmpty(Filter)
                && RollingLeqMinutes > 0;
        }

        /// <summary>
        /// Domain behavior: Set standard A-weighting Fast configuration
        /// Common configuration for general sound level monitoring
        /// </summary>
        public void SetStandardAWeightingFast()
        {
            Mode = "SPL";
            Weighting = "A";
            Filter = "Fast";
            HighPassActive = false;
            RollingLeqActive = false;
            RollingLeqMinutes = 1;
        }

        /// <summary>
        /// Domain behavior: Set C-weighting Slow with rolling Leq
        /// Common configuration for long-term monitoring
        /// </summary>
        public void SetLongTermMonitoring(int rollingMinutes = 15)
        {
            Mode = "SPL";
            Weighting = "C";
            Filter = "Slow";
            HighPassActive = false;
            RollingLeqActive = true;
            RollingLeqMinutes = rollingMinutes;
        }

        #endregion
    }

    /// <summary>
    /// SPL meter reading values
    /// ISO/IEC/IEEE 29148:2018 - Requirements Engineering
    /// Maps to REW API /spl-meter/{id}/levels
    /// Contains SPL, Leq, SEL measurements
    /// </summary>
    public class SPLValues
    {
        /// <summary>
        /// Meter number (1-4)
        /// </summary>
        public int MeterNumber { get; set; }

        /// <summary>
        /// Weighting applied to measurements
        /// </summary>
        public string Weighting { get; set; }

        /// <summary>
        /// Time weighting filter applied
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Current SPL value in dB
        /// </summary>
        public double SPL { get; set; }

        /// <summary>
        /// Equivalent continuous sound level (Leq) in dB
        /// </summary>
        public double Leq { get; set; }

        /// <summary>
        /// Indicates if Leq is a rolling (time-windowed) value
        /// </summary>
        public bool IsRollingLeq { get; set; }

        /// <summary>
        /// Rolling Leq time window in minutes (if applicable)
        /// </summary>
        public int RollingLeqMinutes { get; set; }

        /// <summary>
        /// 1-minute Leq value
        /// </summary>
        public double Leq1m { get; set; }

        /// <summary>
        /// 10-minute Leq value
        /// </summary>
        public double Leq10m { get; set; }

        /// <summary>
        /// Sound Exposure Level (SEL) in dB
        /// </summary>
        public double SEL { get; set; }

        /// <summary>
        /// Elapsed measurement time in seconds
        /// </summary>
        public double ElapsedTime { get; set; }

        #region Domain Behavior

        /// <summary>
        /// Domain behavior: Check if measurement is valid
        /// Ensures values are within reasonable ranges
        /// </summary>
        public bool IsValid()
        {
            return SPL >= 0 && SPL <= 200 // Typical SPL range 0-200 dB
                && Leq >= 0 && Leq <= 200
                && SEL >= 0
                && ElapsedTime >= 0
                && !string.IsNullOrEmpty(Weighting)
                && !string.IsNullOrEmpty(Filter);
        }

        /// <summary>
        /// Domain behavior: Check if SPL exceeds threshold
        /// Useful for protection/monitoring scenarios
        /// </summary>
        public bool ExceedsThreshold(double thresholdDb)
        {
            return SPL > thresholdDb;
        }

        /// <summary>
        /// Domain behavior: Get equivalent Leq for specified time window
        /// Returns Leq1m or Leq10m based on requested minutes
        /// </summary>
        public double GetLeqForTimeWindow(int minutes)
        {
            if (minutes <= 1) return Leq1m;
            if (minutes <= 10) return Leq10m;
            return Leq; // Rolling Leq for longer periods
        }

        /// <summary>
        /// Domain behavior: Format SPL reading as string with weighting
        /// Example: "85.3 dB(A) Fast"
        /// </summary>
        public string FormatReading()
        {
            return $"{SPL:F1} dB({Weighting}) {Filter}";
        }

        #endregion
    }

    #endregion
}
