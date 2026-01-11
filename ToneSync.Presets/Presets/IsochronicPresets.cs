using ToneSync.Presets.Models;

namespace ToneSync.Presets.Presets
{
  /// <summary>
  /// Isochronic tone presets using high-depth amplitude modulation for rhythmic pulsing.
  /// 
  /// IMPLEMENTATION NOTE:
  /// These presets use maximum-depth amplitude modulation (0.9-1.0) with sine wave carriers
  /// to create isochronic-like pulses. True isochronic tones use square wave gating,
  /// but high-depth sine wave AM produces similar entrainment effects with less
  /// harmonic distortion and reduced listening fatigue.
  /// 
  /// Isochronic tones are more intense than gentle brainwave AM and should be
  /// used for shorter sessions (10-20 minutes).
  /// </summary>
  public static class IsochronicPresets
  {
    /// <summary>
    /// Delta range (3Hz) - Rapid physical recovery and deep rest.
    /// Sharp pulsing for power naps and restorative breaks.
    /// </summary>
    public static FrequencyPreset Ic_Delta_PowerNap => new()
    {
      ID = "ic_delta_01",
      DisplayName = "PowerNap (3Hz)",
      Description = "Sharp 3Hz pulses for rapid physical recovery and restorative rest. " +
                    "Ideal for 15-20 minute power naps to quickly recharge energy levels.",
      Category = PresetCategory.Isochronic,
      IsBeginnerFriendly = false, // Intense pulsing
      RecommendedDuration = TimeSpan.FromMinutes(15),
      Tags = ["nap", "recovery", "restorative", "intense"],
      Layers =
        [
          LayerConfig.AmTone(120f, 3.0f, 0.95f, description: "Delta pulse carrier")
        ]
    };

    /// <summary>
    /// Theta range (6Hz) - Creative surge and deep problem-solving.
    /// Rhythmic pulsing to bypass analytical filters and access creativity.
    /// </summary>
    public static FrequencyPreset Ic_Theta_Creative => new()
    {
      ID = "ic_theta_01",
      DisplayName = "Creative Surge (6Hz)",
      Description = "Rhythmic pulsing at 6Hz to bypass analytical filters and access deep creativity. " +
                    "Excellent for brainstorming, artistic work, and innovative problem-solving.",
      Category = PresetCategory.Isochronic,
      IsBeginnerFriendly = false,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["creative", "brainstorming", "innovation", "theta"],
      Layers =
        [
          LayerConfig.AmTone(180f, 6.0f, 0.9f, description: "Theta pulse carrier")
        ]
    };

    /// <summary>
    /// Alpha range (10Hz) - Deep stress release and mental clarity.
    /// Powerful pulsing for rapid stress reduction and clearing mental fog.
    /// </summary>
    public static FrequencyPreset Ic_Alpha_Unwind => new()
    {
      ID = "ic_alpha_01",
      DisplayName = "Deep Unwind (10Hz)",
      Description = "Powerful 10Hz pulses for rapid stress release and clearing mental fog. " +
                    "Use after intense work sessions or during high-stress periods.",
      Category = PresetCategory.Isochronic,
      IsBeginnerFriendly = false,
      RecommendedDuration = TimeSpan.FromMinutes(15),
      Tags = ["stress-relief", "clarity", "alpha", "unwinding"],
      Layers =
        [
          LayerConfig.AmTone(240f, 10.0f, 0.9f, description: "Alpha pulse carrier")
        ]
    };

    /// <summary>
    /// Beta range (15Hz) - High-intensity focus and concentration.
    /// Intense pulsing for demanding cognitive work and study sessions.
    /// </summary>
    public static FrequencyPreset Ic_Beta_Focus => new()
    {
      ID = "ic_beta_01",
      DisplayName = "High Focus (15Hz)",
      Description = "Intense 15Hz pulsing for study, coding, or high-performance cognitive work. " +
                    "Maximum concentration and alertness. Use for short bursts only.",
      Category = PresetCategory.Isochronic,
      IsBeginnerFriendly = false, // Very intense
      RecommendedDuration = TimeSpan.FromMinutes(15),
      Tags = ["focus", "study", "concentration", "cognitive", "beta"],
      Layers =
        [
          LayerConfig.AmTone(300f, 15.0f, 1.0f, description: "Beta pulse carrier")
        ]
    };

    /// <summary>
    /// SMR range (13Hz) - Sensorimotor rhythm for calm alertness.
    /// Balanced state between relaxation and focus, ideal for sustained work.
    /// </summary>
    public static FrequencyPreset Ic_SMR_CalmAlert => new()
    {
      ID = "ic_smr_01",
      DisplayName = "Calm Alertness (13Hz)",
      Description = "Sensorimotor rhythm at 13Hz promotes relaxed yet alert state. " +
                    "Perfect for sustained focus without mental fatigue. Used in neurofeedback training.",
      Category = PresetCategory.Isochronic,
      IsBeginnerFriendly = true, // More gentle than other isochronic presets
      RecommendedDuration = TimeSpan.FromMinutes(25),
      Tags = ["focus", "calm", "smr", "sustained"],
      Layers =
        [
          LayerConfig.AmTone(260f, 13.0f, 0.8f, description: "SMR pulse carrier")
        ]
    };

    /// <summary>
    /// Returns all isochronic presets in ascending frequency order.
    /// </summary>
    public static IReadOnlyList<FrequencyPreset> All =>
      [
        Ic_Delta_PowerNap,
        Ic_Theta_Creative,
        Ic_Alpha_Unwind,
        Ic_SMR_CalmAlert,
        Ic_Beta_Focus
      ];
  }
}
