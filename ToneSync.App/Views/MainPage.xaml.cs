using CommunityToolkit.Mvvm.Input;
using ToneSync.App.ViewModels;
using ToneSync.Presets.Models;

namespace ToneSync.App.Views
{
  public sealed partial class MainPage : ContentPage
  {
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
      InitializeComponent();

      _viewModel = viewModel;
      BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
      base.OnAppearing();
      await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
      base.OnDisappearing();

      // Playback continues in background
      // User can stop via notification or by returning to app
    }

    /// <summary>
    /// Navigate to preset detail page.
    /// Exposed as a command for XAML binding.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToPreset(FrequencyPreset preset)
    {
      if (preset is null)
        return;

      await Shell.Current.GoToAsync(
        nameof(PresetDetailPage),
        new Dictionary<string, object>
        {
          ["Preset"] = preset
        }
      );
    }
  }
}
