namespace ToneSync.Core.Engine
{
  /// <summary>
  /// Audio channel configuration modes.
  /// </summary>
  public enum ChannelMode
  {
    /// <summary>
    /// Single channel output (default).
    /// </summary>
    Mono = 1,

    /// <summary>
    /// Two-channel output (left and right).
    /// </summary>
    Stereo = 2
  }

  /// <summary>
  /// Channel configuration settings.
  /// </summary>
  public static class ChannelSettings
  {
    /// <summary>
    /// Default channel mode.
    /// </summary>
    public const ChannelMode Default = ChannelMode.Mono;

    /// <summary>
    /// Gets the number of channels for a given mode.
    /// </summary>
    public static int GetChannelCount(ChannelMode mode) => (int)mode;
  }
}
