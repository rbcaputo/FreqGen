namespace FreqGen.Presets.Models
{
  /// <summary>
  /// Configuration for a single layer in a preset.
  /// Maps to FreqGen.Core.LayerConfig.
  /// </summary>
  public sealed record LayerConfiguration
  {
    /// <summary>
    /// Carrier frequency in Hz.
    /// </summary>
    public required float CarrierHz { get; init; }

    /// <summary>
    /// Modulation frequency in Hz (0 = no modulation).
    /// </summary>
    public required float ModulationHz { get; init; }

    /// <summary>
    /// Modulation depth (0.0-1.0).
    /// </summary>
    public required float ModulationDepth { get; init; }

    /// <summary>
    /// Layer output weight/volume (0.0-1.0).
    /// </summary>
    public required float Weight { get; init; }

    /// <summary>
    /// Optional description of this layer's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Convert to Core LayerConfig.
    /// </summary>
    public Core.LayerConfig ToLayerConfig() =>
      new()
      {
        CarrierHz = CarrierHz,
        ModulationHz = ModulationHz,
        ModulationDepth = ModulationDepth,
        Weight = Weight,
      };

    public static LayerConfiguration PureTone(
      float frequency,
      float weight = 1f,
      string? description = null
    ) => new()
    {
      CarrierHz = frequency,
      ModulationHz = 0f,
      ModulationDepth = 0f,
      Weight = weight,
      Description = description
    };

    /// <summary>
    /// Create a modulated layer for brainwave entrainment.
    /// </summary>
    public static LayerConfiguration BrainwaveLayer(
      float carrierHz,
      float brainwaveHz,
      float depth = 0.8f,
      float weight = 1f,
      string? description = null
    ) => new()
    {
      CarrierHz = carrierHz,
      ModulationHz = brainwaveHz,
      ModulationDepth = depth,
      Weight = weight,
      Description = description
    };
  }
}
