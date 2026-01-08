using System.Runtime.CompilerServices;
using ToneSync.Core.Nodes;
using ToneSync.Core.Nodes.Modulators;
using ToneSync.Core.Nodes.Oscillators;

namespace ToneSync.Core.Layers
{
  /// <summary>
  /// Orchestrates a single signal path:
  /// Oscillator → (optional) AM Modulation → Envelope → Layer Weight.
  /// 
  /// This class guarantees allocation-free,
  /// deterministic DSP suitable for real-time audio processing.
  /// 
  /// All internal state is owned by the audio thread.
  /// Configuration is provided as immutable data per render call,
  /// ensuring lock-free, deterministic execution.
  /// </summary>
  public sealed class MonoLayer
  {
    /// <summary>
    /// Fixed attenuation applied before amplitude modulation
    /// to prevent peak energy increase at full modulation depth.
    /// 
    /// A value of 0.5 preserves unity peak when modulator reaches +1.
    /// </summary>
    private const float AmHeadroom = 0.5f;

    /// <summary>
    /// Primary audio oscillator responsible for generating the carrier waveform.
    /// </summary>
    private readonly SineOscillator _carrier = new();

    /// <summary>
    /// Low-frequency oscillator used for amplitude modulation.
    /// </summary>
    private readonly LFO _lfo = new();

    /// <summary>
    /// Envelope generator controlling the amplitude evolution of the layer.
    /// </summary>
    private readonly Envelope _envelope = new();

    /// <summary>
    /// Temporary buffer used to store modulation signal samples before they
    /// are applied to the carrier.
    /// This buffer may be resized during initialization but is
    /// never allocated during steady-state audio processing.
    /// </summary>
    private float[] _modulatorBuffer = new float[AudioSettings.MaxBufferSize];

    private bool _isInitialized;

    /// <summary>
    /// Gets the current envelope value for this layer.
    /// Useful for UI feedback or metering.
    /// </summary>
    /// <remarks>
    /// This value is updated during audio processing and should be treated as
    /// an instantaneous snapshot suitable for visualization only.
    /// </remarks>
    public float CurrentEnvelopeValue => _envelope.CurrentValue;

    /// <summary>
    /// Initializes the layer with fixed buffer sizes.
    /// Must be called once before first use.
    /// </summary>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="attackSeconds">Envelope attack time.</param>
    /// <param name="releaseSeconds">Envelope release time.</param>
    /// <remarks>
    /// This method must be called exactly once before the layer is used.
    /// It is not thread-safe and must not be called while audio processing is active.
    /// </remarks>
    public void Initialize(
      float sampleRate,
      float attackSeconds,
      float releaseSeconds
    )
    {
      _envelope.Configure(attackSeconds, releaseSeconds, sampleRate);
      _isInitialized = true;
    }

    /// <summary>
    /// Updates internal DSP state and renders this layer into the
    /// provided buffer using the supplied configuration.
    /// </summary>
    /// <param name="buffer">
    /// Destination buffer to receive the generated audio samples.
    /// The buffer is always fully written.
    /// </param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="config">
    /// Immutable configuration describing oscillator, modulation, and weighting.
    /// </param>
    /// <remarks>
    /// This method is part of the real-time audio path:
    /// - No locks
    /// - No allocations during steady-state operation
    /// - Deterministic execution time
    ///
    /// If the layer is inactive, the buffer is cleared and no DSP work is performed.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void UpdateAndProcess(
      Span<float> buffer,
      float sampleRate,
      LayerConfiguration config
    )
    {
      // Early exit (saves CPU)
      if (!_isInitialized || config is null || !config.IsActive)
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

      // Reserve headroom for amplitude modulation
      ApplyPreModulationHeadroom(buffer);

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
    /// Applies fixed attenuation before amplitude modulation
    /// to ensure AM does not increase peak signal energy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyPreModulationHeadroom(Span<float> buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
        buffer[i] *= AmHeadroom;
    }

    /// <summary>
    /// Applies per-layer gain scaling.
    /// Weight values above 1.0 are allowed but discouraged,
    /// as they reduce available mix headroom.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyWeight(Span<float> buffer, float weight)
    {
      if (weight <= 0f)
      {
        buffer.Clear();
        return;
      }

      if (weight == 1.0f)
        return; // Skip multiplication if no scaling needed

      for (int i = 0; i < buffer.Length; i++)
        buffer[i] *= weight;
    }

    /// <summary>
    /// Triggers the layer's envelope to fade out.
    /// Should be called when stopping playback.
    /// </summary>
    /// <remarks>
    /// This does not silence the layer immediately.
    /// The envelope will enter its release phase and decay according to
    /// its configured release time.
    /// </remarks>
    public void TriggerRelease() =>
      _envelope.Trigger(false);

    /// <summary>
    /// Resets the layer state to prevent clicks on restart.
    /// Should be called when audio engine is stopped.
    /// </summary>
    /// <remarks>
    /// This method clears all oscillator phase and envelope state.
    /// It should only be called when the audio engine is fully stopped,
    /// as it may introduce discontinuities if used during playback.
    /// </remarks>
    public void Reset()
    {
      _carrier.Reset();
      _lfo.Reset();
      _envelope.Reset();
    }
  }
}
