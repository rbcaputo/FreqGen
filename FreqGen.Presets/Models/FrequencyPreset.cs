using FreqGen.Presets.Exceptions;

namespace FreqGen.Presets.Models
{
  /// <summary>
  /// Defines a complete therapeutic audio program consisting of multiple layers.
  /// Immutable to prevent accidental modification of preset definitions.
  /// </summary>
  public sealed class FrequencyPreset
  {
    private IReadOnlyList<LayerConfig>? _layers;

    /// <summary>
    /// Unique identifier for this preset.
    /// Used for persistence, analytics, and preset selection.
    /// </summary>
    public required string ID { get; init; }

    /// <summary>
    /// Display name shown to users in the UI.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Detailed description of the preset's intended effect and use case.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category this preset belongs to (Brainwave, Solfeggio, etc.).
    /// </summary>
    public required PresetCategory Category { get; init; }

    /// <summary>
    /// Layer configurations for this preset.
    /// Immutable collection to prevent modification.
    /// </summary>
    public required IReadOnlyList<LayerConfig> Layers
    {
      get => _layers ?? Array.Empty<LayerConfig>();
      init
      {
        if (value is null || value.Count == 0)
          throw new PresetValidationException(
            "Preset must contain at least one layer.",
            nameof(Layers)
          );

        _layers = value;
      }
    }

    /// <summary>
    /// Recommended listening duration for optimal effect.
    /// Default: 20 minutes (typical therapeutic session length).
    /// </summary>
    public TimeSpan RecommendedDuration { get; init; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Optional tags for searching, filtering, and categorization.
    /// Examples: "sleep", "focus", "meditation", "energy"
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Indicates whether this preset is suitable for beginners.
    /// Gentle presets (low depth, slow modulation) should be marked as true.
    /// </summary>
    public bool IsBeginnerFriendly { get; init; }

    /// <summary>
    /// Performs comprehensive validation on all layers.
    /// Ensures frequencies and weights are within safe hardware limits.
    /// </summary>
    /// <exception cref="PresetValidationException">Thrown if validation fails.</exception>
    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(ID))
        throw new PresetValidationException("Preset ID cannot be empty.", nameof(ID));

      if (string.IsNullOrWhiteSpace(DisplayName))
        throw new PresetValidationException("Preset DisplayName cannot be empty.", nameof(DisplayName));

      if (Layers.Count == 0)
        throw new PresetValidationException("Preset must contain at least one layer.", nameof(Layers));

      if (Layers.Count > Core.AudioSettings.MaxLayers)
        throw new PresetValidationException(
          $"Preset cannot exceed {Core.AudioSettings.MaxLayers} layers. Got: {Layers.Count}",
          nameof(Layers)
        );

      // Validate each layer
      for (int i = 0; i < Layers.Count; i++)
      {
        LayerConfig layer = Layers[i];

        // Validate carrier frequency
        if (!Core.AudioSettings.Carrier.IsValid(layer.CarrierHz, Core.AudioSettings.SampleRate))
          throw new PresetValidationException(
            $"Layer {i}: Carrier frequency {layer.CarrierHz} Hz is outside valid range " +
            $"({Core.AudioSettings.Carrier.Minimum}-{Core.AudioSettings.Carrier.Maximum}Hz).",
            $"Layers[{i}].CarrierHz"
          );

        // Validate modulation frequency (if used)
        if (layer.ModulationHz > 0.0f && !Core.AudioSettings.Modulation.IsValid(layer.ModulationHz))
          throw new PresetValidationException(
            $"Layer {i}: Modulation frequency {layer.ModulationHz} Hz is outside valid range " +
            $"({Core.AudioSettings.Modulation.Minimum}-{Core.AudioSettings.Modulation.Maximum}Hz).",
            $"Layers[{i}].ModulationHz"
          );

        // Validate modulation depth
        if (!Core.AudioSettings.Amplitude.IsValid(layer.ModulationDepth))
          throw new PresetValidationException(
            $"Layer {i}: Modulation depth {layer.ModulationDepth} is outside valid range (0.0-1.0).",
            $"Layers[{i}].ModulationDepth"
          );

        // Validate weight
        if (!Core.AudioSettings.Amplitude.IsValid(layer.Weight))
          throw new PresetValidationException(
            $"Layer {i}: Weight {layer.Weight} is outside valid range (0.0-1.0).",
            $"Layers[{i}].Weight"
          );
      }
    }

    /// <summary>
    /// Returns a string representation of the preset for debugging.
    /// </summary>
    public override string ToString() =>
      $"{DisplayName} ({Category}, {Layers.Count} layers, {RecommendedDuration.TotalMinutes:F0}min)";
  }
}
