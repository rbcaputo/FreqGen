namespace FreqGen.Core.Nodes.Oscillators
{
  /// <summary>
  /// Pure sine wave oscillator.
  /// Thread-safe for parameters updates, real-time safe for audio generation.
  /// </summary>
  public sealed class SineOscillator : IAudioNode
  {
    private float _phase;
    private float _phaseIncrement;

    /// <summary>
    /// Set the oscillator frequency.
    /// Safe to call from any thread.
    /// </summary>
    public void SetFrequency(float frequency, float sampleRate) =>
      _phaseIncrement = MathF.Tau * frequency / sampleRate;

    /// <summary>
    /// Generate the next sample.
    /// Must be called from audio thread only.
    /// </summary>
    public float NextSample()
    {
      float sample = MathF.Sin(_phase);
      _phase += _phaseIncrement;

      // Wrap phase
      if (_phase >= MathF.Tau)
        _phase -= MathF.Tau;
      else if (_phase < 0f)
        _phase += MathF.Tau;

      return sample;
    }

    /// <summary>
    /// Reset phase to zero.
    /// </summary>
    public void Reset() =>
      _phase = 0f;
  }
}
