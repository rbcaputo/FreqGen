using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ToneSync.App.Services;
using ToneSync.Core;
using ToneSync.Presets.Models;

namespace ToneSync.App.ViewModels
{
  /// <summary>
  /// View model for the preset detail screen.
  /// Displays preset information and handles playback.
  /// </summary>
  public sealed partial class PresetDetailViewModel : ObservableObject
  {
    private readonly IAudioService _audioService;
    private readonly MainViewModel _mainViewModel;
    private readonly ILogger<PresetDetailViewModel> _logger;

    [ObservableProperty]
    private FrequencyPreset? _preset;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Gets whether this preset requires stereo mode (binaural).
    /// </summary>
    public bool RequiresStereo => Preset?.Category == PresetCategory.Binaural;

    /// <summary>
    /// Gets the warning text to display for this preset.
    /// </summary>
    public string WarningText => RequiresStereo
      ? "🎧 This preset requires headphones for proper binaural effect."
      : "";

    /// <summary>
    /// Gets whether to show the stereo warning.
    /// </summary>
    public bool ShowWarning => RequiresStereo;

    /// <summary>
    /// Gets the formatted duration text.
    /// </summary>
    public string DurationText => Preset is not null
      ? $"Recommended: {Preset.RecommendedDuration.TotalMinutes:F0} minutes"
      : "";

    /// <summary>
    /// Gets the category badge text.
    /// </summary>
    public string CategoryText => Preset?.Category.ToString() ?? "";

    /// <summary>
    /// Gets the tags as a comma-separated string.
    /// </summary>
    public string TagsText => Preset is not null && Preset.Tags.Count > 0
      ? string.Join(", ", Preset.Tags)
      : "No tags";

    public PresetDetailViewModel(
      IAudioService audioService,
      MainViewModel mainViewModel,
      ILogger<PresetDetailViewModel> logger
    )
    {
      _audioService = audioService ??
        throw new ArgumentNullException(nameof(audioService));
      _mainViewModel = mainViewModel ??
        throw new ArgumentNullException(nameof(mainViewModel));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the view model with a preset.
    /// </summary>
    public void Initialize(FrequencyPreset preset)
    {
      Preset = preset ?? throw new ArgumentNullException(nameof(preset));

      OnPropertyChanged(nameof(RequiresStereo));
      OnPropertyChanged(nameof(WarningText));
      OnPropertyChanged(nameof(ShowWarning));
      OnPropertyChanged(nameof(DurationText));
      OnPropertyChanged(nameof(CategoryText));
      OnPropertyChanged(nameof(TagsText));
    }

    private bool CanPlay() => !IsLoading && Preset is not null;

    /// <summary>
    /// Plays the preset and navigates back to the main screen.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayAsync()
    {
      if (Preset is null)
        return;

      try
      {
        _logger.LogInformation("Playing preset: {PresetName}", Preset.DisplayName);

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        // Determine required channel mode
        ChannelMode requiredMode = Preset.Category == PresetCategory.Binaural
          ? ChannelMode.Stereo
          : ChannelMode.Mono;

        // Check if we need to reinitialize with different channel mode
        if (_audioService.ChannelMode != requiredMode)
        {
          _logger.LogInformation(
            "Reinitializing audio service for {Mode} mode",
            requiredMode
          );

          await _audioService.InitializeAsync(requiredMode);
          _audioService.SetOutputProfile(_mainViewModel.OutputProfile);
          _audioService.SetMasterGain(_mainViewModel.MasterGain);
        }

        // Play the preset
        await _audioService.PlayPresetAsync(Preset);

        // Update main view model state
        _mainViewModel.OnPresetStarted(Preset);

        _logger.LogInformation("Playback started successfully");

        // Navigate back to main screen
        await Shell.Current.GoToAsync("..");
      }
      catch (Exception ex)
      {
        HasError = true;
        ErrorMessage = $"Failed to play preset: {ex.Message}";

        _logger.LogError(ex, "Failed to play preset: {PresetName}", Preset.DisplayName);
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
    }
  }
}
