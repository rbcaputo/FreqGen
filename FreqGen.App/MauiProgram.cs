using FreqGen.App.Services;
using FreqGen.App.ViewModels;
using FreqGen.App.Views;
using Microsoft.Extensions.Logging;

namespace FreqGen.App
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
      builder.Services.AddTransient<MainViewModel>();

      // Register Views
      builder.Services.AddTransient<MainPage>();

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