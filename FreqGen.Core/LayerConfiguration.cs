using FreqGen.Core.Exceptions;

namespace FreqGen.Core
{
  /// <summary>
  /// Immutable, validated configuration data for a single audio layer.
  /// All parameters are guaranteed to be within safe operating ranges.
  /// </summary>
  public sealed record LayerConfiguration
  {
    private readonly float _carrierFrequency;
    private readonly float _modulatorFrequency;
    private readonly float _modulatorDepth;
    private readonly float _weight;

    /// <summary>
    /// Gets the carrier (audible tone) frequency in Hz.
    /// </summary>
    public float CarrierFrequency
    {
      get => _carrierFrequency;
      init
      {
        if (!AudioSettings.CarrierSettings.IsValid(value, AudioSettings.SampleRate))
          throw new InvalidConfigurationException(
            $"Carrier frequency {value} Hz is outside valid range " +
            $"({AudioSettings.CarrierSettings.Minimum}-{AudioSettings.CarrierSettings.Maximum} Hz) " +
            $"or too close to Nyquist frequency.",
            nameof(CarrierFrequency)
          );

        _carrierFrequency = value;
      }
    }

    /// <summary>
    /// Gets the modulation (LFO) frequency in Hz.
    /// Set to 0 for pure tone (no modulation).
    /// </summary>
    public float ModulatorFrequency
    {
      get => _modulatorFrequency;
      init
      {
        if (value == 0.0f)
        {
          _modulatorFrequency = 0.0f;
          return;
        }

        if (!AudioSettings.ModulationSettings.IsValid(value))
          throw new InvalidConfigurationException(
            $"Modulator frequency {value} Hz is outside valid range " +
            $"({AudioSettings.ModulationSettings.Minimum}-{AudioSettings.ModulationSettings.Maximum}Hz).",
            nameof(ModulatorFrequency)
          );

        _modulatorFrequency = value;
      }
    }

    /// <summary>
    /// Gets the modulation depth (0.0 = no modulation, 1.0 = full modulation).
    /// </summary>
    public float ModulatorDepth
    {
      get => _modulatorDepth;
      init
      {
        if (!AudioSettings.AmplitudeSettings.IsValid(value))
          throw new InvalidConfigurationException(
            $"Modulator depth {value} is outside valid range " +
            $"({AudioSettings.AmplitudeSettings.Minimum}-{AudioSettings.AmplitudeSettings.Maximum}).",
            nameof(ModulatorDepth)
          );

        _modulatorDepth = value;
      }
    }

    /// <summary>
    /// Gets the layer output weight/volume (0.0 = silent, 1.0 = full volume).
    /// </summary>
    public float Weight
    {
      get => _weight;
      init
      {
        if (!AudioSettings.AmplitudeSettings.IsValid(value))
          throw new InvalidConfigurationException(
            $"Layer weight {value} is outside valid range " +
            $"({AudioSettings.AmplitudeSettings.Minimum}-{AudioSettings.AmplitudeSettings.Maximum}).",
            nameof(Weight)
          );

        _weight = value;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this layer should be rendered.
    /// Inactive layers are skipped entirely to save CPU.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Validates this configuration against the current sample rate.
    /// </summary>
    /// <param name="sampleRate">The system's audio sample rate.</param>
    /// <exception cref="InvalidConfigurationException">Thrown if validation fails.</exception>
    public void ValidateForSampleRate(float sampleRate)
    {
      if (!AudioSettings.CarrierSettings.IsValid(_carrierFrequency, sampleRate))
        throw new InvalidConfigurationException(
          $"Carrier frequency {_carrierFrequency} Hz exceeds Nyquist limit " +
          $"for sample rate {sampleRate}Hz.",
          nameof(CarrierFrequency)
        );
    }
  }
}
