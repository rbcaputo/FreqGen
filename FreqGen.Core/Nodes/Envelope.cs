using System.Runtime.CompilerServices;

namespace FreqGen.Core.Nodes
{
  /// <summary>
  /// An exponential envelope generator with independent attack and release times.
  /// Crucial for preventing startle responses in therapeutic audio applications.
  /// Uses asymmetric coefficients for natural-sounding fades.
  /// </summary>
  public sealed class Envelope : IAudioNode
  {
    private float _current;
    private float _target;
    private float _attackCoefficient;
    private float _releaseCoefficient;

    /// <summary>
    /// Gets the current envelope value (0.0 to 1.0).
    /// Useful for UI feedback or metering.
    /// </summary>
    public float CurrentValue => _current;

    /// <summary>
    /// Configures the envelope's attack and release characteristics.
    /// Must be called before first use and whenever sample rate changes.
    /// </summary>
    /// <param name="attackSeconds">Time to fade in (typically 10-30 seconds).</param>
    /// <param name="releaseSeconds">Time to fade out (typically 30-60 seconds).</param>
    /// <param name="sampleRate">Current system sample rate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Configure(float attackSeconds, float releaseSeconds, float sampleRate)
    {
      // Exponential coefficient: 1.0 / (time * sampleRate)
      // Larger coefficient = faster fade
      _attackCoefficient =
        1.0f / (MathF.Max(
          AudioSettings.Envelope.MinimumSeconds, attackSeconds) * sampleRate
        );
      _releaseCoefficient =
        1.0f / (MathF.Max(
          AudioSettings.Envelope.MinimumSeconds, releaseSeconds) * sampleRate
        );
    }

    /// <summary>
    /// Triggers the envelope to fade in (true) or fade out (false).
    /// Thread-safe: can be called from UI thread between audio callbacks.
    /// </summary>
    /// <param name="active">True to fade in, false to fade out.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trigger(bool active) =>
      _target = active ? 1.0f : 0.0f;

    /// <summary>
    /// Applies the envelope gain to the provided buffer.
    /// Uses exponential smoothing for natural-sounding fades.
    /// </summary>
    /// <param name="buffer">The buffer to apply envelope to (modified in place).</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Process(Span<float> buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
      {
        // Select coefficient based on fade direction
        float coefficient = _current < _target ? _attackCoefficient : _releaseCoefficient;

        // Exponential smoothing: current += (target - current) * coeff
        _current += (_target - _current) * coefficient;

        // Apply envelope as multiplicative gain
        buffer[i] *= _current;
      }
    }

    /// <summary>
    /// Resets the envelope state to zero.
    /// Should be called when audio engine is stopped to prevent clicks on restart.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
      _current = 0.0f;
      _target = 0.0f;
    }
  }
}
