using FreqGen.Core;

namespace FreqGen.Presets.Models
{
  /// <summary>
  /// Represents the human-readable configuration for a single audio layer.
  /// Includes metadata for UI display and mapping logic for the Core engine.
  /// Immutable to prevent accidental modification of preset definitions.
  /// </summary>
  public sealed record LayerConfig
  {
    /// <summary>
    /// Carrier frequency in Hz (20-2000Hz range).
    /// </summary>
    public required float CarrierHz { get; init; }

    /// <summary>
    /// Modulation frequency in Hz (0 = no modulation, 0.1-100Hz range).
    /// </summary>
    public required float ModulationHz { get; init; }

    /// <summary>
    /// Modulation depth (0.0 = no modulation, 1.0 = full modulation).
    /// </summary>
    public required float ModulationDepth { get; init; }

    /// <summary>
    /// Layer output weight/volume (0.0 = silent, 1.0 = full volume).
    /// </summary>
    public required float Weight { get; init; }

    /// <summary>
    /// Optional human-readable description of this layer's purpose.
    /// Displayed in UI when available.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Maps this domain model to the Core engine's high-performance configuration record.
    /// Performs validation during conversion.
    /// </summary>
    /// <param name="isActive">Whether this layer should currently be audible.</param>
    /// <returns>A validated LayerConfiguration for the audio engine.</returns>
    /// <exception cref="Core.Exceptions.InvalidConfigurationException">Thrown if validation fails.</exception>
    public LayerConfiguration ToCoreConfig(bool isActive) =>
      new()
      {
        CarrierFrequency = CarrierHz,
        ModulatorFrequency = ModulationHz,
        ModulatorDepth = ModulationDepth,
        Weight = Weight,
        IsActive = isActive
      };

    /// <summary>
    /// Creates a pure tone layer configuration (no modulation).
    /// Useful for Solfeggio frequencies and simple carrier tones.
    /// </summary>
    /// <param name="frequency">Carrier frequency in Hz.</param>
    /// <param name="weight">Output volume (default: 1.0).</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A LayerConfig with no modulation.</returns>
    public static LayerConfig PureTone(
      float frequency,
      float weight = 1.0f,
      string? description = null
    ) => new()
    {
      CarrierHz = frequency,
      ModulationHz = 0.0f,
      ModulationDepth = 0.0f,
      Weight = weight,
      Description = description ?? "Pure Tone"
    };

    /// <summary>
    /// Creates an amplitude-modulated layer configuration.
    /// Useful for brainwave entrainment and therapeutic audio.
    /// </summary>
    /// <param name="carrierHz">Carrier frequency in Hz.</param>
    /// <param name="modulationHz">Modulation frequency in Hz.</param>
    /// <param name="depth">Modulation depth (0.0-1.0).</param>
    /// <param name="weight">Output volume (default: 1.0).</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A LayerConfig with amplitude modulation.</returns>
    public static LayerConfig AmTone(
      float carrierHz,
      float modulationHz,
      float depth,
      float weight = 1.0f,
      string? description = null
    ) => new()
    {
      CarrierHz = carrierHz,
      ModulationHz = modulationHz,
      ModulationDepth = depth,
      Weight = weight,
      Description = description ?? $"AM Tone ({modulationHz} Hz)"
    };
  }
}
