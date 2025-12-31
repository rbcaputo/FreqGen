using FreqGen.Core;
using FreqGen.Presets.Models;
using FreqGen.Presets.Presets;
using Microsoft.Extensions.Logging;

namespace FreqGen.App.Services
{
  /// <summary>
  /// Cross-platform audio service implementation.
  /// </summary>
  public sealed partial class AudioService(ILogger<AudioService> logger) : IAudioService
  {
    private readonly ILogger<AudioService> _logger = logger;
    private AudioEngine? _engine;
    private PresetEngine? _presetEngine;
    private bool _isInitialized;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;
    public FrequencyPreset? CurrentPreset => _presetEngine?.CurrentPreset;

    public async Task InitializeAsync()
    {
      if (_isInitialized)
        return;

      try
      {
        // Create core audio engine
        _engine = new(
          sampleRate: AudioSettings.SampleRate,
          layerCount: 6 // Support complex presets
        );

        // Create preset engine
        _presetEngine = new(_engine);

        // Initialize platform-specific audio
        InitializePlatformAudio();

        _isInitialized = true;
      }
      catch (Exception ex)
      {
        _logger.LogError($"Audio initialization failed: {ex}");
        throw;
      }

      await Task.CompletedTask;
    }

    public async Task PlayPresetAsync(FrequencyPreset preset)
    {
      if (!_isInitialized)
        await InitializeAsync();

      ArgumentNullException.ThrowIfNull(preset);

      try
      {
        // Stop current playback
        if (_isPlaying)
          await StopAsync();

        // Load and play preset
        _presetEngine?.LoadAndPlay(preset);
        // Start platform audio
        StartPlatformAudio();

        _isPlaying = true;
      }
      catch (Exception ex)
      {
        _logger.LogError($"Play preset failed: {ex}");
        throw;
      }
    }

    public async Task StopAsync()
    {
      if (!_isPlaying)
        return;

      try
      {
        // Stop preset engine (begins fade-out)
        _presetEngine?.Stop();
        // Stop platform audio
        StopPlatformAudio();

        _isPlaying = false;
      }
      catch (Exception ex)
      {
        _logger.LogError($"Stop failed: {ex}");
        throw;
      }

      await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
      if (_isPlaying)
        await StopAsync();

      DisposePlatformAudio();

      _engine = null;
      _presetEngine = null;
      _isInitialized = false;
    }

    // Platform-specific methods (implemented in partial classes)
    partial void InitializePlatformAudio();
    partial void StartPlatformAudio();
    partial void StopPlatformAudio();
    partial void DisposePlatformAudio();
  }
}