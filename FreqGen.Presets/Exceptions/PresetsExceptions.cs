namespace FreqGen.Presets.Exceptions
{
  /// <summary>
  /// Base exception for all FreqGen.Presets errors.
  /// </summary>
  public class PresetException(string message, Exception? inner = null)
    : Exception(message, inner);

  /// <summary>
  /// 
  /// </summary>
  public class PresetValidationException(string message, string paramName)
    : PresetException($"{message} Parameter: {paramName}");
}
