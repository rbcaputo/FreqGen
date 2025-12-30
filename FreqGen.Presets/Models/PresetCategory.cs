namespace FreqGen.Presets.Models
{
  /// <summary>
  /// Categories of frequency presets.
  /// </summary>
  public enum PresetCategory
  {
    /// <summary>
    /// Brainwave entrainment frequencies (Delta, Theta, Alpha, Beta, Gamma).
    /// </summary>
    Brainwave,

    /// <summary>
    /// Solfeggio frequencies (396, 417, 528, 639, 741, 852 Hz)
    /// </summary>
    Solfeggio,

    /// <summary>
    /// Isochronic tones with rhythmic pulsing.
    /// </summary>
    Isochronic,

    /// <summary>
    /// Binaural beats (stereo only).
    /// </summary>
    Binaural,

    /// <summary>
    /// User-defined or experimental presets.
    /// </summary>
    Custom
  }
}
