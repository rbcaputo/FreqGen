using System.Runtime.CompilerServices;
using ToneSync.Core.Exceptions;
using ToneSync.Core.Layers;

namespace ToneSync.Core.Engine
{
  /// <summary>
  /// The primary entry point for the ToneSync DSP engine.
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

    /// <summary>
    /// Indicates that a new configuration snapshot has been published.
    /// This flag is written by the UI thread and consumed by the audio thread without
    /// locks to ensure real-time safety.
    /// </summary>
    private volatile bool _configDirty;

    private float _outputGain = 1.0f;

    // State flags
    private bool _isInitialized;
    private bool _isPlaying;
    private bool _isDisposed;

    private ChannelMode _channelMode = ChannelMode.Mono;

    /// <summary>
    /// Target master gain applied to the final output stage.
    /// This value may change abruptly (e.g., when switching output profiles),
    /// but is always smoothed before being applied to audio samples.
    /// </summary>
    private volatile float _masterGain = 1.0f;

    /// <summary>
    /// Smoothed version of <see cref="_masterGain"/> used to prevent clicks and
    /// discontinuities when gain changes occur during playback.
    /// This value is updated incrementally inside the audio callback.
    /// </summary>
    private float _smoothedGain = 1.0f;

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
    /// Gets the current channel mode (Mono or Stereo).
    /// </summary>
    public ChannelMode ChannelMode => _channelMode;

    /// <summary>
    /// Linear output gain applied as the final stage before delivery to the platform.
    /// This must remain ≤ 1.0 to avoid speaker overload.
    /// </summary>
    public float OutputGain
    {
      get => _outputGain;
      set => _outputGain = Math.Clamp(value, 0f, 1f);
    }

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
    /// <param name="channelMode"></param>
    /// <param name="attackSeconds">Envelope attack time (default: 10s).</param>
    /// <param name="releaseSeconds">Envelope release time (default: 30s).</param>
    /// <exception cref="InvalidConfigurationException">Thrown if configs are invalid.</exception>
    public void Initialize(
      IReadOnlyList<LayerConfiguration> configs,
      ChannelMode channelMode = ChannelMode.Mono,
      float attackSeconds = AudioSettings.EnvelopeSettings.DefaultAttackSeconds,
      float releaseSeconds = AudioSettings.EnvelopeSettings.DefaultReleaseSeconds
    )
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (configs is null || configs.Count == 0)
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
        _channelMode = channelMode;

        // Initialize mixer with layer count
        _mixer.Initialize(
          configs.Count, _sampleRate,
          channelMode, attackSeconds,
          releaseSeconds
        );

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
    /// Sets the target master gain for the engine.
    /// Must be between 0.0 and 1.0. Applied smoothly in the audio callback.
    /// </summary>
    /// <param name="gain">Linear gain value (0.0 to 1.0).</param>
    public void SetMasterGain(float gain) =>
      _masterGain = Math.Clamp(gain, 0f, 1f);

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
    /// Fills a provided mono buffer with generated audio.
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
    /// The final output stage always applies master gain smoothing and a
    /// hard safety limiter, even if rendering succeeds.
    /// Errors are stored and reported asynchronously via events or polling.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void FillMonoBuffer(Span<float> buffer)
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (!_isInitialized || !_isPlaying)
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
        _mixer.RenderMono(buffer, _sampleRate, configSpan);

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

      // Apply master gain and safety limit
      ApplyMasterGainAndLimit(buffer);
    }

    /// <summary>
    /// Fills provided stereo buffers with generated audio (planar format).
    /// This is the HOT PATH called by platform audio callback.
    /// Must be allocation-free and real-time safe.
    /// </summary>
    /// <param name="leftBuffer">Left channel output buffer.</param>
    /// <param name="rightBuffer">Right channel output buffer.</param>
    /// <remarks>
    /// CRITICAL: This method runs on a real-time audio thread.
    /// - No allocations allowed
    /// - No locks allowed
    /// - No I/O operations allowed
    /// - No logging/diagnostics allowed
    /// The final output stage always applies master gain smoothing and a
    /// hard safety limiter, even if rendering succeeds.
    /// Errors are stored and reported asynchronously via events or polling.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void FillStereoBuffer(Span<float> leftBuffer, Span<float> rightBuffer)
    {
      ObjectDisposedException.ThrowIf(_isDisposed, this);

      if (_channelMode == ChannelMode.Mono)
        throw new InvalidOperationException(
          "Engine is configured for mono output. Use FillBuffer() instead."
        );

      if (leftBuffer.Length != rightBuffer.Length)
        throw new ArgumentException(
          "Left and right buffers must have the same length."
        );

      if (!_isInitialized || !_isPlaying)
      {
        leftBuffer.Clear();
        rightBuffer.Clear();
        return;
      }

      try
      {
        if (_configDirty)
          _configDirty = false;

        ReadOnlySpan<LayerConfiguration> configSpan = _configSnapshot.AsSpan();
        _mixer.RenderStereo(leftBuffer, rightBuffer, _sampleRate, configSpan);

        _consecutiveErrorCount = 0;
      }
      catch (Exception ex)
      {
        _consecutiveErrorCount++;
        _lastError = ex;
        leftBuffer.Clear();
        rightBuffer.Clear();

        if (_consecutiveErrorCount >= MaxConsecutiveErrors)
        {
          _isPlaying = false;
          _hasCriticalError = true;
          ThreadPool.QueueUserWorkItem(_ => RaiseCriticalErrorAsync(ex));
        }
      }

      // Apply master gain and safety limiting to both channels
      ApplyMasterGainAndLimit(leftBuffer);
      ApplyMasterGainAndLimit(rightBuffer);
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
    /// Immediately silences the engine and resets all internal DSP state.
    /// This bypasses envelope release and should only be used for
    /// emergency stop or fault recovery, not normal playback control.
    /// </summary>
    /// <remarks>
    /// This method is safe to call from the UI thread.
    /// It does not dispose the engine or release platform resources.
    /// </remarks>
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
    /// Applies smoothed master gain and safety limiting to a buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyMasterGainAndLimit(Span<float> buffer)
    {
      // Smooth master gain to avoid clicks
      const float gainSlew = 0.001f; // ~100ms response at 48kHz

      for (int i = 0; i < buffer.Length; i++)
      {
        _smoothedGain += (_masterGain - _smoothedGain) * gainSlew;

        float sample = buffer[i] * _smoothedGain;

        // Absolute safety ceiling (never remove)
        buffer[i] = Math.Clamp(sample, -0.999f, 0.999f);

        buffer[i] *= _outputGain;
      }
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
