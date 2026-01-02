using System.Runtime.CompilerServices;

namespace FreqGen.Core
{
  /// <summary>
  /// Sums multiple audio layers into a single output stream with safety clamping.
  /// Pre-allocates all layers to avoid runtime allocations in audio callback.
  /// Implements headroom management to prevent clipping.
  /// </summary>
  public sealed class Mixer
  {
    // Pre-allocated layer pool (fixed size, no dynamic growth)
    private readonly Layer[] _layers = new Layer[AudioSettings.MaxLayers];

    // Pre-allocated temp buffer for per-layer rendering
    private float[] _tempBuffer = new float[AudioSettings.MaxBufferSize];

    // Current number of active layers
    private int _activeLayerCount;

    /// <summary>
    /// Gets the current number of active layers.
    /// </summary>
    public int ActiveLayerCount => _activeLayerCount;

    /// <summary>
    /// Initializes the mixer with a fixed number of layers.
    /// Must be called once before first use.
    /// </summary>
    /// <param name="layerCount">Number of layers to allocate (max 8).</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="attackSeconds">Envelope attack time for all layers.</param>
    /// <param name="releaseSeconds">Envelope release time for all layers.</param>
    /// <exception cref="ArgumentException">Thrown if layerCount exceeds max.</exception>
    public void Initialize(
      int layerCount, float sampleRate,
      float attackSeconds = AudioSettings.Envelope.DefaultAttackSeconds,
      float releaseSeconds = AudioSettings.Envelope.DefaultReleaseSeconds
    )
    {
      if (layerCount <= 0 || layerCount > AudioSettings.MaxLayers)
        throw new ArgumentException(
          $"Layer count must be between 1 and {AudioSettings.MaxLayers}. Got: {layerCount}",
          nameof(layerCount));

      _activeLayerCount = layerCount;

      // Pre-allocate all layers
      for (int i = 0; i < layerCount; i++)
      {
        _layers[i] = new Layer();
        _layers[i].Initialize(sampleRate, attackSeconds, releaseSeconds);
      }
    }

    /// <summary>
    /// Generates a mixed audio block from all active layers.
    /// This is the HOT PATH - must be allocation-free and deterministic.
    /// </summary>
    /// <param name="outputBuffer">The buffer to fill with the final mixed signal.</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="configs">Read-only snapshot of layer configurations.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Render(
      Span<float> outputBuffer,
      float sampleRate,
      ReadOnlySpan<LayerConfiguration> configs
    )
    {
      // Clear output buffer
      outputBuffer.Clear();

      // Resize temp buffer if needed (should only happen once at startup)
      if (_tempBuffer.Length < outputBuffer.Length)
        _tempBuffer = new float[outputBuffer.Length];

      // Render each active layer
      int layersToRender = Math.Min(_activeLayerCount, configs.Length);

      for (int i = 0; i < layersToRender; i++)
      {
        Span<float> tempSpan = _tempBuffer.AsSpan(0, outputBuffer.Length);
        tempSpan.Clear();

        // Render layer into temp buffer
        _layers[i].UpdateAndProcess(tempSpan, sampleRate, configs[i]);

        // Mix into output buffer
        MixBuffers(outputBuffer, tempSpan);
      }

      // Apply soft limiting to prevent clipping
      ApplySoftLimiter(outputBuffer);
    }

    /// <summary>
    /// Mixes one buffer into another (additive mixing).
    /// Inlined for performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MixBuffers(Span<float> destination, ReadOnlySpan<float> source)
    {
      for (int i = 0; i < destination.Length; i++)
        destination[i] += source[i];
    }

    /// <summary>
    /// Applies soft limiting with headroom to prevent harsh clipping.
    /// Uses tanh-style soft clipping for musical saturation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ApplySoftLimiter(Span<float> buffer)
    {
      const float headroom = 0.95f; // -0.44 dB headroom
      const float threshold = 0.8f;  // Start soft clipping at -1.94 dB

      for (int i = 0; i < buffer.Length; i++)
      {
        float sample = buffer[i];

        // Apply headroom scaling
        sample *= headroom;

        // Soft clip if above threshold
        if (MathF.Abs(sample) > threshold)
          // Tanh soft clipping (smooth saturation)
          sample = MathF.Tanh(sample * 1.5f);

        // Hard safety clamp (should never trigger if soft clip works)
        buffer[i] = Math.Clamp(sample, -1.0f, 1.0f);
      }
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

      return _layers[layerIndex].CurrentEnvelopeValue;
    }

    /// <summary>
    /// Triggers release phase for all layers.
    /// Should be called when stopping playback.
    /// </summary>
    public void TriggerReleaseAll()
    {
      for (int i = 0; i < _activeLayerCount; i++)
        _layers[i].TriggerRelease();
    }

    /// <summary>
    /// Resets all layers to prevent clicks on restart.
    /// Should be called when audio engine is fully stopped.
    /// </summary>
    public void Reset()
    {
      for (int i = 0; i < _activeLayerCount; i++)
        _layers[i].Reset();
    }
  }
}
