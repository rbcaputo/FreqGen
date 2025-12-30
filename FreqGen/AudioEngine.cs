namespace FreqGen.Core
{
  /// <summary>
  /// Main audio engine orchestrating the DSP graph.
  /// Thread-safe for parameter updates, real-time safe for audio generation.
  /// </summary>
  public sealed class AudioEngine
  {
    private readonly Mixer _mixer;
    private readonly float _sampleRate;
    private bool _isPlaying;

    /// <summary>
    /// Sample rate of this engine.
    /// </summary>
    public float SampleRate => _sampleRate;

    /// <summary>
    /// Number of available layers.
    /// </summary>
    public int LayerCount => _mixer.LayerCount;

    /// <summary>
    /// Check if engine is currently playing.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Check is all layers have faded out.
    /// </summary>
    public bool IsSilent => _mixer.AllSilent;

    /// <summary>
    /// Create an audio engine.
    /// </summary>
    /// <param name="sampleRate">Audio sample rate (typically 44100 or 48000)</param>
    /// <param name="layerCount">Number of simulataneous layers (typically 2-4)</param>
    public AudioEngine(float sampleRate = AudioSettings.SampleRate, int layerCount = 4)
    {
      if (sampleRate < 8000f || sampleRate > 192000f)
        throw new ArgumentException("Sample rate must be between 8000 and 192000 Hz", nameof(sampleRate));

      _mixer = new(layerCount, sampleRate);
      _sampleRate = sampleRate;
    }

    public void ConfigureLayer(int layerIndex, LayerConfig config)
    {
      if (!config.IsValid())
        throw new ArgumentException("Invalid layer configuration", nameof(config));

      Layer? layer = _mixer.GetLayer(layerIndex);

      layer.SetCarrierFrequency(config.CarrierHz);
      layer.SetModulation(config.ModulationHz, config.ModulationDepth);
      layer.Weight = config.Weight;
    }

    public void ConfigureLayers(LayerConfig[] configs)
    {
      ArgumentNullException.ThrowIfNull(configs);

      if (configs.Length > LayerCount)
        throw new ArgumentException($"Too many configs: {configs.Length}, max is {LayerCount}", nameof(configs));

      // Configure specified layers
      for (int i = 0; i < configs.Length; i++)
        ConfigureLayer(i, configs[i]);

      // Silence unused layers
      for (int i = 0; i < configs.Length; i++)
        _mixer.GetLayer(i).Weight = 0f;
    }

    /// <summary>
    /// Start audio generation (begin attack phase on all active layers).
    /// </summary>
    public void Start()
    {
      if (_isPlaying)
        return;

      _isPlaying = true;
      _mixer.StartAll();
    }

    /// <summary>
    /// Stop audio generation (begin release phase on all layers).
    /// </summary>
    public void Stop()
    {
      if (!_isPlaying)
        return;

      _isPlaying = false;
      _mixer.StopAll();
    }

    /// <summary>
    /// Fill an audio buffer with samples.
    /// MUST be called from audio thread only.
    /// </summary>
    /// <param name="buffer">Output buffer to fill</param>
    public void FillBuffer(float[] buffer)
    {
      ArgumentNullException.ThrowIfNull(buffer);

      for (int i = 0; i < buffer.Length; i++)
        buffer[i] = _mixer.NextSample();
    }

    /// <summary>
    /// Reset all layers to initial state (silence).
    /// Should only be called when audio is stopped.
    /// </summary>
    public void Reset()
    {
      _mixer.ResetAll();
      _isPlaying = false;
    }
  }
}
