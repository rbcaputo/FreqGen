using FreqGen.Core.Exceptions;
using System.Runtime.CompilerServices;

namespace FreqGen.Core
{
  /// <summary>
  /// The primary entry point for the FreqGen DSP engine.
  /// Coordinates the Mixer and provides a thread-safe interface for UI and platform audio.
  /// Implements lock-free configuration updates via snapshot pattern.
  /// </summary>
  public sealed class AudioEngine : IDisposable
  {
    private readonly Mixer _mixer = new();
    private readonly float _sampleRate;
    private readonly Lock _initializationLock = new();

    // Lock-free configuration snapshot (accessed by audio thread)
    private LayerConfiguration[] _configSnapshot = [];

    // Volatile flag for configuration updates (lock-free synchronization)
    private volatile bool _configDirty;

    // State flags
    private bool _isInitialized;
    private bool _isPlaying;
    private bool _isDisposed;

    // Error tracking for graceful degradation
    private int _consecutiveErrorCount;
    private const int MaxConsecutiveErrors = 3;

    // Lock-free error reporting to UI thread
    private volatile Exception? _lastError;
    private volatile bool _hasCriticalError;

    /// <summary>
    /// Gets the current sample rate the engine is operating at.
    /// </summary>
    public float SampleRate => _sampleRate;

    /// <summary>
    /// Gets a value indicating whether the engine has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets a value indicating whether the engine is currently generating audio.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Gets the last error that occurred in the audio callback, if any.
    /// Thread-safe: can be polled from UI thread.
    /// </summary>
    public Exception? LastError => _lastError;

    /// <summary>
    /// Raised when a critical audio error occurs that stops playback.
    /// This event is raised asynchronously from a background thread, NOT from the audio callback.
    /// Subscribers should marshal to the UI thread if updating UI elements.
    /// </summary>
    public event EventHandler<AudioErrorEventArgs>? CriticalError;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioEngine"/> class.
    /// </summary>
    /// <param name="sampleRate">The target sample rate (e.g., 44100 Hz).</param>
    public AudioEngine(float sampleRate = AudioSettings.SampleRate)
    {
      if (sampleRate < 8000 || sampleRate > 192000)
        throw new ArgumentException(
          $"Sample rate {sampleRate} Hz is outside valid range (8000-192000).",
          nameof(sampleRate)
        );

      _sampleRate = sampleRate;
    }

    /// <summary>
    /// Initializes the engine with a specific layer configuration.
    /// Must be called before Start().
    /// </summary>
    /// <param name="configs">The layer configurations to use.</param>
    /// <param name="attackSeconds">Envelope attack time (default: 10s).</param>
    /// <param name="releaseSeconds">Envelope release time (default: 30s).</param>
    /// <exception cref="InvalidConfigurationException">Thrown if configs are invalid.</exception>
    public void Initialize(
      IReadOnlyList<LayerConfiguration> configs,
      float attackSeconds = AudioSettings.Envelope.DefaultAttackSeconds,
      float releaseSeconds = AudioSettings.Envelope.DefaultReleaseSeconds
    )
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (configs == null || configs.Count == 0)
        throw new InvalidConfigurationException(
          "At least one layer configuration is required.",
          nameof(configs)
        );

      if (configs.Count > AudioSettings.MaxLayers)
        throw new InvalidConfigurationException(
          $"Too many layers. Maximum is {AudioSettings.MaxLayers}, got {configs.Count}.",
          nameof(configs)
        );

      // Validate all configurations
      foreach (LayerConfiguration config in configs)
        config.ValidateForSampleRate(_sampleRate);

      lock (_initializationLock)
      {
        // Initialize mixer with layer count
        _mixer.Initialize(configs.Count, _sampleRate, attackSeconds, releaseSeconds);

        // Create initial snapshot
        UpdateConfigs(configs);

        _isInitialized = true;
      }
    }

    /// <summary>
    /// Updates the layer configurations at runtime.
    /// Thread-safe: can be called from UI thread.
    /// Changes take effect on next audio callback.
    /// </summary>
    /// <param name="configs">New configurations to apply.</param>
    public void UpdateConfigs(IReadOnlyList<LayerConfiguration> configs)
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (configs is null || configs.Count == 0)
        return;

      // Validate configurations
      foreach (LayerConfiguration config in configs)
        config.ValidateForSampleRate(_sampleRate);

      // Create immutable snapshot (records are immutable by design)
      LayerConfiguration[] snapshot = new LayerConfiguration[configs.Count];
      for (int i = 0; i < configs.Count; i++)
        snapshot[i] = configs[i];

      // Atomic swap (lock-free)
      _configSnapshot = snapshot;
      _configDirty = true;
    }

    /// <summary>
    /// Begins audio generation by triggering envelopes.
    /// Engine must be initialized first.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    public void Start()
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (!_isInitialized)
        throw new InvalidOperationException(
          "Engine must be initialized before starting. Call Initialize() first."
        );

      _isPlaying = true;
      _consecutiveErrorCount = 0; // Reset error counter
    }

    /// <summary>
    /// Stops audio generation by triggering release phase.
    /// Audio will fade out over the configured release time.
    /// </summary>
    public void Stop()
    {
      if (!_isPlaying)
        return;

      _isPlaying = false;
      _mixer.TriggerReleaseAll();
    }

    /// <summary>
    /// Fills a provided buffer with generated audio.
    /// This is the HOT PATH called by platform audio callback.
    /// Must be allocation-free and real-time safe.
    /// </summary>
    /// <param name="buffer">The span to fill with audio samples.</param>
    /// <remarks>
    /// CRITICAL: This method runs on a real-time audio thread.
    /// - No allocations allowed
    /// - No locks allowed
    /// - No I/O operations allowed
    /// - No logging/diagnostics allowed
    /// Errors are stored and reported asynchronously via events or polling.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void FillBuffer(Span<float> buffer)
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (!_isInitialized)
      {
        buffer.Clear();
        return;
      }

      if (!_isPlaying)
      {
        buffer.Clear();
        return;
      }

      try
      {
        // Lock-free config update check
        if (_configDirty)
          // Config was updated, flag is consumed
          _configDirty = false;

        // Render audio using snapshot (no locks needed)
        ReadOnlySpan<LayerConfiguration> configSpan = _configSnapshot.AsSpan();
        _mixer.Render(buffer, _sampleRate, configSpan);

        // Reset error counter on success
        _consecutiveErrorCount = 0;
      }
      catch (Exception ex)
      {
        // CRITICAL: Never throw from audio callback
        // Store error for later reporting (lock-free)
        _consecutiveErrorCount++;
        _lastError = ex; // Volatile write, thread-safe

        // Silence output to prevent speaker damage
        buffer.Clear();

        // Stop engine if errors persist
        if (_consecutiveErrorCount >= MaxConsecutiveErrors)
        {
          _isPlaying = false;
          _hasCriticalError = true;

          // Schedule error notification on background thread
          // NEVER log/raise events directly from audio callback
          ThreadPool.QueueUserWorkItem(_ => RaiseCriticalErrorAsync(ex));
        }
      }
    }

    /// <summary>
    /// Gets the current envelope value for a specific layer.
    /// Useful for UI metering and visual feedback.
    /// </summary>
    /// <param name="layerIndex">Zero-based layer index.</param>
    /// <returns>Envelope value (0.0 to 1.0).</returns>
    public float GetLayerEnvelopeValue(int layerIndex)
    {
      if (!_isInitialized)
        return 0.0f;

      return _mixer.GetLayerEnvelopeValue(layerIndex);
    }

    /// <summary>
    /// Checks if a critical error has occurred and returns it.
    /// This method is intended for periodic polling from UI thread.
    /// </summary>
    /// <returns>True if a critical error occurred; false otherwise.</returns>
    public bool TryGetCriticalError(out Exception? error)
    {
      if (_hasCriticalError && _lastError != null)
      {
        error = _lastError;
        return true;
      }

      error = null;
      return false;
    }

    /// <summary>
    /// Immediately silences the engine and resets all internal states.
    /// Use this for emergency stop, not normal playback stop.
    /// </summary>
    public void Reset()
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      Stop();
      _mixer.Reset();
    }

    /// <summary>
    /// Disposes the audio engine and releases all resources.
    /// </summary>
    public void Dispose()
    {
      if (_isDisposed)
        return;

      Stop();
      _mixer.Reset();
      _isDisposed = true;

      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Raises the CriticalError event asynchronously on a background thread.
    /// This ensures audio callback is not blocked by event handlers.
    /// </summary>
    private void RaiseCriticalErrorAsync(Exception error)
    {
      try
      {
        CriticalError?.Invoke(
          this,
          new AudioErrorEventArgs(error, _consecutiveErrorCount)
        );
      }
      catch
      {
        // Swallow exceptions in event handlers to prevent crashes
        // The error is already stored in LastError for polling
      }
    }

    /// <summary>
    /// Event arguments for critical audio errors.
    /// </summary>
    public sealed class AudioErrorEventArgs : EventArgs
    {
      /// <summary>
      /// Gets the exception that caused the critical error.
      /// </summary>
      public Exception Error { get; }

      /// <summary>
      /// Gets the number of consecutive errors that occurred before stopping.
      /// </summary>
      public int ConsecutiveErrorCount { get; }

      /// <summary>
      /// Gets the timestamp when the error was detected.
      /// </summary>
      public DateTime Timestamp { get; }

      /// <summary>
      /// Initializes a new instance of the <see cref="AudioErrorEventArgs"/> class.
      /// </summary>
      internal AudioErrorEventArgs(Exception error, int consecutiveErrorCount)
      {
        Error = error ?? throw new ArgumentNullException(nameof(error));
        ConsecutiveErrorCount = consecutiveErrorCount;
        Timestamp = DateTime.UtcNow;
      }

      /// <summary>
      /// Returns a string representation of the error event.
      /// </summary>
      public override string ToString() =>
        $"Audio error at {Timestamp:O}: {Error.Message} (after {ConsecutiveErrorCount} consecutive errors)";
    }
  }
}
