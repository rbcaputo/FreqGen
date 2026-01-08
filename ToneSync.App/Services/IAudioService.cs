using ToneSync.Core;
using ToneSync.Presets.Models;
using static ToneSync.App.Services.AudioService;

namespace ToneSync.App.Services
{
  /// <summary>
  /// Platform-agnostic audio service interface.
  /// Provides high-level audio playback control with error handling.
  /// </summary>
  public interface IAudioService : IAsyncDisposable
  {
    /// <summary>
    /// Gets the current channel mode (Mono or Stereo).
    /// </summary>
    ChannelMode ChannelMode { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="profile"></param>
    void SetOutputProfile(OutputProfile profile);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gain"></param>
    void SetMasterGain(float gain);

    /// <summary>
    /// Gets a value indicating whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets the currently active preset, or null if none is loaded.
    /// </summary>
    FrequencyPreset? CurrentPreset { get; }

    /// <summary>
    /// Initializes the audio system.
    /// Must be called before any playback operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if initialization fails.</exception>
    Task InitializeAsync();

    /// <summary>
    /// Attempts to reinitialize the audio system after a failure.
    /// </summary>
    /// <returns>True if retry succeeded; false otherwise.</returns>
    Task<bool> RetryInitializationAsync();

    /// <summary>
    /// Loads and plays a frequency preset.
    /// Automatically stops any currently playing preset.
    /// </summary>
    /// <param name="preset">The preset to play.</param>
    /// <exception cref="ArgumentNullException">Thrown if preset is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if playback fails.</exception>
    Task PlayPresetAsync(FrequencyPreset preset);

    /// <summary>
    /// Stops the currently playing preset.
    /// Audio will fade out over the configured release time.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Raised when a critical audio error occurs.
    /// Event handlers are invoked on a background thread, not the audio callback thread.
    /// </summary>
    event EventHandler<AudioErrorEventArgs>? AudioError;
  }
}