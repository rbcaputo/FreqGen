namespace ToneSync.Core
{
  /// <summary>
  /// Provides global constants and default values for the FreqGen audio system.
  /// All values are validated against hardware and psychoacoustic constraints.
  /// </summary>
  public static class AudioSettings
  {
    /// <summary>
    /// Default native mobile DAC sample rate (48kHz).
    /// </summary>
    public const int SampleRate = 48000;

    /// <summary>
    /// Recommended buffer size for mobile audio stability.
    /// Actual size may vary per platform audio callback.
    /// </summary>
    public const int RecommendedBufferSize = 1024;

    /// <summary>
    /// Maximum buffer size to pre-allocate for (safety margin).
    /// </summary>
    public const int MaxBufferSize = 4096;

    /// <summary>
    /// Maximum number of simultaneous audio layers.
    /// Pre-allocated to avoid runtime allocations.
    /// </summary>
    public const int MaxLayers = 8;

    /// <summary>
    /// Constraints for Carrier frequencies.
    /// </summary>
    public static class CarrierSettings
    {
      /// <summary>
      /// Minimum audible carrier frequency (20Hz).
      /// </summary>
      public const float Minimum = 20.0f;

      /// <summary>
      /// Maximum safe carrier frequency (2kHz).
      /// Ensures safe distance from Nyquist frequency at 44.1 kHz.
      /// </summary>
      public const float Maximum = 2000.0f;

      /// <summary>
      /// Default carrier frequency (A440 concert pitch).
      /// </summary>
      public const float Default = 440.0f;

      /// <summary>
      /// Validates a carrier frequency against hardware limits.
      /// </summary>
      /// <param name="frequency">Frequency in Hz.</param>
      /// <param name="sampleRate">Current system sample rate.</param>
      /// <returns>True if valid, false otherwise.</returns>
      public static bool IsValid(float frequency, float sampleRate)
      {
        if (float.IsNaN(frequency) || float.IsInfinity(frequency))
          return false;

        if (frequency < Minimum || frequency > Maximum)
          return false;

        // Nyquist safety: carrier must be below 45% of sample rate
        return frequency < sampleRate * 0.45f;
      }
    }

    /// <summary>
    /// Constraints for Modulation (LFO) frequencies.
    /// </summary>
    public static class ModulationSettings
    {
      /// <summary>
      /// Minimum modulation frequency (0.1Hz).
      /// </summary>
      public const float Minimum = 0.1f;

      /// <summary>
      /// Maximum modulation frequency (100Hz).
      /// </summary>
      public const float Maximum = 100.0f;

      /// <summary>
      /// Validates a modulation frequency.
      /// </summary>
      public static bool IsValid(float frequency)
      {
        if (float.IsNaN(frequency) || float.IsInfinity(frequency))
          return false;

        return frequency >= Minimum && frequency <= Maximum;
      }
    }

    /// <summary>
    /// Constraints for modulation depth and layer weights.
    /// </summary>
    public static class AmplitudeSettings
    {
      /// <summary>
      /// Minimum depth/weight (0.0 = off).
      /// </summary>
      public const float Minimum = 0.0f;

      /// <summary>
      /// Maximum depth/weight (1.0 = full).
      /// </summary>
      public const float Maximum = 1.0f;

      /// <summary>
      /// Validates an amplitude value (depth, weight, gain).
      /// </summary>
      public static bool IsValid(float value)
      {
        if (float.IsNaN(value) || float.IsInfinity(value))
          return false;

        return value >= Minimum && value <= Maximum;
      }
    }

    /// <summary>
    /// Constraints for smooth signal transitions.
    /// </summary>
    public static class EnvelopeSettings
    {
      /// <summary>
      /// Default attack time (10 seconds).
      /// Prevents startle response in therapeutic applications.
      /// </summary>
      public const float DefaultAttackSeconds = 10.0f;

      /// <summary>
      /// Default release time (30 seconds).
      /// Extended fade-out for safe session termination.
      /// </summary>
      public const float DefaultReleaseSeconds = 30.0f;

      /// <summary>
      /// Minimum envelope time to prevent division by zero.
      /// </summary>
      public const float MinimumSeconds = 0.1f;

      /// <summary>
      /// Validates an envelope time parameter.
      /// </summary>
      public static bool IsValid(float timeInSeconds)
      {
        if (float.IsNaN(timeInSeconds) || float.IsInfinity(timeInSeconds))
          return false;

        return timeInSeconds >= MinimumSeconds;
      }
    }

    /// <summary>
    /// Channel configuration settings.
    /// </summary>
    public static class ChannelSettings
    {
      /// <summary>
      /// Default channel mode.
      /// </summary>
      public const ChannelMode Default = ChannelMode.Mono;

      /// <summary>
      /// Gets the number of channels for a given mode.
      /// </summary>
      public static int GetChannelCount(ChannelMode mode) => (int)mode;
    }
  }
}
