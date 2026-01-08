using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using ToneSync.App.Services;
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
    private string _statusText = "Tap to initialize...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryInitializationCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayPresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayPresetCommand))]
    private bool _isInitialized;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayPresetCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isPlaying;

    [ObservableProperty]
    private float _masterGain = 1.0f;

    [ObservableProperty]
    private OutputProfile _outputProfile = OutputProfile.DeviceSpeaker;

    [ObservableProperty]
    private FrequencyPreset? _currentPreset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetryInitializationCommand))]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

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
          )
        ];
    }

    /// <summary>
    /// Initializes the audio system.
    /// Should be called when the view appears.
    /// </summary>
    public async Task InitializeAsync()
    {
      if (IsInitialized || IsLoading)
        return;

      try
      {
        _logger.LogInformation("Initializing view model");

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        StatusText = "Initializing audio system...";

        await _audioService.InitializeAsync();
        _audioService.SetMasterGain(MasterGain);
        _audioService.SetOutputProfile(OutputProfile);

        IsInitialized = true;
        StatusText = "✓ Ready - Select a preset to begin";

        _logger.LogInformation("View model initialized successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Initialization failed");

        HasError = true;
        ErrorMessage = $"Failed to initialize audio: {ex.Message}";
        StatusText = "❌ Initialization failed";
      }
      finally
      {
        IsLoading = false;
      }
    }

    partial void OnMasterGainChanged(float value)
    {
      _logger.LogInformation("Master gain changed to {Gain}", value);

      if (IsInitialized)
        _audioService.SetMasterGain(value);
    }

    partial void OnOutputProfileChanged(OutputProfile value)
    {
      _logger.LogInformation("Output profile changed to {Profile}", value);

      if (IsInitialized)
        _audioService.SetOutputProfile(value);
    }

    private bool CanPlayPreset() => IsInitialized && !IsLoading && !IsPlaying;
    private bool CanStop() => IsPlaying && !IsLoading;
    private bool CanRetryInitialization() => HasError && !IsLoading;

    /// <summary>
    /// Plays the selected preset.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlayPreset))]
    private async Task PlayPresetAsync(FrequencyPreset? preset)
    {
      if (preset is null || IsLoading)
        return;

      try
      {
        _logger.LogInformation("Playing preset: {PresetName}", preset.DisplayName);

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        StatusText = $"Loading {preset.DisplayName}...";

        await _audioService.PlayPresetAsync(preset);

        CurrentPreset = preset;
        IsPlaying = true;
        StatusText = $"▶ Playing: {preset.DisplayName}\n" +
                     $"Duration: {preset.RecommendedDuration.TotalMinutes:F0} minutes\n" +
                     $"{preset.Description}";

        _logger.LogInformation("Playback started successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Playback failed");

        HasError = true;
        ErrorMessage = $"Playback failed: {ex.Message}";
        StatusText = "❌ Playback failed";
        IsPlaying = false;
        CurrentPreset = null;
      }
      finally
      {
        IsLoading = false;
      }
    }

    /// <summary>
    /// Stops the currently playing preset.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
      if (IsLoading)
        return;

      try
      {
        _logger.LogInformation("Stopping playback");

        IsLoading = true;
        StatusText = "Stopping (fading out)...";

        await _audioService.StopAsync();

        IsPlaying = false;
        CurrentPreset = null;
        StatusText = "⏹ Stopped - Select another preset";

        _logger.LogInformation("Playback stopped successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Stop failed");

        HasError = true;
        ErrorMessage = $"Stop failed: {ex.Message}";
        StatusText = "❌ Stop failed";
      }
      finally
      {
        IsLoading = false;
      }
    }

    /// <summary>
    /// Retries audio system initialization after a failure.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRetryInitialization))]
    private async Task RetryInitializationAsync()
    {
      if (IsLoading)
        return;

      try
      {
        _logger.LogInformation("Retrying initialization");

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        StatusText = "Retrying initialization...";

        bool success = await _audioService.RetryInitializationAsync();

        if (success)
        {
          IsInitialized = true;
          _audioService.SetMasterGain(MasterGain);
          _audioService.SetOutputProfile(OutputProfile);
          StatusText = "✓ Initialization successful - Select a preset";
          _logger.LogInformation("Retry successful");
        }
        else
        {
          HasError = true;
          ErrorMessage = "Maximum retry attempts exceeded";
          StatusText = "❌ Retry failed - Please restart the app";
          _logger.LogWarning("Retry failed - max attempts exceeded");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Retry failed");

        HasError = true;
        ErrorMessage = $"Retry failed: {ex.Message}";
        StatusText = "❌ Retry failed";
      }
      finally
      {
        IsLoading = false;
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

      if (!IsPlaying && IsInitialized)
        StatusText = "✓ Ready - Select a preset";
      else if (!IsInitialized)
        StatusText = "Tap to initialize...";
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

        if (ev.CanRetry)
          StatusText = "❌ Audio error - Tap 'Retry' to restart";
        else
          StatusText = "❌ Critical audio error - Please restart the app";
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
