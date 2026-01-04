using FreqGen.Core;
using FreqGen.Presets.Models;
using FreqGen.Presets.Presets;
using Microsoft.Extensions.Logging;

namespace FreqGen.App.Services
{
  /// <summary>
  /// Cross-platform audio service implementation.
  /// Manages AudioEngine and PresetEngine lifecycle with proper error handling.
  /// </summary>
  public sealed partial class AudioService(ILogger<AudioService> logger) : IAudioService, IAsyncDisposable
  {
    private readonly ILogger<AudioService> _logger = logger
      ?? throw new ArgumentNullException(nameof(logger));
    private AudioEngine? _engine;
    private PresetEngine? _presetEngine;
    private bool _isInitialized;
    private bool _isDisposed;

    // Error handling
    private const int MaxInitRetries = 3;
    private int _initRetryCount;

    public bool IsPlaying => _engine?.IsPlaying ?? false;

    public FrequencyPreset? CurrentPreset => _presetEngine?.ActivePreset;

    public event EventHandler<AudioErrorEventArgs>? AudioError;

    public async Task InitializeAsync()
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (_isInitialized)
        return;

      try
      {
        _logger.LogInformation("Initializing audio service...");

        // Create core audio engine
        _engine = new(AudioSettings.SampleRate);

        // Subscribe to critical errors
        _engine.CriticalError += OnEngineCriticalError;

        // Create preset engine
        _presetEngine = new(_engine);

        // Initialize platform-specific audio
        InitializePlatformAudio();

        _isInitialized = true;
        _initRetryCount = 0;

        _logger.LogInformation("Audio service initialized successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError($"Audio initialization failed");

        // Clean up on failure
        await CleanupAsync();

        throw new InvalidOperationException("Failed to initialize audio system", ex);
      }

      await Task.CompletedTask;
    }

    public async Task PlayPresetAsync(FrequencyPreset preset)
    {
      ArgumentNullException.ThrowIfNull(preset);
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (!_isInitialized)
      {
        _logger.LogInformation("Audio not initialized, initializing now...");
        await InitializeAsync();
      }

      try
      {
        _logger.LogInformation("Playing preset: {PresetName}", preset.DisplayName);

        // Stop current playback if any
        if (IsPlaying)
        {
          _logger.LogDebug("Stopping current playback");
          await StopAsync();
        }

        // Brief delay to allow clean stop
        await Task.Delay(100);

        // Load preset (validates and initialize engine)
        _presetEngine?.LoadPreset(
          preset,
          AudioSettings.EnvelopeSettings.DefaultAttackSeconds,
          AudioSettings.EnvelopeSettings.DefaultReleaseSeconds
        );

        // Start platform audio thread
        StartPlatformAudio();

        // Start playback
        _presetEngine?.StartPlayback();

        _logger.LogInformation("Successfully started preset: {PresetName}", preset.DisplayName);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to play preset: {PresetName}", preset.DisplayName);

        // Ensure clean state on failure
        try
        {
          await StopAsync();
        }
        catch
        {
          // Ignore errors during cleanup
        }

        throw new InvalidOperationException($"Failed to play preset: {preset.DisplayName}", ex);
      }
    }

    public async Task StopAsync()
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (!IsPlaying)
        return;

      try
      {
        _logger.LogInformation("Stopping audio playback");

        // Stop preset engine (triggers release phase)
        _presetEngine?.StopPlayback();

        // Allow release phase to complete (brief delay)
        await Task.Delay(50);

        // Stop platform audio thread
        StopPlatformAudio();

        _logger.LogInformation("Audio playback stopped successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error while stopping playback");
        throw new InvalidOperationException("Failed to stop playback", ex);
      }
    }

    public async Task<bool> RetryInitializationAsync()
    {
      if (_initRetryCount >= MaxInitRetries)
      {
        _logger.LogError("Maximum initialization retries ({MaxRetries}) exceeded", MaxInitRetries);
        return false;
      }

      _initRetryCount++;
      _logger.LogInformation(
        "Retrying audio initialization (attempt {Attempt}/{MaxAttempts})",
        _initRetryCount,
        MaxInitRetries
      );

      try
      {
        // Clean up existing resources
        await CleanupAsync();

        // Wait before retry
        await Task.Delay(500);

        // Try initialization again
        await InitializeAsync();

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Initialization retry failed");
        return false;
      }
    }

    public async ValueTask DisposeAsync()
    {
      if (_isDisposed)
        return;

      _logger.LogInformation("Disposing audio service");

      await CleanupAsync();

      _isDisposed = true;

      _logger.LogInformation("Audio service disposed");

      DisposePlatformAudio();
    }

    /// <summary>
    /// Cleans up all resources without throwing exceptions.
    /// </summary>
    private async Task CleanupAsync()
    {
      try
      {
        if (IsPlaying)
          await StopAsync();
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Error during stop in cleanup");
      }

      try
      {
        DisposePlatformAudio();
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Error during platform cleanup");
      }

      if (_engine is not null)
      {
        _engine.CriticalError -= OnEngineCriticalError;
        _engine.Dispose();
        _engine = null;
      }

      _presetEngine = null;
      _isInitialized = false;
    }

    /// <summary>
    /// Handles critical errors from the audio engine.
    /// Raised on background thread, not audio callback thread.
    /// </summary>
    private void OnEngineCriticalError(object? sender, AudioEngine.AudioErrorEventArgs ev)
    {
      _logger.LogCritical(
        ev.Error,
        "Critical audio engine error after {Count} consecutive errors",
        ev.ConsecutiveErrorCount
      );

      // Forward error to service subscribers
      try
      {
        AudioError?.Invoke(this, new(
          ev.Error.Message,
          ev.ConsecutiveErrorCount,
          canRetry: _initRetryCount < MaxInitRetries
        ));
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error while raising AudioError event");
      }
    }

    // Platform-specific methods (implemented in partial classes)
    partial void InitializePlatformAudio();
    partial void StartPlatformAudio();
    partial void StopPlatformAudio();
    partial void DisposePlatformAudio();

    /// <summary>
    /// Event arguments for audio service errors.
    /// </summary>
    public sealed class AudioErrorEventArgs : EventArgs
    {
      /// <summary>
      /// Gets the error message.
      /// </summary>
      public string Message { get; }

      /// <summary>
      /// Gets the number of consecutive errors that occurred.
      /// </summary>
      public int ConsecutiveErrorCount { get; }

      /// <summary>
      /// Gets a value indicating whether retry is possible.
      /// </summary>
      public bool CanRetry { get; }

      /// <summary>
      /// Gets the timestamp when the error occurred.
      /// </summary>
      public DateTime Timestamp { get; }

      internal AudioErrorEventArgs(string message, int consecutiveErrorCount, bool canRetry)
      {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ConsecutiveErrorCount = consecutiveErrorCount;
        CanRetry = canRetry;
        Timestamp = DateTime.UtcNow;
      }
    }
  }
}
