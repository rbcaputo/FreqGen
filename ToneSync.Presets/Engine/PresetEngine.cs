using ToneSync.Core;
using ToneSync.Core.Engine;
using ToneSync.Core.Layers;
using ToneSync.Presets.Models;
using ToneSync.Presets.Presets;

namespace ToneSync.Presets.Engine
{
  /// <summary>
  /// Manages the selection, loading, and state tracking of frequency presets.
  /// Acts as the primary interface between the UI and the AudioEngine.
  /// Thread-safe for UI updates.
  /// </summary>
  /// <remarks>
  /// Initializes a new instance of the <see cref="PresetEngine"/> class.
  /// </remarks>
  /// <param name="audioEngine">The audio engine to control.</param>
  public sealed class PresetEngine(AudioEngine audioEngine)
  {
    private readonly AudioEngine _audioEngine = audioEngine
      ?? throw new ArgumentNullException(nameof(audioEngine));
    private FrequencyPreset? _activePreset;

    // Cache of converted core configurations (updated when preset changes)
    private List<LayerConfiguration> _currentCoreConfigs = [];

    /// <summary>
    /// Gets the currently active preset (null if none loaded).
    /// </summary>
    public FrequencyPreset? ActivePreset => _activePreset;

    /// <summary>
    /// Gets a value indicating whether a preset is currently loaded.
    /// </summary>
    public bool HasActivePreset => _activePreset != null;

    /// <summary>
    /// Gets a value indicating whether the audio engine is currently playing.
    /// </summary>
    public bool IsPlaying => _audioEngine.IsPlaying;

    /// <summary>
    /// Returns all available presets from all categories.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetAllPresets() =>
      BrainwavePresets.All
        .Concat(SolfeggioPresets.All)
        .Concat(IsochronicPresets.All);

    /// <summary>
    /// Returns presets filtered by category.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetPresetsByCategory(PresetCategory category) =>
      GetAllPresets().Where(p => p.Category == category);

    /// <summary>
    /// Returns presets filtered by tags.
    /// </summary>
    public static IEnumerable<FrequencyPreset> GetPresetsByTag(string tag) =>
      GetAllPresets().Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Loads a preset and initializes the audio engine.
    /// Validates preset before loading and converts to core configurations.
    /// </summary>
    /// <param name="preset">The preset to load.</param>
    /// <param name="attackSeconds">Override default attack time (optional).</param>
    /// <param name="releaseSeconds">Override default release time (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown if preset is null.</exception>
    /// <exception cref="Exceptions.PresetValidationException">Thrown if preset validation fails.</exception>
    public void LoadPreset(
      FrequencyPreset preset,
      float? attackSeconds = null,
      float? releaseSeconds = null
    )
    {
      ArgumentNullException.ThrowIfNull(preset);

      // Validate preset structure
      preset.Validate();

      // Convert preset layers to core configurations
      List<LayerConfiguration> coreConfigs = new(preset.Layers.Count);

      foreach (LayerConfig layer in preset.Layers)
      {
        // Convert and validate each layer
        LayerConfiguration coreConfig = layer.ToCoreConfig(isActive: false); // Start inactive
        coreConfig.ValidateForSampleRate(_audioEngine.SampleRate);
        coreConfigs.Add(coreConfig);
      }

      // Store converted configs
      _currentCoreConfigs = coreConfigs;

      // Initialize audio engine with new configuration
      _audioEngine.Initialize(
        _currentCoreConfigs.AsReadOnly(),
        attackSeconds ?? AudioSettings.EnvelopeSettings.DefaultAttackSeconds,
        releaseSeconds ?? AudioSettings.EnvelopeSettings.DefaultReleaseSeconds
      );

      // Store active preset
      _activePreset = preset;
    }

    /// <summary>
    /// Starts playback of the currently loaded preset.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no preset is loaded.</exception>
    public void StartPlayback()
    {
      if (_activePreset is null)
        throw new InvalidOperationException("No preset loaded. Call LoadPreset() first.");

      // Activate all layers
      UpdateLayerStates(isActive: true);

      // Start audio engine
      _audioEngine.Start();
    }

    /// <summary>
    /// Stops playback and triggers release phase.
    /// Audio will fade out over the configured release time.
    /// </summary>
    public void StopPlayback()
    {
      if (_activePreset is null)
        return;

      // Stop audio engine (triggers release)
      _audioEngine.Stop();

      // Deactivate all layers
      UpdateLayerStates(isActive: false);
    }

    /// <summary>
    /// Updates the active state of all layers and pushes changes to audio engine.
    /// </summary>
    private void UpdateLayerStates(bool isActive)
    {
      if (_activePreset is null)
        return;

      // Rebuild configurations with new active state
      List<LayerConfiguration> updatedConfigs = new(_activePreset.Layers.Count);

      foreach (LayerConfig layer in _activePreset.Layers)
      {
        LayerConfiguration config = layer.ToCoreConfig(isActive);
        updatedConfigs.Add(config);
      }

      _currentCoreConfigs = updatedConfigs;

      // Push changes to audio engine (thread-safe)
      _audioEngine.UpdateConfigs(_currentCoreConfigs.AsReadOnly());
    }

    /// <summary>
    /// Immediately resets the engine and clears the active preset.
    /// Use for emergency stop, not normal playback termination.
    /// </summary>
    public void Reset()
    {
      _audioEngine.Reset();
      _currentCoreConfigs.Clear();
      _activePreset = null;
    }

    /// <summary>
    /// Gets the current envelope value for a specific layer.
    /// Useful for UI metering and visual feedback.
    /// </summary>
    /// <param name="layerIndex">Zero-based layer index.</param>
    /// <returns>Envelope value (0.0 to 1.0).</returns>
    public float GetLayerEnvelopeValue(int layerIndex)
    {
      if (!HasActivePreset || layerIndex < 0 || layerIndex >= _currentCoreConfigs.Count)
        return 0.0f;

      return _audioEngine.GetLayerEnvelopeValue(layerIndex);
    }
  }
}
