namespace FreqGen.Core.Nodes
{
  /// <summary>
  /// Exponential envelope generator for smooth amplitude transitions.
  /// Uses exponential curves for natural-sounding fades.
  /// </summary>
  public sealed class Envelope
  {
    private float _current;
    private float _attackCoef;
    private float _releaseCoef;
    private bool _isActive;

    /// <summary>
    /// Current envelope value.
    /// </summary>
    public float Current => _current;

    /// <summary>
    /// Check if envelope has effectively reached target (within 0.1%)
    /// </summary>
    public bool IsSettled => _isActive ? _current > 0.999f : _current < 0.001f;

    /// <summary>
    /// Set attack time (fade-in duration).
    /// </summary>
    /// <param name="seconds">Attack time in seconds (typically 10-60s for therapy audio)</param>
    /// <param name="sampleRate">Audio sample rate</param>
    public void SetAttackTime(float seconds, float sampleRate) =>
      // Exponential coefficient: reaches ~99.3% of target after 'seconds'
      _attackCoef = 1f - MathF.Exp(-1f / (seconds * sampleRate));

    /// <summary>
    /// Set release time (fade-out duration).
    /// </summary>
    /// <param name="seconds">Release time in seconds</param>
    /// <param name="sampleRate">Audio sample rate</param>
    public void SetReleaseTime(float seconds, float sampleRate) =>
      _releaseCoef = 1f - MathF.Exp(-1f / (seconds * sampleRate));

    /// <summary>
    /// Trigger envelope on or off.
    /// </summary>
    /// <param name="on">True to start attack phase, false to start release phase</param>
    public void Trigger(bool on) =>
      _isActive = on;

    /// <summary>
    /// Get next envelope value.
    /// </summary>
    /// <returns>Envelope amplitude [0.0, 1.0]</returns>
    public float NextSampkle()
    {
      float target = _isActive ? 1f : 0f;
      float coef = _isActive ? _attackCoef : _releaseCoef;

      _current += (target - _current) * coef;

      return _current;
    }

    /// <summary>
    /// Reset envelope to zero.
    /// </summary>
    public void Reset()
    {
      _current = 0f;
      _isActive = false;
    }
  }
}
