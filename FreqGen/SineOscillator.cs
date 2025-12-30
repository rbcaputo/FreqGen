namespace FreqGen.Core
{
  public sealed class SineOscillator
  {
    private const double TwoPi = Math.PI * 2.0;

    private double _phase;
    private double _phaseIncrement;

    public SineOscillator(double frequency) =>
      SetFrequency(frequency);

    public void SetFrequency(double frequency) =>
      _phaseIncrement = TwoPi * frequency / AudioSettings.SampleRate;

    public float NextSample()
    {
      float sample = (float)Math.Sin(_phase);

      _phase += _phaseIncrement;
      if (_phase >= TwoPi)
        _phase -= TwoPi;

      return sample;
    }
  }
}
