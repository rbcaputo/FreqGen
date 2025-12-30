using FreqGen.Core.Nodes;
using FreqGen.Core.Nodes.Modulators;
using FreqGen.Core.Nodes.Oscillators;

namespace FreqGen.Core
{
  /// <summary>
  /// A single audio layer combining carrier, modulation and envelope.
  /// Represents one complete signal path in the mix.
  /// </summary>
  public sealed class Layer : IAudioNode
  {
    private readonly SineOscillator _carrier;
    private readonly LFO _lfo;
    private readonly AMModulator _modulator;
    private readonly Envelope _envelope;
    private readonly float _sampleRate;

    /// <summary>
    /// Layer output weight/volume.
    /// Safe to update from any thread.
    /// </summary>
    public float Weight { get; set; } = 1f;

    /// <summary>
    /// Check if the layer has faded out completely.
    /// </summary>
    public bool IsSilent => _envelope.Current < 0.001f;

    public Layer(
      float sampleRate,
      float attackSeconds = 30f,
      float releaseSeconds = 30f
    )
    {
      _carrier = new();
      _lfo = new();
      _modulator = new();
      _envelope = new();
      _sampleRate = sampleRate;

      _envelope.SetAttackTime(attackSeconds, sampleRate);
      _envelope.SetReleaseTime(releaseSeconds, sampleRate);
    }

    /// <summary>
    /// Configure the carrier frequency.
    /// </summary>
    public void SetCarrierFrequency(float frequency) =>
      _carrier.SetFrequency(frequency, _sampleRate);

    /// <summary>
    /// Configure the modulation frequency and depth.
    /// </summary>
    /// <param name="frequency">Modulation rate in Hz (typically 0.5-30 Hz)</param>
    /// <param name="depth">Modulation depth (0.0-1.0)</param>
    public void SetModulation(float frequency, float depth)
    {
      _lfo.SetFrequency(frequency, _sampleRate);
      _modulator.Depth = depth;
    }

    /// <summary>
    /// Start the layer (begin attack phase).
    /// </summary>
    public void Start() =>
      _envelope.Trigger(true);

    /// <summary>
    /// Stop the layer (begin release phase).
    /// </summary>
    public void Stop() =>
      _envelope.Trigger(false);

    /// <summary>
    /// Generate next sample (audio thread only).
    /// </summary>
    public float NextSample()
    {
      float carrier = _carrier.NextSample();
      float lfo = _lfo.NextSample();
      float modulated = _modulator.Apply(carrier, lfo);
      float envelope = _envelope.NextSampkle();

      return modulated * envelope * Weight;
    }

    /// <summary>
    /// Reset all internal state.
    /// </summary>
    public void Reset()
    {
      _carrier.Reset();
      _lfo.Reset();
      _envelope.Reset();
    }
  }
}
