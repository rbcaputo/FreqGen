namespace FreqGen.Core
{
  public sealed class ParameterSmoother(float smoothingTime, float sampleRate)
  {
    private readonly float _smoothingCoef = 1f - MathF.Exp(-1f / (smoothingTime * sampleRate));
    private float _current;
    private float _target;

    public float Current => _current;

    public void SetTarget(float value) =>
      _target = value;

    public float Next()
    {
      _current += (_target - _current) * _smoothingCoef;

      return _current;
    }
  }
}
