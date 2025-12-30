namespace FreqGen.Core.Nodes.Modulators
{
  /// <summary>
  /// Low Frequency Oscillator for control-rate modulation.
  /// Optimized to update at control rate rather than audio rate.
  /// </summary>
  public sealed class LFO : IAudioNode
  {
    private const float TwoPI = MathF.PI * 2f;
    private const int UpdateInterval = 64; // Control rate: update every 64 samples

    private float _phase;
    private float _phaseIncrement;
    private float _cachedSample;
    private int _updateCounter;

    /// <summary>
    /// Set the LFO frequency (typically 0.5-30 Hz)
    /// </summary>
    public void SetFrequency(float frequency, float sampleRate) =>
      _phaseIncrement = TwoPI * frequency / sampleRate;

    /// <summary>
    /// Get next modulation value.
    /// Returns cached value most of the time for CPU efficiency.
    /// </summary>
    public float NextSample()
    {
      if (++_updateCounter >= UpdateInterval)
      {
        _updateCounter = 0;
        _cachedSample = MathF.Sin(_phase);

        _phase += _phaseIncrement * UpdateInterval;
        if (_phase >= TwoPI)
          _phase -= TwoPI;
      }

      return _cachedSample;
    }

    /// <summary>
    /// Reset phase and cache.
    /// </summary>
    public void Reset()
    {
      _phase = 0f;
      _cachedSample = 0f;
      _updateCounter = 0;
    }
  }
}
