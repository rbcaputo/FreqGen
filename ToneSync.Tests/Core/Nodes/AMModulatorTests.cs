using ToneSync.Core.Nodes.Modulators;

namespace ToneSync.Tests.Core.Nodes
{
  public sealed class AMModulatorTests
  {
    private const float Tolerance = 1e-6f;

    [Fact]
    public void Depth_Zero_Does_Nothing()
    {
      var carrier = new float[3] { 0.5f, -0.5f, 1f };
      var modulator = new float[3] { -1f, 0f, 1f };

      AMModulator.Apply(carrier, modulator, 0f);

      Assert.Equal(0.5f, carrier[0], Tolerance);
      Assert.Equal(-0.5f, carrier[1], Tolerance);
      Assert.Equal(1f, carrier[2], Tolerance);
    }

    [Fact]
    public void Full_Depth_Never_Exceeds_Unity()
    {
      var carrier = new float[3] { 1f, -1f, 0.8f };
      var modulator = new float[3] { -1f, 1f, 0f };

      AMModulator.Apply(carrier, modulator, 1f);

      foreach (var sample in carrier)
        Assert.InRange(sample, -1f, 1f);
    }

    [Fact]
    public void Modulation_Scales_Amplitude_Correctly()
    {
      var carrier = new float[3] { 1f, 1f, 1f };
      var modulator = new float[3] { -1f, 0f, 1f };

      AMModulator.Apply(carrier, modulator, 1f);

      Assert.Equal(0f, carrier[0], Tolerance);   // min
      Assert.Equal(0.5f, carrier[1], Tolerance); // mid
      Assert.Equal(1f, carrier[2], Tolerance);   // max
    }

    [Fact]
    public void Silent_Carrier_Remains_Silent()
    {
      var carrier = new float[128];
      var modulator = new float[128];
      Array.Fill(modulator, 1f);

      AMModulator.Apply(carrier, modulator, 1f);

      foreach (var sample in carrier)
        Assert.Equal(0f, sample, Tolerance);
    }
  }
}
