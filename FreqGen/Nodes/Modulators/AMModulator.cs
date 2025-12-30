namespace FreqGen.Core.Nodes.Modulators
{
  /// <summary>
  /// Amplitude Modulation processor.
  /// Applies smooth AM to a carrier signal using an LFO.
  /// </summary>
  public sealed class AMModulator
  {
    /// <summary>
    /// Modulation depth (0.0 = no modulation, 1.0 = full modulation).
    /// Safe to update from any thread.
    /// </summary>
    public float Depth { get; set; } = 0.5f;

    /// <summary>
    /// Apply amplitude modulation to a carrier signal.
    /// </summary>
    /// <param name="carrier">Input carrier signal</param>
    /// <param name="modulator">LFO signal in range [-1, 1]</param>
    /// <returns>Modulated signal</returns>
    public float Apply(float carrier, float modulator) =>
      // Transform modulator from [-1, 1] to [1 - depth, 1 + depth]
      // This ensures the signal never inverts phase
      carrier * (1f + Depth * modulator);
  }
}
