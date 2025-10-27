using System.Text.Json.Serialization;

namespace REWApi.Domain;

/// <summary>
/// Signal-specific configuration classes for REW Generator
/// Each signal type requires its own configuration schema per REW API documentation
/// Source: http://localhost:4735/
/// </summary>

#region Noise Signal Configurations

/// <summary>
/// Configuration for Random Noise signals (Pink, White, etc.)
/// Used for: pinknoise, whitenoise
/// </summary>
public class RandomNoiseConfiguration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("octaveCentre")]
    public double? OctaveCentre { get; set; }

    [JsonPropertyName("thirdOctaveCentre")]
    public double? ThirdOctaveCentre { get; set; }

    [JsonPropertyName("customLowCut")]
    public bool CustomLowCut { get; set; }

    [JsonPropertyName("customLowCutFreq")]
    public int? CustomLowCutFreq { get; set; }

    [JsonPropertyName("customHighCut")]
    public bool CustomHighCut { get; set; }

    [JsonPropertyName("customHighCutFreq")]
    public int? CustomHighCutFreq { get; set; }

    [JsonPropertyName("customFilterOrder")]
    public int? CustomFilterOrder { get; set; }

    [JsonPropertyName("gaussian")]
    public bool Gaussian { get; set; }
}

/// <summary>
/// Configuration for Periodic Noise signals
/// Used for: pinkpn, whitepn
/// </summary>
public class PeriodicNoiseConfiguration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("octaveCentre")]
    public double? OctaveCentre { get; set; }

    [JsonPropertyName("thirdOctaveCentre")]
    public double? ThirdOctaveCentre { get; set; }

    [JsonPropertyName("customLowCut")]
    public bool CustomLowCut { get; set; }

    [JsonPropertyName("customLowCutFreq")]
    public int? CustomLowCutFreq { get; set; }

    [JsonPropertyName("customHighCut")]
    public bool CustomHighCut { get; set; }

    [JsonPropertyName("customHighCutFreq")]
    public int? CustomHighCutFreq { get; set; }

    [JsonPropertyName("customFilterOrder")]
    public int? CustomFilterOrder { get; set; }

    [JsonPropertyName("minimiseCrestFactor")]
    public bool MinimiseCrestFactor { get; set; }

    [JsonPropertyName("sequenceLength")]
    public string? SequenceLength { get; set; }
}

#endregion

#region Tone Signal Configurations

/// <summary>
/// Configuration for Sine wave signal
/// </summary>
public class SineConfiguration
{
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("lockFrequencyToRTAFFT")]
    public bool LockFrequencyToRTAFFT { get; set; }

    [JsonPropertyName("addHarmonicDistortion")]
    public bool AddHarmonicDistortion { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Harmonic distortion settings for sine wave
/// </summary>
public class SineDistortion
{
    [JsonPropertyName("addH2")]
    public bool AddH2 { get; set; }

    [JsonPropertyName("dBH2")]
    public double DBH2 { get; set; }

    [JsonPropertyName("degreesH2")]
    public double DegreesH2 { get; set; }

    [JsonPropertyName("addH3")]
    public bool AddH3 { get; set; }

    [JsonPropertyName("dBH3")]
    public double DBH3 { get; set; }

    [JsonPropertyName("degreesH3")]
    public double DegreesH3 { get; set; }

    [JsonPropertyName("addH4")]
    public bool AddH4 { get; set; }

    [JsonPropertyName("dBH4")]
    public double DBH4 { get; set; }

    [JsonPropertyName("degreesH4")]
    public double DegreesH4 { get; set; }

    [JsonPropertyName("addH5")]
    public bool AddH5 { get; set; }

    [JsonPropertyName("dBH5")]
    public double DBH5 { get; set; }

    [JsonPropertyName("degreesH5")]
    public double DegreesH5 { get; set; }

    [JsonPropertyName("addH6")]
    public bool AddH6 { get; set; }

    [JsonPropertyName("dBH6")]
    public double DBH6 { get; set; }

    [JsonPropertyName("degreesH6")]
    public double DegreesH6 { get; set; }

    [JsonPropertyName("addH7")]
    public bool AddH7 { get; set; }

    [JsonPropertyName("dBH7")]
    public double DBH7 { get; set; }

    [JsonPropertyName("degreesH7")]
    public double DegreesH7 { get; set; }

    [JsonPropertyName("addH8")]
    public bool AddH8 { get; set; }

    [JsonPropertyName("dBH8")]
    public double DBH8 { get; set; }

    [JsonPropertyName("degreesH8")]
    public double DegreesH8 { get; set; }

    [JsonPropertyName("addH9")]
    public bool AddH9 { get; set; }

    [JsonPropertyName("dBH9")]
    public double DBH9 { get; set; }

    [JsonPropertyName("degreesH9")]
    public double DegreesH9 { get; set; }
}

/// <summary>
/// Configuration for Square wave signal
/// </summary>
public class SquareConfiguration
{
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("dutyCyclePercent")]
    public int DutyCyclePercent { get; set; }

    [JsonPropertyName("bandLimit")]
    public bool BandLimit { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Configuration for Sawtooth wave signal
/// </summary>
public class SawtoothConfiguration
{
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("bandLimit")]
    public bool BandLimit { get; set; }

    [JsonPropertyName("invert")]
    public bool Invert { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

#endregion

#region Burst Signal Configurations

/// <summary>
/// Configuration for Tone Burst signal
/// </summary>
public class ToneBurstConfiguration
{
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("window")]
    public string Window { get; set; } = "";

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; }

    [JsonPropertyName("windowWidthUnit")]
    public string WindowWidthUnit { get; set; } = "";

    [JsonPropertyName("period")]
    public int Period { get; set; }

    [JsonPropertyName("periodUnit")]
    public string PeriodUnit { get; set; } = "";

    [JsonPropertyName("repeat")]
    public bool Repeat { get; set; }
}

/// <summary>
/// Configuration for CEA Burst signal
/// </summary>
public class CEABurstConfiguration
{
    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("repeat")]
    public bool Repeat { get; set; }
}

#endregion

#region Multi-Tone Signal Configurations

/// <summary>
/// Configuration for Two-Tone signal (dualtone)
/// </summary>
public class TwoToneConfiguration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("customF1")]
    public int? CustomF1 { get; set; }

    [JsonPropertyName("customF2")]
    public int? CustomF2 { get; set; }

    [JsonPropertyName("customRatio")]
    public int? CustomRatio { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Configuration for Three-Tone signal (tripletone)
/// </summary>
public class ThreeToneConfiguration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("customF1")]
    public int? CustomF1 { get; set; }

    [JsonPropertyName("customF2")]
    public int? CustomF2 { get; set; }

    [JsonPropertyName("customF3")]
    public int? CustomF3 { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Configuration for Quadtone (4-tone) signal
/// </summary>
public class QuadtoneConfiguration
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("customF1")]
    public int? CustomF1 { get; set; }

    [JsonPropertyName("customF2")]
    public int? CustomF2 { get; set; }

    [JsonPropertyName("customRatio")]
    public int? CustomRatio { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Configuration for Multitone signal
/// </summary>
public class MultitoneConfiguration
{
    [JsonPropertyName("startFreq")]
    public int StartFreq { get; set; }

    [JsonPropertyName("endFreq")]
    public int EndFreq { get; set; }

    [JsonPropertyName("sequenceLength")]
    public string SequenceLength { get; set; } = "";

    [JsonPropertyName("spectrum")]
    public string Spectrum { get; set; } = "";

    [JsonPropertyName("spacing")]
    public string Spacing { get; set; } = "";

    [JsonPropertyName("linearSpacingHz")]
    public int? LinearSpacingHz { get; set; }

    [JsonPropertyName("octaveSpacing")]
    public string? OctaveSpacing { get; set; }

    [JsonPropertyName("decadeSpacing")]
    public string? DecadeSpacing { get; set; }

    [JsonPropertyName("minimiseCrestFactor")]
    public bool MinimiseCrestFactor { get; set; }

    [JsonPropertyName("addDither")]
    public bool AddDither { get; set; }

    [JsonPropertyName("ditherBits")]
    public int DitherBits { get; set; }
}

/// <summary>
/// Details about generated multitone signal
/// </summary>
public class MultitoneDetails
{
    [JsonPropertyName("firstFreq")]
    public double FirstFreq { get; set; }

    [JsonPropertyName("lastFreq")]
    public double LastFreq { get; set; }

    [JsonPropertyName("numTones")]
    public int NumTones { get; set; }

    [JsonPropertyName("crestFactordB")]
    public double CrestFactorDB { get; set; }

    [JsonPropertyName("kurtosis")]
    public double Kurtosis { get; set; }
}

#endregion

#region Sweep Signal Configurations

/// <summary>
/// Configuration for Linear Sweep signal (linearsweep)
/// </summary>
public class LinearSweepConfiguration
{
    [JsonPropertyName("startFrequency")]
    public int StartFrequency { get; set; }

    [JsonPropertyName("endFrequency")]
    public int EndFrequency { get; set; }

    [JsonPropertyName("fadeInMilliseconds")]
    public int FadeInMilliseconds { get; set; }

    [JsonPropertyName("fadeOutMilliseconds")]
    public int FadeOutMilliseconds { get; set; }

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("loop")]
    public bool Loop { get; set; }
}

/// <summary>
/// Configuration for Logarithmic Sweep signal (logsweep)
/// </summary>
public class LogSweepConfiguration
{
    [JsonPropertyName("startFrequency")]
    public int StartFrequency { get; set; }

    [JsonPropertyName("endFrequency")]
    public int EndFrequency { get; set; }

    [JsonPropertyName("fadeInFraction")]
    public string FadeInFraction { get; set; } = "";

    [JsonPropertyName("fadeOutFraction")]
    public string FadeOutFraction { get; set; } = "";

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("loop")]
    public bool Loop { get; set; }
}

/// <summary>
/// Configuration for Measurement Sweep signal (meassweep)
/// </summary>
public class MeasSweepConfiguration
{
    [JsonPropertyName("startFrequency")]
    public int StartFrequency { get; set; }

    [JsonPropertyName("endFrequency")]
    public int EndFrequency { get; set; }

    [JsonPropertyName("length")]
    public string Length { get; set; } = "";

    [JsonPropertyName("fillSilenceWithDither")]
    public bool FillSilenceWithDither { get; set; }
}

#endregion
