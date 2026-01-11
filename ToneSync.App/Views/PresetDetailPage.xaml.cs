using ToneSync.App.ViewModels;
using ToneSync.Presets.Models;

namespace ToneSync.App.Views
{
  [QueryProperty(nameof(Preset), "Preset")]
  public sealed partial class PresetDetailPage : ContentPage
  {
    private readonly PresetDetailViewModel _viewModel;

    public FrequencyPreset? Preset { get; set; }

    public PresetDetailPage(PresetDetailViewModel viewModel)
    {
      InitializeComponent();

      _viewModel = viewModel;
      BindingContext = _viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
      base.OnNavigatedTo(args);

      if (Preset is not null)
        _viewModel.Initialize(Preset);
    }
  }
}
