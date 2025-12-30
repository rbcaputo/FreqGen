using Android.Util;
using Android.Views;

namespace FreqGen.PoC
{
  [Activity(Label = "FreqGen PoC", MainLauncher = true)]
  public class MainActivity : Activity
  {
    private AndroidAudioPlayer? _player;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
      base.OnCreate(savedInstanceState);

      LinearLayout layout = new(this)
      {
        Orientation = Orientation.Vertical
      };
      layout.SetPadding(16, 500, 16, 16);

      EditText input = new(this)
      {
        Hint = "Frequency (Hz)",
        InputType = Android.Text.InputTypes.ClassNumber |
                    Android.Text.InputTypes.NumberFlagDecimal
      };

      Button startButton = new(this)
      {
        Text = "Start",
        LayoutParameters = new LinearLayout.LayoutParams(
          ViewGroup.LayoutParams.MatchParent,
          ViewGroup.LayoutParams.WrapContent
        )
      };

      Button stopButton = new(this)
      {
        Text = "Stop",
        LayoutParameters = new LinearLayout.LayoutParams(
          ViewGroup.LayoutParams.MatchParent,
          ViewGroup.LayoutParams.WrapContent
        )
      };

      TextView statusText = new(this)
      {
        Text = "Initializing...",
        LayoutParameters = new LinearLayout.LayoutParams(
          ViewGroup.LayoutParams.MatchParent,
          ViewGroup.LayoutParams.WrapContent
        )
      };

      layout.AddView(input);
      layout.AddView(startButton);
      layout.AddView(stopButton);
      layout.AddView(statusText);

      SetContentView(layout);

      try
      {
        _player = new();
        statusText.Text = "Ready";

        startButton.Click += (_, _) =>
        {
          try
          {
            if (double.TryParse(input.Text, out double frequency))
              _player?.SetFrequency(frequency);

            _player?.Start();
            statusText.Text = "Playing";
          }
          catch (Exception ex)
          {
            statusText.Text = $"Start Error: {ex.Message}";
            Log.Error("FreqGen", $"Start error: {ex}");
          }
        };

        stopButton.Click += (_, _) =>
        {
          try
          {
            _player?.Stop();
            statusText.Text = "Stopped";
          }
          catch (Exception ex)
          {
            statusText.Text = $"Stop Error: {ex.Message}";
            Log.Error("FreqGen", $"Stop error: {ex}");
          }
        };
      }
      catch (Exception ex)
      {
        statusText.Text = $"Init Error: {ex.Message}";
        Log.Error("FreqGen", $"Init error: {ex}");
      }
    }

    protected override void OnDestroy()
    {
      _player?.Stop();
      base.OnDestroy();
    }
  }
}
