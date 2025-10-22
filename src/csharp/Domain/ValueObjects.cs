using System;
using System.Collections.Generic;
using System.Linq;

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
        [System.Text.Json.Serialization.JsonPropertyName("mode")]
        public string Mode { get; set; }

        /// <summary>
        /// Frequency weighting
        /// Options: "A", "C", "Z" (get from /spl-meter/weightings)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("weighting")]
        public string Weighting { get; set; }

        /// <summary>
        /// Time weighting filter
        /// Options: "Fast", "Slow", "Impulse" (get from /spl-meter/filters)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("filter")]
        public string Filter { get; set; }

        /// <summary>
        /// High-pass filter active flag
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("highPassActive")]
        public bool HighPassActive { get; set; }

        /// <summary>
        /// Rolling Leq calculation active flag
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rollingLeqActive")]
        public bool RollingLeqActive { get; set; }

        /// <summary>
        /// Rolling Leq time window in minutes
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rollingLeqMinutes")]
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
        [System.Text.Json.Serialization.JsonPropertyName("meterNumber")]
        public int MeterNumber { get; set; }

        /// <summary>
        /// Weighting applied to measurements
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("weighting")]
        public string Weighting { get; set; }

        /// <summary>
        /// Time weighting filter applied
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("filter")]
        public string Filter { get; set; }

        /// <summary>
        /// Current SPL value in dB
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("SPL")]
        public double SPL { get; set; }

        /// <summary>
        /// Equivalent continuous sound level (Leq) in dB
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("Leq")]
        public double Leq { get; set; }

        /// <summary>
        /// Indicates if Leq is a rolling (time-windowed) value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("isRollingLeq")]
        public bool IsRollingLeq { get; set; }

        /// <summary>
        /// Rolling Leq time window in minutes (if applicable)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rollingLeqMinutes")]
        [System.Text.Json.Serialization.JsonConverter(typeof(Domain.JsonConverters.EmptyStringToIntConverter))]
        public int RollingLeqMinutes { get; set; }

        /// <summary>
        /// 1-minute Leq value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("Leq1m")]
        public double Leq1m { get; set; }

        /// <summary>
        /// 10-minute Leq value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("Leq10m")]
        public double Leq10m { get; set; }

        /// <summary>
        /// Sound Exposure Level (SEL) in dB
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("SEL")]
        public double SEL { get; set; }

        /// <summary>
        /// Elapsed measurement time in seconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("elapsedTime")]
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
            return $"{SPL.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} dB({Weighting}) {Filter}";
        }

        #endregion
    }

    #endregion

    #region Generator Domain Objects

    /// <summary>
    /// Generator status information
    /// Maps to REW API /generator/status endpoint
    /// </summary>
    public class GeneratorStatus
    {
        /// <summary>
        /// Whether the generator is enabled (requires audio output available)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether the generator is currently playing
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("playing")]
        public bool Playing { get; set; }

        /// <summary>
        /// Currently selected signal type
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("signal")]
        public string Signal { get; set; } = "";

        /// <summary>
        /// Current output level
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        public double Level { get; set; }

        /// <summary>
        /// Unit for the level (e.g., "dBFS")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("levelUnit")]
        public string LevelUnit { get; set; } = "dBFS";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if generator can be started
        /// </summary>
        public bool CanStart() => Enabled && !Playing;

        /// <summary>
        /// Domain validation: Check if generator can be stopped
        /// </summary>
        public bool CanStop() => Enabled && Playing;

        #endregion
    }

    /// <summary>
    /// Generator signal configuration
    /// Different signals have different configuration options
    /// </summary>
    public class GeneratorConfiguration
    {
        // Common settings for all signals
        
        /// <summary>
        /// Add dither to the signal
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("addDither")]
        public bool AddDither { get; set; }

        /// <summary>
        /// Dither bit depth (when addDither is true)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("ditherBits")]
        [System.Text.Json.Serialization.JsonConverter(typeof(Domain.JsonConverters.EmptyStringToIntConverter))]
        public int DitherBits { get; set; }

        // Sine signal specific settings
        
        /// <summary>
        /// Frequency for sine wave (Hz)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("frequency")]
        public double? Frequency { get; set; }

        // Noise signal specific settings
        
        /// <summary>
        /// Noise color/type (Pink, White, etc.)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("noiseColor")]
        public string? NoiseColor { get; set; }

        /// <summary>
        /// Enable custom filtering for noise
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customFilter")]
        public bool? CustomFilter { get; set; }

        /// <summary>
        /// High-pass filter frequency for custom noise filtering
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("highPassFreq")]
        public double? HighPassFreq { get; set; }

        /// <summary>
        /// Low-pass filter frequency for custom noise filtering
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("lowPassFreq")]
        public double? LowPassFreq { get; set; }

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if configuration is valid for sine signal
        /// </summary>
        public bool IsValidForSine()
        {
            return Frequency.HasValue && Frequency.Value > 0 && Frequency.Value <= 24000;
        }

        /// <summary>
        /// Domain validation: Check if custom filter settings are valid
        /// Requires both HPF and LPF to be set, with HPF < LPF
        /// </summary>
        public bool IsValidCustomFilter()
        {
            // Both filters must be specified
            if (!HighPassFreq.HasValue || !LowPassFreq.HasValue)
                return false;
            
            // HPF must be less than LPF
            if (HighPassFreq.Value >= LowPassFreq.Value)
                return false;
            
            // Frequencies must be in valid range
            return HighPassFreq.Value > 0 && LowPassFreq.Value <= 24000;
        }

        #endregion
    }

    /// <summary>
    /// Generator protection settings
    /// Stops generator if input signal exceeds threshold or clips
    /// </summary>
    public class GeneratorProtectionSettings
    {
        /// <summary>
        /// Enable SPL limit abort - generator stops if input exceeds threshold
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("splLimitAbort")]
        public bool SPLLimitAbort { get; set; }

        /// <summary>
        /// Enable clipping abort - stops generator if input clips
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("clippingAbort")]
        public bool ClippingAbort { get; set; }

        /// <summary>
        /// dB SPL limit - threshold for SPL abort
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("dBSPLLimit")]
        public double DBSPLLimit { get; set; }

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if threshold is reasonable
        /// </summary>
        public bool IsReasonableThreshold()
        {
            // Typical SPL threshold range: 80-140 dB
            return DBSPLLimit >= 80 && DBSPLLimit <= 140;
        }

        /// <summary>
        /// Domain behavior: Create safe default protection settings
        /// </summary>
        public static GeneratorProtectionSettings CreateSafeDefaults()
        {
            return new GeneratorProtectionSettings
            {
                SPLLimitAbort = true,
                ClippingAbort = true,
                DBSPLLimit = 110.0 // Safe listening level
            };
        }

        #endregion
    }

    /// <summary>
    /// Generator signal response wrapper
    /// </summary>
    public class GeneratorSignalResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("signal")]
        public string Signal { get; set; } = "";
    }

    /// <summary>
    /// Generator level response wrapper
    /// </summary>
    public class GeneratorLevelResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public double Value { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "dBFS";
    }

    /// <summary>
    /// Wrapper for frequency response from API
    /// API returns {"value":1000.0, "unit":"Hz"}
    /// </summary>
    public class GeneratorFrequencyResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public double Value { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "Hz";
    }

    #endregion

    #region RTA (Real-Time Analyzer) Domain Objects

    /// <summary>
    /// RTA status from REW API
    /// Indicates whether RTA is enabled and running
    /// </summary>
    public class RTAStatus
    {
        /// <summary>
        /// Whether the RTA is enabled (audio input available)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Whether the RTA is currently running
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("running")]
        public bool Running { get; set; }

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if RTA can be started
        /// </summary>
        public bool CanStart() => Enabled && !Running;

        /// <summary>
        /// Domain validation: Check if RTA can be stopped
        /// </summary>
        public bool CanStop() => Enabled && Running;

        #endregion
    }

    /// <summary>
    /// RTA main configuration
    /// Controls frequency resolution, averaging, and analysis mode
    /// </summary>
    public class RTAConfiguration
    {
        /// <summary>
        /// Spectrum or fractional octave mode ("Spectrum" or "1/3", "1/6", etc.)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("mode")]
        public string Mode { get; set; } = "1/3";

        /// <summary>
        /// FFT length (samples) for spectrum mode
        /// REW API returns this as a string, so we use a converter
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fftLength")]
        [System.Text.Json.Serialization.JsonConverter(typeof(Domain.JsonConverters.EmptyStringToIntConverter))]
        public int FFTLength { get; set; } = 32768;

        /// <summary>
        /// Averaging type ("None", "Exponential", "Linear")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("averaging")]
        public string Averaging { get; set; } = "Exponential";

        /// <summary>
        /// Number of averages for linear averaging
        /// REW API returns this as a string, so we use a converter
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("averages")]
        [System.Text.Json.Serialization.JsonConverter(typeof(Domain.JsonConverters.EmptyStringToIntConverter))]
        public int Averages { get; set; } = 8;

        /// <summary>
        /// Decay rate for exponential averaging (0.0-1.0)
        /// REW API returns this as a string, so we use a converter
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("decay")]
        [System.Text.Json.Serialization.JsonConverter(typeof(Domain.JsonConverters.EmptyStringToDoubleConverter))]
        public double Decay { get; set; } = 0.5;

        /// <summary>
        /// Window type for FFT analysis
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("window")]
        public string Window { get; set; } = "Hann";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if FFT length is valid (power of 2)
        /// </summary>
        public bool IsValidFFTLength()
        {
            return FFTLength >= 512 && FFTLength <= 131072 && 
                   (FFTLength & (FFTLength - 1)) == 0; // Power of 2 check
        }

        /// <summary>
        /// Domain validation: Check if averaging settings are valid
        /// </summary>
        public bool IsValidAveraging()
        {
            if (Averaging == "Linear")
                return Averages >= 1 && Averages <= 100;
            if (Averaging == "Exponential")
                return Decay >= 0.0 && Decay <= 1.0;
            return Averaging == "None";
        }

        #endregion
    }

    /// <summary>
    /// RTA appearance/display configuration
    /// Controls visual aspects of the RTA display
    /// </summary>
    public class RTAAppearanceConfiguration
    {
        /// <summary>
        /// Display peak hold
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("peakHold")]
        public bool PeakHold { get; set; }

        /// <summary>
        /// Peak hold time in seconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("peakHoldTime")]
        public double PeakHoldTime { get; set; } = 5.0;

        /// <summary>
        /// Show RTA overlays
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("showOverlays")]
        public bool ShowOverlays { get; set; } = true;

        /// <summary>
        /// RTA trace color (hex format)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("traceColor")]
        public string TraceColor { get; set; } = "#00FF00";
    }

    /// <summary>
    /// RTA distortion measurement configuration
    /// Controls harmonic distortion analysis
    /// </summary>
    public class RTADistortionConfiguration
    {
        /// <summary>
        /// Enable harmonic distortion measurement
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Number of harmonics to measure (2-10)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("harmonics")]
        public int Harmonics { get; set; } = 5;

        /// <summary>
        /// High-pass filter frequency (Hz) for distortion measurement
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("hpFreq")]
        public double HPFreq { get; set; } = 20;

        /// <summary>
        /// Low-pass filter frequency (Hz) for distortion measurement
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("lpFreq")]
        public double LPFreq { get; set; } = 20000;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if harmonic count is valid
        /// </summary>
        public bool IsValidHarmonics()
        {
            return Harmonics >= 2 && Harmonics <= 10;
        }

        /// <summary>
        /// Domain validation: Check if filter settings are valid
        /// </summary>
        public bool IsValidFilterRange()
        {
            return HPFreq > 0 && LPFreq <= 24000 && HPFreq < LPFreq;
        }

        #endregion
    }

    /// <summary>
    /// RTA input level reading
    /// Timestamp + running sample count + level value
    /// </summary>
    public class RTALevel
    {
        /// <summary>
        /// Nanosecond timestamp of reading
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("timestampNanos")]
        public long TimestampNanos { get; set; }

        /// <summary>
        /// Running sum of samples processed since RTA started
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sampleCount")]
        public long SampleCount { get; set; }

        /// <summary>
        /// RMS level value (unit specified in request)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("rms")]
        public double RMS { get; set; }

        /// <summary>
        /// Peak level value (unit specified in request)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("peak")]
        public double Peak { get; set; }

        /// <summary>
        /// Unit for level values (dBFS, dBu, dBV, SPL)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "dBFS";
    }

    /// <summary>
    /// RTA distortion measurement results
    /// Includes fundamental, harmonics, THD, THD+N
    /// </summary>
    public class RTADistortion
    {
        /// <summary>
        /// Nanosecond timestamp of reading
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("timestampNanos")]
        public long TimestampNanos { get; set; }

        /// <summary>
        /// Running sum of samples processed since RTA started
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sampleCount")]
        public long SampleCount { get; set; }

        /// <summary>
        /// Fundamental frequency detected (Hz)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fundamental")]
        public double Fundamental { get; set; }

        /// <summary>
        /// Fundamental level (unit specified in request)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fundamentalLevel")]
        public double FundamentalLevel { get; set; }

        /// <summary>
        /// Array of harmonic levels (2nd, 3rd, 4th, etc.)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("harmonicLevels")]
        public double[] HarmonicLevels { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Total Harmonic Distortion (THD) in specified unit
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("thd")]
        public double THD { get; set; }

        /// <summary>
        /// Total Harmonic Distortion + Noise (THD+N) in specified unit
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("thdPlusN")]
        public double THDPlusN { get; set; }

        /// <summary>
        /// Unit for level values (dBFS, dBu, dBV, SPL)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("levelUnit")]
        public string LevelUnit { get; set; } = "dBFS";

        /// <summary>
        /// Unit for distortion values (dB or percent)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("distortionUnit")]
        public string DistortionUnit { get; set; } = "dB";

        #region Domain Behavior

        /// <summary>
        /// Domain calculation: Get level of specific harmonic
        /// </summary>
        public double GetHarmonicLevel(int harmonicNumber)
        {
            if (harmonicNumber < 2 || harmonicNumber > HarmonicLevels.Length + 1)
                throw new ArgumentOutOfRangeException(nameof(harmonicNumber));
            return HarmonicLevels[harmonicNumber - 2]; // 2nd harmonic is index 0
        }

        /// <summary>
        /// Domain validation: Check if distortion is within acceptable limits
        /// </summary>
        public bool IsLowDistortion(double thresholdPercent = 1.0)
        {
            if (DistortionUnit == "percent")
                return THD < thresholdPercent;
            else // dB
                return THD < -40; // ~1% = -40dB
        }

        #endregion
    }

    /// <summary>
    /// RTA graph display limits
    /// Y-axis range for the RTA display
    /// </summary>
    public class RTAGraphLimits
    {
        /// <summary>
        /// Minimum Y-axis value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("min")]
        public double Min { get; set; } = 0;

        /// <summary>
        /// Maximum Y-axis value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("max")]
        public double Max { get; set; } = 120;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if limits are valid
        /// </summary>
        public bool IsValidRange()
        {
            return Max > Min && (Max - Min) >= 10; // At least 10 dB range
        }

        #endregion
    }

    /// <summary>
    /// RTA image capture settings
    /// For saving RTA graph screenshots
    /// </summary>
    public class RTAImageCaptureSettings
    {
        /// <summary>
        /// Image width in pixels
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("width")]
        public int Width { get; set; } = 1920;

        /// <summary>
        /// Image height in pixels
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("height")]
        public int Height { get; set; } = 1080;

        /// <summary>
        /// Image format ("PNG", "JPEG", "SVG")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("format")]
        public string Format { get; set; } = "PNG";

        /// <summary>
        /// JPEG quality (1-100, only for JPEG format)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("quality")]
        public int Quality { get; set; } = 95;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if dimensions are reasonable
        /// </summary>
        public bool IsValidDimensions()
        {
            return Width >= 640 && Width <= 7680 && // 640p to 8K
                   Height >= 480 && Height <= 4320;
        }

        /// <summary>
        /// Domain validation: Check if quality is valid for JPEG
        /// </summary>
        public bool IsValidQuality()
        {
            return Format != "JPEG" || (Quality >= 1 && Quality <= 100);
        }

        #endregion
    }

    #endregion

    #region Measure Service Domain Objects

    /// <summary>
    /// Measurement naming configuration
    /// Controls how measurements are automatically named
    /// </summary>
    public class MeasureNaming
    {
        /// <summary>
        /// Selected naming option (e.g., "Automatic", "Custom", "Sequential")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("option")]
        public string Option { get; set; } = "Automatic";

        /// <summary>
        /// Custom prefix for measurement names
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("prefix")]
        public string Prefix { get; set; } = "";

        /// <summary>
        /// Whether to include date in measurement name
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("includeDate")]
        public bool IncludeDate { get; set; } = false;

        /// <summary>
        /// Date format string
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("dateFormat")]
        public string DateFormat { get; set; } = "";

        /// <summary>
        /// Whether to include time in measurement name
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("includeTime")]
        public bool IncludeTime { get; set; } = false;

        /// <summary>
        /// Time format string
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("timeFormat")]
        public string TimeFormat { get; set; } = "";

        /// <summary>
        /// Sequential number for naming
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sequenceNumber")]
        public int SequenceNumber { get; set; } = 1;

        /// <summary>
        /// Format of the next measurement name (read-only from API)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("nextName")]
        public string NextName { get; set; } = "";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if naming configuration is valid
        /// </summary>
        public bool IsValidConfiguration()
        {
            return SequenceNumber > 0;
        }

        #endregion
    }

    /// <summary>
    /// Measurement notes
    /// Notes to be included with the next measurement
    /// </summary>
    public class MeasureNotes
    {
        /// <summary>
        /// Notes text
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("notes")]
        public string Notes { get; set; } = "";
    }

    /// <summary>
    /// Measurement output level configuration
    /// </summary>
    public class MeasureLevel
    {
        /// <summary>
        /// Level value
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("level")]
        public double Level { get; set; } = -12.0;

        /// <summary>
        /// Unit for level (dBFS, dBu, dBV, SPL)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "dBFS";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if level is within safe range
        /// </summary>
        public bool IsSafeLevel()
        {
            if (Unit == "dBFS")
                return Level <= 0.0 && Level >= -96.0;
            if (Unit == "dBu" || Unit == "dBV")
                return Level <= 20.0 && Level >= -60.0;
            if (Unit == "SPL")
                return Level <= 120.0 && Level >= 40.0;
            return true; // Unknown unit, assume safe
        }

        #endregion
    }

    /// <summary>
    /// Protection options for measurements
    /// Abort measurements if limits are exceeded
    /// </summary>
    public class MeasureProtectionOptions
    {
        /// <summary>
        /// Abort on excessive input clipping
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("abortOnClipping")]
        public bool AbortOnClipping { get; set; } = false;

        /// <summary>
        /// Clipping detection threshold (%)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("clippingThreshold")]
        public double ClippingThreshold { get; set; } = 0.5;

        /// <summary>
        /// Abort if SPL exceeds limit
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("abortOnSPLExceeded")]
        public bool AbortOnSPLExceeded { get; set; } = false;

        /// <summary>
        /// SPL limit in dB
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("splLimit")]
        public double SPLLimit { get; set; } = 100.0;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if protection settings are reasonable
        /// </summary>
        public bool IsValidConfiguration()
        {
            return ClippingThreshold >= 0.0 && ClippingThreshold <= 100.0 &&
                   SPLLimit >= 40.0 && SPLLimit <= 140.0;
        }

        /// <summary>
        /// Domain behavior: Check if any protection is active
        /// </summary>
        public bool HasActiveProtection()
        {
            return AbortOnClipping || AbortOnSPLExceeded;
        }

        #endregion
    }

    /// <summary>
    /// Sweep configuration for measurements
    /// </summary>
    public class SweepConfiguration
    {
        /// <summary>
        /// Sweep start frequency in Hz
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("startFreq")]
        public double StartFreq { get; set; } = 20.0;

        /// <summary>
        /// Sweep end frequency in Hz
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("endFreq")]
        public double EndFreq { get; set; } = 20000.0;

        /// <summary>
        /// Sweep length (API returns strings like "256k", "512k", "1024k", etc.)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("length")]
        public string Length { get; set; } = "256k";

        /// <summary>
        /// Fill silence with dither to keep audio path active
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fillSilenceWithDither")]
        public bool FillSilenceWithDither { get; set; } = false;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if frequency range is valid
        /// </summary>
        public bool IsValidFrequencyRange()
        {
            return StartFreq > 0 && EndFreq > StartFreq && 
                   StartFreq >= 1.0 && EndFreq <= 96000.0;
        }

        /// <summary>
        /// Domain validation: Check if sweep length is valid (must be one of REW's supported values as string)
        /// </summary>
        public bool IsValidLength()
        {
            // REW supports specific sweep lengths as strings: "32k", "64k", "128k", "256k", "512k", "1024k", "2048k", "4096k"
            var validLengths = new[] { "32k", "64k", "128k", "256k", "512k", "1024k", "2048k", "4096k" };
            return validLengths.Contains(Length);
        }

        /// <summary>
        /// Domain calculation: Calculate frequency span in octaves
        /// </summary>
        public double GetFrequencySpanOctaves()
        {
            return Math.Log2(EndFreq / StartFreq);
        }

        #endregion
    }

    /// <summary>
    /// Sweep repetitions configuration
    /// </summary>
    public class SweepRepetitions
    {
        /// <summary>
        /// Number of sweep repetitions (1-16)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("repetitions")]
        public int Repetitions { get; set; } = 1;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if repetitions count is valid
        /// </summary>
        public bool IsValidCount()
        {
            return Repetitions >= 1 && Repetitions <= 16;
        }

        #endregion
    }

    /// <summary>
    /// Timing reference configuration
    /// </summary>
    public class TimingReference
    {
        /// <summary>
        /// Selected timing reference mode
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("reference")]
        public string Reference { get; set; } = "None";

        // Additional configuration properties would be added based on reference mode

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if reference mode is recognized
        /// </summary>
        public bool IsValidReference()
        {
            var validReferences = new[] { "None", "Loopback", "Acoustic", "Wired", "USB mic" };
            return validReferences.Contains(Reference);
        }

        #endregion
    }

    /// <summary>
    /// Timing offset configuration
    /// </summary>
    public class TimingOffset
    {
        /// <summary>
        /// Timing offset in milliseconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("offset")]
        public double Offset { get; set; } = 0.0;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if offset is reasonable
        /// </summary>
        public bool IsReasonableOffset()
        {
            return Math.Abs(Offset) <= 100.0; // ±100ms is reasonable
        }

        #endregion
    }

    /// <summary>
    /// Playback mode configuration
    /// </summary>
    public class PlaybackMode
    {
        /// <summary>
        /// Selected playback mode
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("mode")]
        public string Mode { get; set; } = "REW sweep";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if playback mode is valid
        /// </summary>
        public bool IsValidMode()
        {
            var validModes = new[] { "REW sweep", "File playback" };
            return validModes.Contains(Mode);
        }

        #endregion
    }

    /// <summary>
    /// File playback stimulus path
    /// </summary>
    public class FilePlaybackStimulus
    {
        /// <summary>
        /// Path to stimulus file
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("path")]
        public string Path { get; set; } = "";
    }

    /// <summary>
    /// Measurement mode configuration
    /// </summary>
    public class MeasurementMode
    {
        /// <summary>
        /// Selected measurement mode
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("mode")]
        public string Mode { get; set; } = "Single";

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if measurement mode is valid
        /// </summary>
        public bool IsValidMode()
        {
            var validModes = new[] { "Single", "Repeated", "Ramped", "Sequential" };
            return validModes.Contains(Mode);
        }

        /// <summary>
        /// Domain behavior: Check if mode requires multiple measurements
        /// </summary>
        public bool IsMultiMeasurementMode()
        {
            return Mode == "Repeated" || Mode == "Ramped" || Mode == "Sequential";
        }

        #endregion
    }

    /// <summary>
    /// Number of repetitions for repeated/ramped measurements
    /// </summary>
    public class MeasurementRepetitions
    {
        /// <summary>
        /// Number of repetitions
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("repetitions")]
        public int Repetitions { get; set; } = 1;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if repetitions count is reasonable
        /// </summary>
        public bool IsReasonableCount()
        {
            return Repetitions >= 1 && Repetitions <= 100;
        }

        #endregion
    }

    /// <summary>
    /// Sequential channel configuration
    /// </summary>
    public class SequentialChannels
    {
        /// <summary>
        /// List of channels to measure sequentially
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("channels")]
        public List<string> Channels { get; set; } = new List<string>();

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if channel list is valid
        /// </summary>
        public bool HasChannels()
        {
            return Channels != null && Channels.Count > 0;
        }

        #endregion
    }

    /// <summary>
    /// Start delay configuration
    /// </summary>
    public class StartDelay
    {
        /// <summary>
        /// Delay in seconds before measurement starts
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("delay")]
        public double Delay { get; set; } = 0.0;

        #region Domain Behavior

        /// <summary>
        /// Domain validation: Check if delay is reasonable
        /// </summary>
        public bool IsReasonableDelay()
        {
            return Delay >= 0.0 && Delay <= 300.0; // Max 5 minutes
        }

        #endregion
    }

    /// <summary>
    /// Invert second output flag
    /// </summary>
    public class InvertSecondOutput
    {
        /// <summary>
        /// Whether to invert the second output
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("invert")]
        public bool Invert { get; set; } = false;
    }

    /// <summary>
    /// Fill silence with dither flag
    /// </summary>
    public class FillSilenceWithDither
    {
        /// <summary>
        /// Whether to fill silence with dither
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fill")]
        public bool Fill { get; set; } = false;
    }

    /// <summary>
    /// Capture noise floor flag
    /// </summary>
    public class CaptureNoiseFloor
    {
        /// <summary>
        /// Whether to capture noise floor before measurement
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("capture")]
        public bool Capture { get; set; } = false;
    }

    #endregion

    #region Stepped Measurement Data Models

    /// <summary>
    /// Frequency span configuration for stepped measurements
    /// </summary>
    public class SteppedFreqSpan
    {
        /// <summary>
        /// Start frequency in Hz
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("startFreq")]
        public double StartFreq { get; set; }

        /// <summary>
        /// End frequency in Hz
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("endFreq")]
        public double EndFreq { get; set; }

        /// <summary>
        /// Points per octave
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("ppo")]
        public int Ppo { get; set; }
    }

    /// <summary>
    /// Level span configuration for stepped measurements
    /// </summary>
    public class SteppedLevelSpan
    {
        /// <summary>
        /// Start level in dBFS
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("startLevel")]
        public double StartLevel { get; set; }

        /// <summary>
        /// End level in dBFS
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("endLevel")]
        public double EndLevel { get; set; }

        /// <summary>
        /// Step size in dB
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("step")]
        public double Step { get; set; }
    }

    /// <summary>
    /// FFT configuration for stepped measurements
    /// </summary>
    public class SteppedFFTConfiguration
    {
        /// <summary>
        /// FFT length (e.g. "16k", "32k", "65k")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("fftLength")]
        public string FftLength { get; set; } = "16k";

        /// <summary>
        /// Number of averages
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("averages")]
        public int Averages { get; set; }

        /// <summary>
        /// Maximum overlap (e.g. "None", "50%", "75%")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("maximumOverlap")]
        public string MaximumOverlap { get; set; } = "None";

        /// <summary>
        /// Window function (e.g. "Hann", "Blackman", "Rectangular")
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("window")]
        public string Window { get; set; } = "Hann";
    }

    /// <summary>
    /// Options for stepped measurements
    /// </summary>
    public class SteppedOptions
    {
        /// <summary>
        /// Silence interval in seconds between measurement points
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("silenceIntervalSeconds")]
        public int SilenceIntervalSeconds { get; set; }

        /// <summary>
        /// Whether to capture spectrum data
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("captureSpectrum")]
        public bool CaptureSpectrum { get; set; }

        /// <summary>
        /// Whether to stop measurement on heavy clipping
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("stopForHeavyClipping")]
        public bool StopForHeavyClipping { get; set; }

        /// <summary>
        /// Whether to stop at distortion limit
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("stopAtDistortionLimit")]
        public bool StopAtDistortionLimit { get; set; }

        /// <summary>
        /// Distortion limit in percent
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("distortionLimitPercent")]
        public double DistortionLimitPercent { get; set; }

        /// <summary>
        /// Whether to reduce step size if distortion limit is hit
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("reduceStepIfDistortionLimitHit")]
        public bool ReduceStepIfDistortionLimitHit { get; set; }
    }

    /// <summary>
    /// Progress information for stepped measurements
    /// </summary>
    public class SteppedProgress
    {
        /// <summary>
        /// Current measurement point
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("point")]
        public int Point { get; set; }

        /// <summary>
        /// Total number of measurement points
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("points")]
        public int Points { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Estimated time remaining in seconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("timeRemainingSeconds")]
        public int TimeRemainingSeconds { get; set; }
    }

    #endregion

    #region Room Simulator Data Models

    /// <summary>
    /// Room size configuration for room simulator
    /// </summary>
    public class RoomSize
    {
        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "metres";

        [System.Text.Json.Serialization.JsonPropertyName("length")]
        public double Length { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("width")]
        public double Width { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("height")]
        public double Height { get; set; }
    }

    /// <summary>
    /// Surface absorption coefficients for room simulator
    /// </summary>
    public class RoomAbsorptions
    {
        [System.Text.Json.Serialization.JsonPropertyName("front")]
        public double Front { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("back")]
        public double Back { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("left")]
        public double Left { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("right")]
        public double Right { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ceiling")]
        public double Ceiling { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("floor")]
        public double Floor { get; set; }
    }

    /// <summary>
    /// Room simulator options configuration
    /// </summary>
    public class RoomSimOptions
    {
        [System.Text.Json.Serialization.JsonPropertyName("showAnechoicResponses")]
        public bool ShowAnechoicResponses { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("timeAlignSpeakersAndSubs")]
        public bool TimeAlignSpeakersAndSubs { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("alignSubsIndividually")]
        public bool AlignSubsIndividually { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("useCrossoverFilter")]
        public bool UseCrossoverFilter { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("crossoverFrequencyHz")]
        public int CrossoverFrequencyHz { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("subsHaveSameDelay")]
        public bool SubsHaveSameDelay { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("subsDelayms")]
        public double SubsDelayms { get; set; }
    }

    /// <summary>
    /// Position in room (head, mic, or source)
    /// </summary>
    public class RoomPosition
    {
        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "metres";

        [System.Text.Json.Serialization.JsonPropertyName("fromRear")]
        public double FromRear { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fromLeft")]
        public double FromLeft { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fromFloor")]
        public double FromFloor { get; set; }
    }

    /// <summary>
    /// Microphone position offsets for room simulator
    /// </summary>
    public class MicPositionOffsets
    {
        [System.Text.Json.Serialization.JsonPropertyName("unit")]
        public string Unit { get; set; } = "metres";

        [System.Text.Json.Serialization.JsonPropertyName("inFront")]
        public double InFront { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("behind")]
        public double Behind { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("left")]
        public double Left { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("right")]
        public double Right { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("above")]
        public double Above { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("below")]
        public double Below { get; set; }
    }

    /// <summary>
    /// Source configuration for room simulator
    /// </summary>
    public class SourceConfiguration
    {
        [System.Text.Json.Serialization.JsonPropertyName("lfMinus3dBHz")]
        public int LfMinus3dBHz { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("enclosureType")]
        public string EnclosureType { get; set; } = "Ported";

        [System.Text.Json.Serialization.JsonPropertyName("invert")]
        public bool Invert { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("delayms")]
        public double Delayms { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("gaindB")]
        public double GaindB { get; set; }
    }

    /// <summary>
    /// Sources list wrapper
    /// </summary>
    public class SourcesList
    {
        [System.Text.Json.Serialization.JsonPropertyName("sources")]
        public List<string> Sources { get; set; } = new();
    }

    #endregion
}
