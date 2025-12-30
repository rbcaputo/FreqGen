namespace FreqGen.Core
{
  public sealed class AudioEngine(double frequency = 440.0)
  {
    private readonly SineOscillator _oscillator = new(
      ValidateFrequency(frequency)
    );

    public void SetFrequency(double frequency) =>
      _oscillator.SetFrequency(
        ValidateFrequency(frequency)
      );

    public void FillBuffer(float[] buffer)
    {
      for (int i = 0; i < buffer.Length; i++)
        buffer[i] = _oscillator.NextSample();
    }

    private static double ValidateFrequency(double frequency)
    {
      if (double.IsNaN(frequency) || double.IsInfinity(frequency))
        return 440.0;

      if (frequency < AudioSettings.MinFrequency)
        return AudioSettings.MinFrequency;

      if (frequency > AudioSettings.MaxFrequency)
        return AudioSettings.MaxFrequency;

      return frequency;
    }
  }
}
