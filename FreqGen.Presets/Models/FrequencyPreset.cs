namespace FreqGen.Presets.Models
{
  /// <summary>
  /// A complete frequency therapy preset with metadata and layer configurations.
  /// </summary>
  public sealed class FrequencyPreset
  {
    /// <summary>
    /// Unique identifier for this preset.
    /// </summary>
    public required string ID { get; init; }

    /// <summary>
    /// Display name shown to users.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Detailed description of the preset's intended effect.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category this preset belongs to.
    /// </summary>
    public required PresetCategory Category { get; init; }

    /// <summary>
    /// Layer configurations for this preset.
    /// </summary>
    public required LayerConfig[] Layers { get; init; }

    /// <summary>
    /// Recommended listening duration.
    /// </summary>
    public TimeSpan RecommendedDuration { get; init; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Optional tags for searching/filtering.
    /// </summary>
    public string[] Tags { get; init; } = [];

    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(ID))
        throw new InvalidOperationException("Preset ID cannot be empty");

      if (Layers.Length == 0)
        throw new InvalidOperationException("Preset must have at least one layer");

      foreach (LayerConfig layer in Layers)
        if (!layer.ToLayerConfig().IsValid())
          throw new InvalidOperationException($"Invalid layer configuration in preset {ID}");
    }

    /// <summary>
    /// Convert all layers to Core LayerConfig array.
    /// </summary>
    public Core.LayerConfiguration[] ToLayerConfigs() =>
      [.. Layers.Select(l => l.ToLayerConfig())];

    /// <summary>
    /// Get a summary of the preset.
    /// </summary>
    public string GetSummary() =>
      $"{DisplayName} ({Category}) - {Layers.Length} layer(s), {RecommendedDuration.TotalMinutes}min";
  }
}
