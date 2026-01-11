using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using ToneSync.App.Services;
using ToneSync.Core;
using ToneSync.Presets.Engine;
using ToneSync.Presets.Models;
using static ToneSync.App.Services.AudioService;

namespace ToneSync.App.ViewModels
{
  /// <summary>
  /// Main view model for the FreqGen application.
  /// Manages preset browsing, playback control, and error handling.
  /// </summary>
  public sealed partial class MainViewModel : ObservableObject, IDisposable
  {
    private readonly IAudioService _audioService;
    private readonly ILogger<MainViewModel> _logger;
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isPlaying;

    [ObservableProperty]
    private float _masterGain = 0.7f;

    [ObservableProperty]
    private OutputProfile _outputProfile = OutputProfile.DeviceSpeaker;

    [ObservableProperty]
    private FrequencyPreset? _currentPreset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryInitializationCommand))]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Display text for the currently playing preset.
    /// </summary>
    public string NowPlayingText => CurrentPreset is not null
      ? $"▶ {CurrentPreset.DisplayName}"
      : "No preset playing";

    /// <summary>
    /// Available output profiles for the picker.
    /// </summary>
    public IReadOnlyList<OutputProfile> OutputProfiles { get; } =
      Enum.GetValues<OutputProfile>();

    /// <summary>
    /// Grouped presets by category for UI display.
    /// </summary>
    public ObservableCollection<PresetGroup> PresetGroups { get; }

    public MainViewModel(IAudioService audioService, ILogger<MainViewModel> logger)
    {
      _audioService = audioService ??
        throw new ArgumentNullException(nameof(audioService));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));

      // Subscribe to audio service errors
      _audioService.AudioError += OnAudioError;

      // Initialize preset groups
      PresetGroups =
        [
          new(
            "🧠 Brainwave Entrainment",
            "Neural oscillation patterns for focus, relaxation, and sleep",
            PresetEngine.GetPresetsByCategory(PresetCategory.Brainwave)
          ),
          new(
            "🎵 Solfeggio Frequencies",
            "Ancient healing frequencies used in sacred music",
            PresetEngine.GetPresetsByCategory(PresetCategory.Solfeggio)
          ),
          new(
            "⚡ Isochronic Tones",
            "Rhythmic pulsing for rapid entrainment and energy",
            PresetEngine.GetPresetsByCategory(PresetCategory.Isochronic)
          ),
          new(
            "🎧 Binaural Beats",
            "Stereo frequency offsets for neural entrainment (headphones required)",
            PresetEngine.GetPresetsByCategory(PresetCategory.Binaural)
          )
        ];
    }

    /// <summary>
    /// Initializes the audio system.
    /// Should be called when the view appears.
    /// </summary>
    public async Task InitializeAsync()
    {
      if (_audioService.ChannelMode != ChannelMode.Mono)
        return;

      try
      {
        _logger.LogInformation("Initializing audio service");

        // Initialize in mono mode by default
        // Will be switched to stereo mode automatically when binaural preset is selected
        await _audioService.InitializeAsync();
        _audioService.SetOutputProfile(OutputProfile);
        _audioService.SetMasterGain(MasterGain);

        _logger.LogInformation("View model initialized successfully");
      }
      catch (Exception ex)
      {
        HasError = true;
        ErrorMessage = $"Failed to initialize audio: {ex.Message}";

        _logger.LogError(ex, "Initialization failed");
      }
    }

    partial void OnOutputProfileChanged(OutputProfile value)
    {
      _logger.LogInformation("Output profile changed to {Profile}", value);

      _audioService.SetOutputProfile(value);
    }

    partial void OnMasterGainChanged(float value)
    {
      _audioService.SetMasterGain(value);

      _logger.LogInformation("Master gain changed to {Gain}", value);
    }

    private bool CanStop() => IsPlaying;

    /// <summary>
    /// Stops the currently playing preset.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
      try
      {
        _logger.LogInformation("Stopping playback");

        await _audioService.StopAsync();

        IsPlaying = false;
        CurrentPreset = null;

        _logger.LogInformation("Playback stopped successfully");
      }
      catch (Exception ex)
      {
        HasError = true;
        ErrorMessage = $"Stop failed: {ex.Message}";

        _logger.LogError(ex, "Stop failed");
      }
    }

    /// <summary>
    /// Retries audio system initialization after a failure.
    /// </summary>
    [RelayCommand]
    private async Task RetryInitializationAsync()
    {
      try
      {
        _logger.LogInformation("Retrying initialization");

        HasError = false;
        ErrorMessage = null;

        bool success = await _audioService.RetryInitializationAsync();

        if (success)
        {
          _audioService.SetOutputProfile(OutputProfile);
          _audioService.SetMasterGain(MasterGain);

          _logger.LogInformation("Retry successful");
        }
        else
        {
          HasError = true;
          ErrorMessage = "Maximum retry attempts exceeded";

          _logger.LogWarning("Retry failed - max attempts exceeded");
        }
      }
      catch (Exception ex)
      {
        HasError = true;
        ErrorMessage = $"Retry failed: {ex.Message}";

        _logger.LogError(ex, "Retry failed");
      }
    }

    /// <summary>
    /// Dismisses the current error message.
    /// </summary>
    [RelayCommand]
    private void DismissError()
    {
      HasError = false;
      ErrorMessage = null;
    }

    /// <summary>
    /// Called internally when a preset starts playing.
    /// Updates the UI state to reflect active playback.
    /// </summary>
    internal void OnPresetStarted(FrequencyPreset preset)
    {
      CurrentPreset = preset;
      IsPlaying = true;
    }

    /// <summary>
    /// Handles critical audio errors from the service.
    /// </summary>
    private void OnAudioError(object? sender, AudioErrorEventArgs ev)
    {
      _logger.LogError("Audio error received: {Message}", ev.Message);

      // Update UI on main thread
      MainThread.BeginInvokeOnMainThread(() =>
      {
        HasError = true;
        ErrorMessage = $"Audio error: {ev.Message}\n" +
                       $"Consecutive errors: {ev.ConsecutiveErrorCount}";

        IsPlaying = false;
        CurrentPreset = null;
      });
    }

    /// <summary>
    /// Disposes the view model and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _audioService.AudioError -= OnAudioError;
      _isDisposed = true;

      _logger.LogInformation("View model disposed");
    }
  }

  /// <summary>
  /// Grouped collection of presets for categorized display.
  /// </summary>
  public sealed class PresetGroup(
    string name,
    string description,
    IEnumerable<FrequencyPreset> presets
  ) : List<FrequencyPreset>(presets)
  {
    /// <summary>
    /// Gets the category name displayed in UI.
    /// </summary>
    public string Name { get; } = name ??
      throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets the category description.
    /// </summary>
    public string Description { get; } = description ??
      throw new ArgumentNullException(nameof(description));
  }
}
