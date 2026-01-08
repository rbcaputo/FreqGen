namespace ToneSync.Core.Layers
{
  /// <summary>
  /// Manages stereo rendering by coordinating two independent layers
  /// with frequency offset for binaural beat effects.
  /// </summary>
  public sealed class StereoLayer
  {
    private readonly MonoLayer _leftChannel = new();
    private readonly MonoLayer _rightChannel = new();

    /// <summary>
    /// Gets the current envelope value (average of both channels).
    /// </summary>
    public float CurrentEnvelopeValue =>
      _leftChannel.CurrentEnvelopeValue + _rightChannel.CurrentEnvelopeValue;

    /// <summary>
    /// Initializes both channels with the same envelope settings.
    /// </summary>
    public void Initialize(
      float sampleRate,
      float attackSerconds,
      float relesaseSeconds
    )
    {
      _leftChannel.Initialize(sampleRate, attackSerconds, relesaseSeconds);
      _rightChannel.Initialize(sampleRate, attackSerconds, relesaseSeconds);
    }

    /// <summary>
    /// Renders both left and right channels into separate buffers.
    /// </summary>
    /// <param name="leftBuffer">Destination buffer for left channel.</param>
    /// <param name="rightBuffer">Destination buffer for right channel.</param>
    /// <param name="sampleRate">System audio sample rate.</param>
    /// <param name="config">Layer configuration with stereo parameters.</param>
    public void UpdateAndProcess(
      Span<float> leftBuffer,
      Span<float> rightBuffer,
      float sampleRate,
      LayerConfiguration config
    )
    {
      if (!config.IsActive)
      {
        leftBuffer.Clear();
        rightBuffer.Clear();
        return;
      }

      // Create left channel configuration
      LayerConfiguration leftConfig = config with
      {
        CarrierFrequency = config.CarrierFrequency
      };

      // Create right channel configuration with frequency offset
      LayerConfiguration rightConfig = config with
      {
        CarrierFrequency = config.CarrierFrequency + config.StereoFrequencyOffset
      };

      // Render both channels independently
      _leftChannel.UpdateAndProcess(leftBuffer, sampleRate, leftConfig);
      _rightChannel.UpdateAndProcess(rightBuffer, sampleRate, rightConfig);
    }

    /// <summary>
    /// Triggers release phase on both channels.
    /// </summary>
    public void TriggerRelease()
    {
      _leftChannel.TriggerRelease();
      _rightChannel.TriggerRelease();
    }

    /// <summary>
    /// Resets both channels.
    /// </summary>
    public void Reset()
    {
      _leftChannel.Reset();
      _rightChannel.Reset();
    }
  }
}
