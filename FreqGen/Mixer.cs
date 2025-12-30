using FreqGen.Core.Nodes;

namespace FreqGen.Core
{
  /// <summary>
  /// Mixes multiple audio layers into a single output.
  /// Applies headroom control and soft limiting.
  /// </summary>
  public sealed class Mixer : IAudioNode
  {
    private readonly Layer[] _layers;
    private readonly float _headroom;

    /// <summary>
    /// Number of layers in this mixer.
    /// </summary>
    public int LayerCount => _layers.Length;

    public bool AllSilent
    {
      get
      {
        for (int i = 0; i < LayerCount; i++)
          if (!_layers[i].IsSilent)
            return false;

        return true;
      }
    }

    /// <summary>
    /// Create a mixer with specified number of layers.
    /// </summary>
    /// <param name="layerCount">Number of layers to mix</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <param name="headroom">Output headroom multiplier (default 0.8 = -1.9 dB)</param>
    public Mixer(int layerCount, float sampleRate, float headroom = 0.8f)
    {
      if (layerCount < 1)
        throw new ArgumentException("Layer count must be at least 1", nameof(layerCount));

      if (headroom <= 0f || headroom > 1f)
        throw new ArgumentException("Headroom must be in range (0, 1]", nameof(headroom));

      _layers = new Layer[layerCount];
      _headroom = headroom;

      for (int i = 0; i < layerCount; i++)
        _layers[i] = new(sampleRate);
    }

    /// <summary>
    /// Get a specific layer for configuration.
    /// </summary>
    public Layer GetLayer(int index)
    {
      if (index < 0 || index >= _layers.Length)
        throw new ArgumentOutOfRangeException(nameof(index));


      return _layers[index];
    }

    /// <summary>
    /// Generate next mixed sample (audio thread only).
    /// </summary>
    /// <returns></returns>
    public float NextSample()
    {
      float sum = 0f;

      for (int i = 0; i < _layers.Length; i++)
        sum += _layers[i].NextSample();

      // Apply headroom and soft clipping
      sum *= _headroom;
      return Math.Clamp(sum, -1f, 1f);
    }

    /// <summary>
    /// Start all layers.
    /// </summary>
    public void StartAll()
    {
      for (int i = 0; i < _layers.Length; i++)
        _layers[i].Start();
    }

    /// <summary>
    /// Stop all layers.
    /// </summary>
    public void StopAll()
    {
      for (int i = 0; i < _layers.Length; i++)
        _layers[i].Stop();
    }

    /// <summary>
    /// Reset all layers.
    /// </summary>
    public void ResetAll()
    {
      for (int i = 0; i < _layers.Length; i++)
        _layers[i].Reset();
    }
  }
}
