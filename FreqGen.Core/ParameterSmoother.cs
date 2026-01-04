namespace FreqGen.Core
{
  /// <summary>
  /// Provides linear-to-exponential smoothing for parameters to prevent "zipper noise."
  /// </summary>
  public sealed class ParameterSmoother(float initialValue, float smoothingFactor = 0.001f)
  {
    private float _current = initialValue;
    private float _target = initialValue;

    /// <summary>
    /// 
    /// </summary>
    public float Value => _current;

    /// <summary>
    /// Sets a new target value for the parameter.
    /// </summary>
    public void SetTarget(float target) =>
      _target = target;

    /// <summary>
    /// Calculates the next smoothed value. Should be called per-sample or per-block.
    /// </summary>
    public float Step()
    {
      _current += (_target - _current) * smoothingFactor;
      return _current;
    }
  }
}
