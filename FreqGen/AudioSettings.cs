namespace FreqGen.Core
{
  public static class AudioSettings
  {
    public const int SampleRate = 44100;
    public const int BufferSize = 1024;

    public const double MinFrequency = 20.0;
    public const double MaxFrequency = SampleRate / 2.0;
  }
}
