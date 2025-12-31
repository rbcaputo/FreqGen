using FreqGen.Core;
using FreqGen.Presets.Models;

namespace FreqGen.Presets.Presets
{
  /// <summary>
  /// High-level engine for managing frequency presets.
  /// Bridges between preset definitions and the core audio engine.
  /// </summary>
  /// <remarks>
  /// Create a preset engine wrapping an audio engine.
  /// </remarks>
  public sealed class PresetEngine(AudioEngine audioEngine)
  {
    private readonly AudioEngine _audioEngine = audioEngine ??
      throw new ArgumentNullException(nameof(audioEngine));
    private FrequencyPreset? _currentPreset;

    /// <summary>
    /// Currently loaded preset (null if none).
    /// </summary>
    public FrequencyPreset? CurrentPreset => _currentPreset;

    /// <summary>
    /// Check if a preset is currently loaded.
    /// </summary>
    public bool HasPreset => _currentPreset is not null;

    /// <summary>
    /// Check if audio is currently playing.
    /// </summary>
    public bool IsPlaying => _audioEngine.IsPlaying;

    /// <summary>
    /// Load a preset into the audio engine.
    /// Does not start playback automatically.
    /// </summary>
    public void LoadPreset(FrequencyPreset preset)
    {
      ArgumentNullException.ThrowIfNull(nameof(preset));

      preset.Validate();

      // Stop current playback if running
      if (_audioEngine.IsPlaying)
        Stop();

      _currentPreset = preset;

      // Convert preset layers to core layer configs
      LayerConfiguration[] layerConfigs = preset.ToLayerConfigs();

      // Validate we have enough layers
      if (layerConfigs.Length > _audioEngine.LayerCount)
        throw new InvalidOperationException(
          $"Preset '{preset.DisplayName}' requires {layerConfigs.Length} layers, " +
          $"but engine only has {_audioEngine.LayerCount}");

      // Configure the audio engine
      _audioEngine.ConfigureLayers(layerConfigs);
    }

    /// <summary>
    /// Start playback of the currently loaded preset.
    /// </summary>
    public void Start()
    {
      if (_currentPreset is null)
        throw new InvalidOperationException("No preset loaded. Call LoadPreset first.");

      _audioEngine.Start();
    }

    /// <summary>
    /// Stop playback (begins fade-out).
    /// </summary>
    public void Stop() =>
      _audioEngine.Stop();

    /// <summary>
    /// Load and immediately start a preset.
    /// </summary>
    public void LoadAndPlay(FrequencyPreset preset)
    {
      LoadPreset(preset);
      Start();
    }

    /// <summary>
    /// Reset the engine to silence.
    /// </summary>
    public void Reset()
    {
      _audioEngine.Reset();
      _currentPreset = null;
    }

    #region Static Preset Discovery

    /// <summary>
    /// Get all available presets from all categories.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetAllPresets() =>
      BrainwavePresets.All
        .Concat(SolfeggioPresets.All)
        .Concat(IsochronicPresets.All);

    /// <summary>
    /// Get presets filtered by category.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetPresetsByCategory(PresetCategory category) =>
      GetAllPresets().Where(p => p.Category == category);

    /// <summary>
    /// Get presets filtered by tag.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetPresetByTag(string tag) =>
      GetAllPresets().Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Find a preset by its ID.
    /// </summary>
    public static FrequencyPreset? FindPresetByID(string id) =>
      GetAllPresets().FirstOrDefault(p =>
        p.ID.Equals(id, StringComparison.OrdinalIgnoreCase)
      );

    /// <summary>
    /// Get all available categories that have presets.
    /// </summary>
    public static PresetCategory[] GetAvailableCategories() =>
      [
        .. GetAllPresets()
          .Select(p => p.Category)
          .Distinct()
          .OrderBy(c => c)
      ];

    /// <summary>
    /// Get all unique tags across all presets.
    /// </summary>
    public static string[] GetAllTags() =>
      [
        .. GetAllPresets()
          .SelectMany(p => p.Tags)
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .OrderBy(t => t)
      ];

    #endregion
  }
}
