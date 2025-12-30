namespace FreqGen.Core
{
  /// <summary>
  /// Configuration data for a single audio layer.
  /// Immutable value type for thread-safe parameter passing.
  /// </summary>
  public readonly struct LayerConfig
  {
    /// <summary>
    /// Carrier frequency in Hz (typically 20-20000 Hz for therapy audio).
    /// </summary>
    public float CarrierHz { get; init; }

    /// <summary>
    /// Modulation frequency in Hz (typically 0.5-30 Hz for brainwave entrainment).
    /// Set to 0 for no modulation (pure tone).
    /// </summary>
    public float ModulationHz { get; init; }

    /// <summary>
    /// Modulation depth (0.0 = no modulation, 1.0 = full amplitude modulation).
    /// </summary>
    public float ModulationDepth { get; init; }

    /// <summary>
    /// Layer output weight/volume (typically 0.0-1.0).
    /// </summary>
    public float Weight { get; init; }

    /// <summary>
    /// Create a simple unmodulated carrier.
    /// </summary>
    public static LayerConfig PureTone(float carrierHz, float weight = 1f) =>
      new()
      {
        CarrierHz = carrierHz,
        ModulationHz = 0f,
        ModulationDepth = 0f,
        Weight = weight
      };

    /// <summary>
    /// Create an amplitude-modulated carrier.
    /// </summary>
    public static LayerConfig ModulatedTone(
      float carrierHz,
      float modulationHz,
      float modulationDepth,
      float weight = 1f
    ) => new()
    {
      CarrierHz = carrierHz,
      ModulationHz = modulationHz,
      ModulationDepth = modulationDepth,
      Weight = weight
    };

    /// <summary>
    /// Validate configuration values.
    /// </summary>
    public bool IsValid() =>
      CarrierHz > 0f && CarrierHz <= AudioSettings.MaxFrequency &&
      ModulationHz >= 0f && ModulationHz <= 100f &&
      ModulationDepth >= 0f && ModulationDepth <= 1f &&
      Weight >= 0f;

    public override string ToString()
    {
      if (ModulationHz < 0.001f)
        return $"Carrier: {CarrierHz:F1} Hz, Weight: {Weight:F2}";

      return $"Carrier: {CarrierHz:F1} Hz, Mod: {ModulationHz:F1} HZ @ {ModulationDepth:F2}, Weight: {Weight:F2}";
    }
  }
}
