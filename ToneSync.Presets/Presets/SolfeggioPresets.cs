using ToneSync.Presets.Models;

namespace ToneSync.Presets.Presets
{
  /// <summary>
  /// Solfeggio frequency presets based on the ancient 6-tone scale.
  /// These frequencies have been used in sacred music and are claimed to have healing properties.
  /// All presets use pure tones (no modulation) for maximum clarity.
  /// </summary>
  public static class SolfeggioPresets
  {
    /// <summary>
    /// 174Hz Solfeggio frequency preset.
    /// Associated with pain reduction and creating a sense of organizational safety.
    /// The foundation frequency for physical grounding.
    /// </summary>
    public static FrequencyPreset Sf_174Hz => new()
    {
      ID = "sf_174",
      DisplayName = "Foundation (174Hz)",
      Description = "Associated with pain reduction and creating a sense of organizational safety. " +
                    "The foundation frequency for physical grounding.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["pain-relief", "grounding", "foundation"],
      Layers = [LayerConfig.PureTone(174f, description: "Foundation tone")]
    };

    /// <summary>
    /// 285Hz Solfeggio frequency preset.
    /// Associated with healing tissue and internal organ repair.
    /// May enhance cellular regeneration and energy field restructuring.
    /// </summary>
    public static FrequencyPreset Sf_285Hz => new()
    {
      ID = "sf_285",
      DisplayName = "Quantum Cognition (285Hz)",
      Description = "Associated with healing tissue and internal organ repair. " +
                    "May enhance cellular regeneration and energy field restructuring.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["healing", "cellular", "energy"],
      Layers = [LayerConfig.PureTone(285f, description: "Healing frequency")]
    };

    /// <summary>
    /// 396Hz Solfeggio frequency preset.
    /// Associated with liberating guilt and fear, turning grief into joy.
    /// The frequency of emotional release and freedom from negative patterns.
    /// </summary>
    public static FrequencyPreset Sf_396Hz => new()
    {
      ID = "sf_396",
      DisplayName = "Liberation (396Hz)",
      Description = "Associated with liberating guilt and fear, turning grief into joy. " +
                    "The frequency of emotional release and freedom.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["emotional", "release", "liberation", "fear-release"],
      Layers = [LayerConfig.PureTone(396f, description: "Liberation frequency")]
    };

    /// <summary>
    /// 417Hz Solfeggio frequency preset.
    /// Associated with undoing situations and facilitating positive change.
    /// Helps clear negative energy patterns and encourages new beginnings.
    /// </summary>
    public static FrequencyPreset Sf_417Hz => new()
    {
      ID = "sf_417",
      DisplayName = "Change (417Hz)",
      Description = "Associated with undoing situations and facilitating positive change. " +
                    "Helps clear negative energy and encourages new beginnings.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["transformation", "change", "cleansing"],
      Layers = [LayerConfig.PureTone(417f, description: "Change frequency")]
    };

    /// <summary>
    /// 528Hz Solfeggio frequency preset.
    /// The 'Miracle' tone associated with DNA repair, transformation, and clarity of mind.
    /// The most researched and documented Solfeggio frequency, often called the "Love Frequency."
    /// </summary>
    public static FrequencyPreset Sf_528Hz => new()
    {
      ID = "sf_528",
      DisplayName = "Transformation (528Hz)",
      Description = "The 'Miracle' tone associated with DNA repair, transformation, and clarity of mind. " +
                    "The most researched and documented Solfeggio frequency.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["miracle", "dna", "transformation", "love"],
      Layers = [LayerConfig.PureTone(528f, description: "Miracle frequency")]
    };

    /// <summary>
    /// 639Hz Solfeggio frequency preset.
    /// Associated with harmonizing relationships and enhancing communication.
    /// Promotes understanding, tolerance, empathy, and social connection.
    /// </summary>
    public static FrequencyPreset Sf_639Hz => new()
    {
      ID = "sf_639",
      DisplayName = "Relationship (639Hz)",
      Description = "Associated with harmonizing relationships and enhancing communication. " +
                    "Promotes understanding, tolerance, and social connection.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["relationships", "connection", "harmony", "communication"],
      Layers = [LayerConfig.PureTone(639f, description: "Connection frequency")]
    };

    /// <summary>
    /// 741Hz Solfeggio frequency preset.
    /// Associated with solving problems, self-expression, and awakening intuition.
    /// Helps with creative expression and finding solutions to challenges.
    /// </summary>
    public static FrequencyPreset Sf_741Hz => new()
    {
      ID = "sf_741",
      DisplayName = "Expression (741Hz)",
      Description = "Associated with solving problems, expression, and awakening intuition. " +
                    "Helps with self-expression and finding solutions.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["expression", "intuition", "problem-solving"],
      Layers = [LayerConfig.PureTone(741f, description: "Expression frequency")]
    };

    /// <summary>
    /// 852Hz Solfeggio frequency preset.
    /// Associated with returning to spiritual order and awakening intuition.
    /// Enhances spiritual awareness, inner strength, and intuitive perception.
    /// </summary>
    public static FrequencyPreset Sf_852Hz => new()
    {
      ID = "sf_852",
      DisplayName = "Intuition (852Hz)",
      Description = "Associated with returning to spiritual order and awakening intuition. " +
                    "Enhances spiritual awareness and inner strength.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["intuition", "spiritual", "awareness"],
      Layers = [LayerConfig.PureTone(852f, description: "Intuition frequency")]
    };

    /// <summary>
    /// 963Hz Solfeggio frequency preset.
    /// Associated with higher consciousness and spiritual enlightenment.
    /// The frequency of divine connection, universal awareness, and unity consciousness.
    /// </summary>
    public static FrequencyPreset Sf_963Hz => new()
    {
      ID = "sf_963",
      DisplayName = "Divinity (963Hz)",
      Description = "Associated with higher consciousness and spiritual enlightenment. " +
                    "The frequency of divine connection and universal awareness.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(20),
      Tags = ["enlightenment", "divine", "consciousness", "spiritual"],
      Layers = [LayerConfig.PureTone(963f, description: "Divine frequency")]
    };

    /// <summary>
    /// The Core Triad Harmony preset.
    /// A harmonious blend of the three core Solfeggio frequencies:
    /// 396Hz (Liberation), 528Hz (Transformation), and 639Hz (Connection).
    /// </summary>
    public static FrequencyPreset Sf_CoreTriad => new()
    {
      ID = "sf_harmony",
      DisplayName = "Core Triad Harmony",
      Description = "A balanced synthesis of Liberation (396 Hz), Transformation (528 Hz), and Connection (639 Hz). " +
                    "Creates a comprehensive healing experience combining emotional release, cellular transformation, and social harmony.",
      Category = PresetCategory.Solfeggio,
      IsBeginnerFriendly = true,
      RecommendedDuration = TimeSpan.FromMinutes(30),
      Tags = ["harmony", "balanced", "comprehensive"],
      Layers =
        [
          LayerConfig.PureTone(396f, 0.33f, description: "Liberation"),
          LayerConfig.PureTone(528f, 0.33f, description: "Transformation"),
          LayerConfig.PureTone(639f, 0.33f, description: "Connection")
        ]
    };

    /// <summary>
    /// Gets all Solfeggio frequency presets in ascending frequency order.
    /// </summary>
    public static IReadOnlyList<FrequencyPreset> All =>
      [
        Sf_174Hz, Sf_285Hz, Sf_396Hz,
        Sf_417Hz, Sf_528Hz, Sf_639Hz,
        Sf_741Hz, Sf_852Hz, Sf_963Hz,
        Sf_CoreTriad
      ];
  }
}
