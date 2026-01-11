using ToneSync.App.Views;

namespace ToneSync.App
{
  public sealed partial class AppShell : Shell
  {
    public AppShell()
    {
      InitializeComponent();

      // Register preset detail page route
      Routing.RegisterRoute(
        nameof(PresetDetailPage),
        typeof(PresetDetailPage)
      );
    }
  }
}
