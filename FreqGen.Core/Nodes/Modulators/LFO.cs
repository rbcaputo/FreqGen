using System.Runtime.CompilerServices;

namespace FreqGen.Core.Nodes.Modulators
{
  /// <summary>
  /// Low-Frequency Oscillator optimized for modulation.
  /// Updates at a control rate (every 64 samples) to conserve CPU on mobile devices.
  /// Double-precision phase accumulation prevents drift in long sessions.
  /// </summary>
  public sealed class LFO : IAudioNode
  {
    /// <summary>
    /// Control rate decimation factor.
    /// LFO updates once per this many samples to save battery.
    /// </summary>
    private const int ControlRate = 16;

    private double _phase;
    private double _phaseIncrement;

    private float _currentValue;
    private float _nextValue;

    private int _sampleCounter;
    private int _interpCounter;

    /// <summary>
    /// Set the LFO frequency (typically 0.5-30Hz).
    /// Thread-safe: can be called from UI thread between audio callbacks.
    /// </summary>
    /// <param name="frequency">Modulation frequency in Hz.</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFrequency(float frequency, float sampleRate) =>
      // Compensate increment for the reduced update rate
      // Use double precision to prevent drift
      _phaseIncrement = Math.Tau * frequency / (sampleRate / ControlRate);

    /// <summary>
    /// Populates the buffer with a stepped LFO signal for modulation.
    /// Output range is [-1.0, 1.0] for bipolar modulation.
    /// </summary>
    /// <param name="buffer">The span to populate with LFO samples.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Process(Span<float> buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
      {
        // Update at control rate only
        if (_sampleCounter++ % ControlRate == 0)
        {
          _currentValue = _nextValue;
          _nextValue = (float)Math.Sin(_phase);

          _interpCounter = 0;

          // Increment and wrap phase
          _phase += _phaseIncrement;
          if (_phase >= Math.Tau)
            _phase -= Math.Tau;
        }

        float t = (float)_interpCounter / ControlRate;
        buffer[i] = _currentValue + t * (_nextValue - _currentValue);
        _interpCounter++;
      }
    }

    /// <summary>
    /// Resets the oscillator state to prevent clicks on restart.
    /// Should be called when audio engine is stopped.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
      _phase = 0.0;
      _currentValue = 0.0f;
      _nextValue = 0.0f;
      _sampleCounter = 0;
      _interpCounter = 0;
    }
  }
}
