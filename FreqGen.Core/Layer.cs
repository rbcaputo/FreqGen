using FreqGen.Core.Nodes;
using FreqGen.Core.Nodes.Modulators;
using FreqGen.Core.Nodes.Oscillators;
using System.Runtime.CompilerServices;

namespace FreqGen.Core
{
  /// <summary>
  /// Orchestrates a single signal path: Oscillator → Modulator → Envelope → Output.
  /// Pre-allocates all buffers to avoid runtime allocations in audio callback.
  /// Thread-safe: configuration changes are atomic via immutable records.
  /// </summary>
  public sealed class Layer
  {
    private readonly SineOscillator _carrier = new();
    private readonly LFO _lfo = new();
    private readonly Envelope _envelope = new();

    // Pre-allocated modulator buffer (sized for max expected buffer)
    private float[] _modulatorBuffer = new float[AudioSettings.MaxBufferSize];

    /// <summary>
    /// Gets the current envelope value for this layer.
    /// Useful for UI feedback or metering.
    /// </summary>
    public float CurrentEnvelopeValue => _envelope.CurrentValue;

    /// <summary>
    /// Initializes the layer with fixed buffer sizes.
    /// Must be called once before first use.
    /// </summary>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="attackSeconds">Envelope attack time.</param>
    /// <param name="releaseSeconds">Envelope release time.</param>
    public void Initialize(
      float sampleRate,
      float attackSeconds,
      float releaseSeconds
    ) => _envelope.Configure(attackSeconds, releaseSeconds, sampleRate);

    /// <summary>
    /// Processes the layer logic into the provided buffer based on a configuration.
    /// This is the HOT PATH - must be allocation-free and deterministic.
    /// </summary>
    /// <param name="buffer">The buffer to populate with audio samples.</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="config">The configuration for this layer.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void UpdateAndProcess(Span<float> buffer, float sampleRate, LayerConfiguration config)
    {
      // Early exit if layer is inactive (saves CPU)
      if (!config.IsActive)
      {
        buffer.Clear();
        return;
      }

      // Update oscillator frequencies
      _carrier.SetFrequency(config.CarrierFrequency, sampleRate);
      _lfo.SetFrequency(config.ModulatorFrequency, sampleRate);
      _envelope.Trigger(true); // Layer is active, envelope should be up

      // Resize modulator buffer if needed (should only happen once at startup)
      if (_modulatorBuffer.Length < buffer.Length)
        _modulatorBuffer = new float[buffer.Length];

      // Generate carrier signal
      _carrier.Process(buffer);

      // Generate modulator signal (if modulation is enabled)
      if (config.ModulatorFrequency > 0.0f && config.ModulatorDepth > 0.0f)
      {
        Span<float> modulatorSpan = _modulatorBuffer.AsSpan(0, buffer.Length);
        _lfo.Process(modulatorSpan);

        // Apply amplitude modulation
        AMModulator.Apply(
          buffer,
          modulatorSpan,
          config.ModulatorDepth
        );
      }

      // Apply envelope
      _envelope.Process(buffer);

      // Apply layer weight
      ApplyWeight(buffer, config.Weight);
    }

    /// <summary>
    /// Applies weight scaling to the buffer.
    /// Inlined for performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyWeight(Span<float> buffer, float weight)
    {
      if (weight == 1.0f)
        return; // Skip multiplication if no scaling needed

      for (int i = 0; i < buffer.Length; i++)
        buffer[i] *= weight;
    }

    /// <summary>
    /// Triggers the layer's envelope to fade out.
    /// Should be called when stopping playback.
    /// </summary>
    public void TriggerRelease() =>
      _envelope.Trigger(false);

    /// <summary>
    /// Resets the layer state to prevent clicks on restart.
    /// Should be called when audio engine is stopped.
    /// </summary>
    public void Reset()
    {
      _carrier.Reset();
      _lfo.Reset();
      _envelope.Reset();
    }
  }
}
