using System.Runtime.CompilerServices;
using ToneSync.Core.Engine;
using ToneSync.Core.Layers;

namespace ToneSync.Core
{
  /// <summary>
  /// Sums multiple audio layers into stereo or mono output buffers.
  /// Supports both mono and stereo layers with automatic upmixing.
  /// Uses planar (non-interleaved) buffer format for easier processing and flexibility.
  /// All layers are pre-allocated to ensure allocation-free execution inside the
  /// real-time audio callback.
  /// 
  /// This class performs pure additive mixing only.
  /// Gain staging, normalization, and safety limiting are handled by
  /// higher-level components.
  /// </summary>
  public sealed class Mixer
  {
    /// <summary>
    /// Fixed attenuation applied after summing all active layers.
    /// 
    /// This reserves headroom to prevent clipping when multiple layers,
    /// envelopes, and modulators are active simultaneously.
    /// 
    /// A value of 0.5 corresponds to -6 dB of headroom.
    /// </summary>
    private const float MixHeadroom = 0.5f;

    // Layer pools

    /// <summary>
    /// Fixed-size pool of audio layers used for rendering.
    /// The array is allocated once and never resized to guarantee
    /// deterministic behavior on the audio thread.
    /// </summary>
    private readonly MonoLayer[] _monoLayers = new MonoLayer[AudioSettings.MaxLayers];

    private readonly StereoLayer[] _stereoLayers = new StereoLayer[AudioSettings.MaxLayers];

    // Temporary rendering buffers (planar format)

    /// <summary>
    /// Temporary buffer used for rendering individual layers before
    /// summing them into the final output buffer.
    /// This buffer may be resized during initialization but is
    /// never allocated during steady-state audio processing.
    /// </summary>
    private float[] _monoTempBuffer = new float[AudioSettings.MaxBufferSize];

    private float[] _leftTempBuffer = new float[AudioSettings.MaxBufferSize];

    private float[] _rightTempBuffer = new float[AudioSettings.MaxBufferSize];

    /// <summary>
    /// Number of currently active layers initialized in the mixer.
    /// This value is fixed after initialization and
    /// read frequently by the audio thread.
    /// </summary>
    private int _activeLayerCount;

    private ChannelMode _outputMode = ChannelMode.Mono;

    /// <summary>
    /// Gets the current number of active layers.
    /// </summary>
    public int ActiveLayerCount => _activeLayerCount;

    /// <summary>
    /// 
    /// </summary>
    public ChannelMode OutputMode => _outputMode;

    /// <summary>
    /// Initializes the mixer with specified channel mode.
    /// Must be called once before first use.
    /// </summary>
    /// <param name="layerCount">Number of layers to allocate (max 8).</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="outputMode"></param>
    /// <param name="attackSeconds">Envelope attack time for all layers.</param>
    /// <param name="releaseSeconds">Envelope release time for all layers.</param>
    /// <remarks>
    /// This method must be called exactly once before the mixer is used.
    /// It is not thread-safe and must not be called while audio rendering is in progress.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if layerCount exceeds max.</exception>
    public void Initialize(
      int layerCount, float sampleRate,
      ChannelMode outputMode = ChannelMode.Mono,
      float attackSeconds = AudioSettings.EnvelopeSettings.DefaultAttackSeconds,
      float releaseSeconds = AudioSettings.EnvelopeSettings.DefaultReleaseSeconds
    )
    {
      if (layerCount <= 0 || layerCount > AudioSettings.MaxLayers)
        throw new ArgumentException(
          $"Layer count must be between 1 and {AudioSettings.MaxLayers}. Got: {layerCount}",
          nameof(layerCount));

      _activeLayerCount = layerCount;
      _outputMode = outputMode;

      // Pre-allocate all layers
      for (int i = 0; i < layerCount; i++)
      {
        _monoLayers[i] = new MonoLayer();
        _monoLayers[i].Initialize(sampleRate, attackSeconds, releaseSeconds);

        _stereoLayers[i] = new StereoLayer();
        _stereoLayers[i].Initialize(sampleRate, attackSeconds, releaseSeconds);
      }
    }

    /// <summary>
    /// Renders and mixes all active audio mono layers into the provided output buffer.
    /// </summary>
    /// <param name="outputBuffer">
    /// Destination buffer that will contain the summed audio signal.
    /// The buffer is always fully written.
    /// </param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="configs">Read-only snapshot of per-layer configuration data.</param>
    /// <remarks>
    /// This method is part of the real-time audio path:
    /// - No locks
    /// - No allocations during steady-state operation
    /// - Deterministic execution time
    ///
    /// The mixer performs additive summation only.
    /// No gain normalization, limiting, or safety clamping is applied here.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void RenderMono(
      Span<float> outputBuffer,
      float sampleRate,
      ReadOnlySpan<LayerConfiguration> configs
    )
    {
      if (_outputMode == ChannelMode.Stereo)
        throw new InvalidOperationException(
          "Mixer is configured for stereo output. Use RenderStereo() instead."
        );

      // Clear output buffer
      outputBuffer.Clear();

      // Resize temp buffer if needed (should only happen once at startup)
      if (_monoTempBuffer.Length < outputBuffer.Length)
        _monoTempBuffer = new float[outputBuffer.Length];

      // Render each active layer
      int layersToRender = Math.Min(_activeLayerCount, configs.Length);

      for (int i = 0; i < layersToRender; i++)
      {
        Span<float> tempSpan = _monoTempBuffer.AsSpan(0, outputBuffer.Length);
        tempSpan.Clear();

        // Render layer into temporary buffer
        _monoLayers[i].UpdateAndProcess(tempSpan, sampleRate, configs[i]);

        // Additively mix into output buffer
        MixBuffers(outputBuffer, tempSpan);
      }

      // Apply fixed mix headroom attenuation
      ApplyHeadroom(outputBuffer);
    }

    /// <summary>
    /// Renders audio in stereo format using planar (non-interleaved) buffers.
    /// Automatically upmixes mono layers using constant-power panning.
    /// </summary>
    /// <param name="leftBuffer">Left channel output buffer.</param>
    /// <param name="rightBuffer">Right channel output buffer.</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="configs">Read-only snapshot of per-layer configuration data.</param>
    /// <remarks>
    /// This method is part of the real-time audio path:
    /// - No locks
    /// - No allocations during steady-state operation
    /// - Deterministic execution time
    ///
    /// The mixer performs additive summation only.
    /// No gain normalization, limiting, or safety clamping is applied here.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void RenderStereo(
      Span<float> leftBuffer,
      Span<float> rightBuffer,
      float sampleRate,
      ReadOnlySpan<LayerConfiguration> configs
    )
    {
      if (leftBuffer.Length != rightBuffer.Length)
        throw new ArgumentException(
          "Left and right buffers must have the same length."
        );

      leftBuffer.Clear();
      rightBuffer.Clear();

      int bufferSize = leftBuffer.Length;

      // Resize temp buffers if needed
      if (_monoTempBuffer.Length < bufferSize)
      {
        _monoTempBuffer = new float[bufferSize];
        _leftTempBuffer = new float[bufferSize];
        _rightTempBuffer = new float[bufferSize];
      }

      int layersToRender = Math.Min(_activeLayerCount, configs.Length);

      for (int i = 0; i < layersToRender; i++)
      {
        LayerConfiguration config = configs[i];

        if (config.ChannelMode == ChannelMode.Stereo)
          // Render stereo layer
          RenderStereoLayer(i, leftBuffer, rightBuffer, bufferSize, sampleRate, config);
        else
          // Render mono layer and pan to stereo
          RenderMonoLayerToStereo(i, leftBuffer, rightBuffer, bufferSize, sampleRate, config);
      }

      // Apply headroom to both channels
      ApplyHeadroom(leftBuffer);
      ApplyHeadroom(rightBuffer);
    }

    /// <summary>
    /// Renders a stereo layer with frequency offset (for binaural beats).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderStereoLayer(
      int layerIndex,
      Span<float> leftBuffer,
      Span<float> rightBuffer,
      int bufferSize,
      float sampleRate,
      LayerConfiguration config
    )
    {
      Span<float> leftTempBuffer = _leftTempBuffer.AsSpan(0, bufferSize);
      Span<float> rightTempBuffer = _rightTempBuffer.AsSpan(0, bufferSize);

      leftTempBuffer.Clear();
      rightTempBuffer.Clear();

      // Render both channels
      _stereoLayers[layerIndex].UpdateAndProcess(
        leftTempBuffer, rightTempBuffer,
        sampleRate, config
      );

      // Mix into output buffers
      MixBuffers(leftBuffer, leftTempBuffer);
      MixBuffers(rightBuffer, rightTempBuffer);
    }

    /// <summary>
    /// Adds the contents of one buffer into another using sample-wise addition.
    /// Assumes both buffers are the same length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MixBuffers(Span<float> destination, ReadOnlySpan<float> source)
    {
      for (int i = 0; i < destination.Length; i++)
        destination[i] += source[i];
    }

    /// <summary>
    /// Applies fixed post-mix attenuation to reserve headroom
    /// before device-specific gain and safety limiting.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyHeadroom(Span<float> buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
        buffer[i] *= MixHeadroom;
    }

    /// <summary>
    /// Gets the current envelope value for a specific layer.
    /// Useful for UI metering.
    /// </summary>
    /// <param name="layerIndex">Zero-based layer index.</param>
    /// <returns>Envelope value (0.0 to 1.0), or 0.0 if index is invalid.</returns>
    public float GetLayerEnvelopeValue(int layerIndex)
    {
      if (layerIndex < 0 || layerIndex >= _activeLayerCount)
        return 0.0f;

      // Return envelope from whichever layer type is being used
      // In stereo mode, stereo layers take precedence
      return _outputMode == ChannelMode.Stereo
        ? _stereoLayers[layerIndex].CurrentEnvelopeValue
        : _monoLayers[layerIndex].CurrentEnvelopeValue;
    }

    /// <remarks>
    /// This method does not stop audio immediately.
    /// Each layer will fade out according to its configured release time.
    /// </remarks>
    public void TriggerReleaseAll()
    {
      for (int i = 0; i < _activeLayerCount; i++)
      {
        _monoLayers[i].TriggerRelease();
        _stereoLayers[i].TriggerRelease();
      }
    }

    /// <summary>
    /// Resets all layers to prevent clicks on restart.
    /// Should be called when audio engine is fully stopped.
    /// </summary>
    /// <remarks>
    /// This method resets internal oscillator and envelope state.
    /// It should only be called when the audio engine is fully stopped,
    /// as it may cause discontinuities if used during active playback.
    /// </remarks>
    public void Reset()
    {
      for (int i = 0; i < _activeLayerCount; i++)
      {
        _monoLayers[i].Reset();
        _stereoLayers[i].Reset();
      }
    }

    /// <summary>
    /// Renders a mono layer and pans it to stereo using constant-power panning.
    /// Pan = -1.0 (full left), 0.0 (center), +1.0 (full right)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderMonoLayerToStereo(
      int layerIndex,
      Span<float> leftBuffer,
      Span<float> rightBuffer,
      int bufferSize,
      float sampleRate,
      LayerConfiguration config
    )
    {
      Span<float> monoTempBuffer = _monoTempBuffer.AsSpan(0, bufferSize);

      monoTempBuffer.Clear();

      // Render mono channel
      _monoLayers[layerIndex].UpdateAndProcess(monoTempBuffer, sampleRate, config);

      // Calculate constant-power pan gains
      // Formula: Left = cos(pan * π/4), Right = sin(pan * π/4)
      // This ensures constant energy: L^2 + R^2 = 1
      float panAngle = (config.Pan + 1.0f) * 0.25f * MathF.PI; // Map [-1, 1] to [0, π/2]
      float leftGain = MathF.Cos(panAngle);
      float rightGain = MathF.Sin(panAngle);

      // Pan and mix into output buffers
      for (int i = 0; i < bufferSize; i++)
      {
        float sample = monoTempBuffer[i];

        leftBuffer[i] += sample * leftGain;
        rightBuffer[i] += sample * rightGain;
      }
    }
  }
}
