namespace FreqGen.Core.Nodes
{
  /// <summary>
  /// Base interface for all audio processing nodes.
  /// Each node produces one sample per tick at audio rate.
  /// </summary>
  public interface IAudioNode
  {
    /// <summary>
    /// Generate the next audio sample.
    /// </summary>
    /// <returns>Audio sample in range [-1.0, 1.0]</returns>
    public float NextSample();
  }
}
