using Microsoft.Extensions.Logging;
using ToneSync.App.Services;
using ToneSync.App.ViewModels;
using ToneSync.App.Views;

namespace ToneSync.App
{
  public static class MauiProgram
  {
    public static MauiApp CreateMauiApp()
    {
      MauiAppBuilder builder = MauiApp.CreateBuilder();

      builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
          fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
          fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

      // Register services
      builder.Services.AddSingleton<IAudioService, AudioService>();

      // Register ViewModels
      builder.Services.AddSingleton<MainViewModel>();
      builder.Services.AddTransient<PresetDetailViewModel>();

      // Register Views
      builder.Services.AddSingleton<MainPage>();
      builder.Services.AddTransient<PresetDetailPage>();

      builder.Services.AddLogging(logging =>
      {
        logging.AddDebug();
#if ANDROID || IOS
        logging.SetMinimumLevel(LogLevel.Information);
#endif
      });

      return builder.Build();
    }
  }
}