using FreqGen.Presets.Models;

namespace FreqGen.Presets.Presets
{
  /// <summary>
  /// Brainwave entrainment presets using amplitude modulation.
  /// Based on established neural oscillation frequency ranges.
  /// All presets use gentle modulation depths suitable for beginners.
  /// </summary>
  public static class BrainwavePresets
  {
    /// <summary>
    /// Delta wave (2.0Hz) - Deep sleep and physical restoration.
    /// Dual-layer design with primary and supporting carrier.
    /// </summary>
    public static FrequencyPreset Bw_Delta_DeepRest => new()
    {
      ID = "bw_delta_01",
      DisplayName = "Deep Rest (Delta)",
      Description = "2.0Hz: Ideal for deep sleep, pain relief, and access to the unconscious mind. " +
                    "Promotes physical restoration and cellular regeneration.",
      Category = PresetCategory.Brainwave,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(30),
      Tags = ["sleep", "recovery", "deep", "restorative"],
      Layers =
        [
          LayerConfig.AmTone(174f, 2.0f, 0.6f, 0.7f, "Primary Delta carrier"),
          LayerConfig.AmTone(176f, 2.0f, 0.3f, 0.3f, "Supporting harmonic")
        ]
    };

    /// <summary>
    /// Theta wave (5.5Hz) - Deep meditation and creative visualization.
    /// The "twilight" state between waking and sleeping.
    /// </summary>
    public static FrequencyPreset Bw_Theta_Meditation => new()
    {
      ID = "bw_theta_01",
      DisplayName = "Deep Meditation (Theta)",
      Description = "5.5Hz: The 'twilight' state between sleep and wakefulness. " +
                    "Promotes deep meditation, vivid visualization, and enhanced creativity.",
      Category = PresetCategory.Brainwave,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["meditation", "creative", "visualization", "intuitive"],
      Layers =
        [
          LayerConfig.AmTone(220f, 5.5f, 0.7f, 0.8f, "Primary Theta carrier"),
          LayerConfig.AmTone(225f, 5.5f, 0.4f, 0.2f, "Harmonic enhancement")
        ]
    };

    /// <summary>
    /// Alpha wave (10.0Hz) - Relaxed focus and stress reduction.
    /// The "flow state" frequency for effortless concentration.
    /// </summary>
    public static FrequencyPreset Bw_Alpha_Relaxation => new()
    {
      ID = "bw_alpha_01",
      DisplayName = "Relaxed Focus (Alpha)",
      Description = "10.0Hz: Synchronizes the brain for 'flow states,' stress reduction, and relaxed learning. " +
                    "Perfect for light meditation and creative work.",
      Category = PresetCategory.Brainwave,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(25),
      Tags = ["focus", "relaxation", "stress-relief", "flow"],
      Layers =
        [
          LayerConfig.AmTone(330f, 10.0f, 0.6f, 1.0f, "Alpha wave carrier")
        ]
    };

    /// <summary>
    /// Beta wave (20.0Hz) - Enhanced alertness and analytical thinking.
    /// Stimulates cognitive processing and concentration.
    /// </summary>
    public static FrequencyPreset Bw_Beta_Cognition => new()
    {
      ID = "bw_beta_01",
      DisplayName = "High Alertness (Beta)",
      Description = "20.0Hz: Stimulates cognitive processing, concentration, and analytical thinking. " +
                    "Ideal for complex problem-solving and detailed work.",
      Category = PresetCategory.Brainwave,
      IsBeginnerFriendly = false, // Higher frequency, more stimulating
      RecommendedDuration = TimeSpan.FromMinutes(15),
      Tags = ["focus", "alertness", "cognitive", "analytical"],
      Layers =
        [
          LayerConfig.AmTone(440f, 20.0f, 0.5f, 1.0f, "Beta wave carrier")
        ]
    };

    /// <summary>
    /// Gamma wave (40.0Hz) - Peak cognitive performance and insight.
    /// Associated with heightened perception and information processing.
    /// </summary>
    public static FrequencyPreset Bw_Gamma_Insight => new()
    {
      ID = "bw_gamma_01",
      DisplayName = "Peak Perception (Gamma)",
      Description = "40.0Hz: Associated with high-level information processing and bursts of insight. " +
                    "May enhance learning and perceptual binding. Use for short sessions only.",
      Category = PresetCategory.Brainwave,
      IsBeginnerFriendly = false, // Advanced, use with caution
      RecommendedDuration = TimeSpan.FromMinutes(10),
      Tags = ["cognitive", "insight", "perception", "advanced"],
      Layers =
        [
          LayerConfig.AmTone(512f, 40.0f, 0.3f, 1.0f, "Gamma wave carrier")
        ]
    };

    /// <summary>
    /// Returns all brainwave presets in order from slowest to fastest.
    /// </summary>
    public static IReadOnlyList<FrequencyPreset> All =>
      [
        Bw_Delta_DeepRest,
        Bw_Theta_Meditation,
        Bw_Alpha_Relaxation,
        Bw_Beta_Cognition,
        Bw_Gamma_Insight
      ];
  }
}
