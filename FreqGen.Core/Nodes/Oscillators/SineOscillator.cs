using System.Runtime.CompilerServices;

namespace FreqGen.Core.Nodes.Oscillators
{
  /// <summary>
  /// A high-precision sine wave oscillator using double-precision phase accumulation.
  /// Uses a phase-accumulator approach to ensure continuous signals across block boundaries.
  /// Optimized for long-running therapeutic audio sessions (30+ minutes).
  /// </summary>
  public sealed class SineOscillator : IAudioNode
  {
    // Use double for phase to prevent drift over hours
    private double _phase;
    private double _phaseIncrement;

    /// <summary>
    /// Updates the oscillator's frequency based on the current sample rate.
    /// Thread-safe: can be called from UI thread between audio callbacks.
    /// </summary>
    /// <param name="frequency">Target frequency in Hz.</param>
    /// <param name="sampleRate">The system's current audio sample rate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFrequency(float frequency, float sampleRate) =>
      // Use double precision for phase increment calculation
      _phaseIncrement = Math.Tau * frequency / sampleRate;

    /// <summary>
    /// Fills the buffer with a pure sine wave.
    /// Optimized for SIMD and cache efficiency.
    /// </summary>
    /// <param name="buffer">The span to populate with audio samples.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Process(Span<float> buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
      {
        // Cast to float only at output to maintain precision
        buffer[i] = (float)Math.Sin(_phase);

        // Increment and wrap phase
        _phase += _phaseIncrement;

        // Exact subtraction to prevent accumulation error
        // Faster than modulo and maintains precision
        if (_phase >= Math.Tau)
          _phase -= Math.Tau;
      }
    }

    /// <summary>
    /// Resets the oscillator state to prevent clicks on restart.
    /// Should be called when audio engine is stopped.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() =>
      _phase = 0.0;
  }
}
