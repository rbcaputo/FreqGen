namespace FreqGen.Core
{
  /// <summary>
  /// Global audio configuration constants.
  /// </summary>
  public sealed record AudioSettings
  {
    /// <summary>
    /// Default sample rate in Hz.
    /// 44100 Hz is CD quality and widely supported.
    /// </summary>
    public const int SampleRate = 44100;

    /// <summary>
    /// Aufio buffer size in samples.
    /// Larger = more latency but more efficient.
    /// 1024 samples @ 44.1 kHz = ~23ms latency.
    /// </summary>
    public const int BufferSize = 1024;

    /// <summary>
    /// Minimum audible frequency in Hz.
    /// Below this, sound is felt rather than heard.
    /// </summary>
    public const float MinFrequency = 20f;

    /// <summary>
    /// Maximum safe frequency (Nyquist frequency).
    /// Must be less than SampleRate / 2 to avoid aliasing.
    /// </summary>
    public const float MaxFrequency = SampleRate / 2f;

    /// <summary>
    /// Typical carrier frequency range for therapy audio.
    /// </summary>
    public sealed record CarrierRange
    {
      public const float Min = 100f;  // Lower bound for pleasant tones
      public const float Max = 1000f; // Upper bound for sustained listening
      public const float Default = 200f;
    }

    /// <summary>
    /// Modeulation frequency ranges for different brainwave bands.
    /// </summary>
    public sealed record ModulationRange
    {
      public const float DeltaMin = 0.5f;
      public const float DeltaMax = 4f;

      public const float ThetaMin = 4f;
      public const float ThetaMax = 8f;

      public const float AlphaMin = 8f;
      public const float AlphaMax = 13f;

      public const float BetaMin = 13f;
      public const float BetaMax = 30f;

      public const float GammaMin = 30f;
      public const float GammaMax = 100f;
    }

    /// <summary>
    /// Recommended envelope times for therapy sessions.
    /// </summary>
    public sealed record EnvelopeTimes
    {
      public const float FastAttack = 5f;    // 5 seconds
      public const float NormalAttack = 30f; // 30 seconds (default)
      public const float SlowAttack = 60f;   // 60 seconds

      public const float FastRelease = 5f;
      public const float NormalRelease = 30f;
      public const float SlowRelease = 60f;
    }
  }
}
