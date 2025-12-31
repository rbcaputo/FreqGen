namespace FreqGen.Core.Nodes.Oscillators
{
  /// <summary>
  /// Square wave oscillator for true isochronic tones.
  /// Produces hard on/off pulses suitable for rhythmic entrainment.
  /// </summary>
  public sealed class SquareOscillator : IAudioNode
  {
    private float _phase;
    private float _phaseIncrement;
    private float _dutyCycle = 0.5f; // 50% duty cycle by default

    /// <summary>
    /// Duty cycle: ratio of "on" time to total period (0.0-1.0).
    /// 0.5 = symmetric square wave.
    /// </summary>
    public float DutyCycle
    {
      get => _dutyCycle;
      set => Math.Clamp(value, 0.1f, 0.9f);
    }

    /// <summary>
    /// Set the oscillator frequency.
    /// Safe to call from any thread.
    /// </summary>
    public void SetFrequency(float frequency, float sampleRate) =>
      _phaseIncrement = frequency / sampleRate;


    /// <summary>
    /// Generate the next sample.
    /// Must be called from audio thread only.
    /// </summary>
    public float NextSample()
    {
      // Normalize phase to [0, 1]
      float normalizedPhase = _phase - MathF.Floor(_phase);
      // Square wave: +1 when phase < duty cycle, -1 otherwise
      float sample = normalizedPhase < _dutyCycle ? 1f : -1f;
      _phase += _phaseIncrement;

      // Wrap phase
      if (_phase >= 1f)
        _phase -= 1f;
      else if (_phase < 0f)
        _phase += 1f;

      return sample;
    }

    /// <summary>
    /// Reset phase to zero.
    /// </summary>
    public void Reset() =>
      _phase = 0f;
  }
}
