using ToneSync.Core;
using ToneSync.Core.Nodes.Oscillators;

namespace ToneSync.Tests.Core.Nodes
{
  public sealed class SineOscillatorTests
  {
    private const float Tolerance = 1e-5f;

    [Fact]
    public void Outputs_Are_Within_Minus_One_To_One()
    {
      var osc = new SineOscillator();
      osc.SetFrequency(440f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.SampleRate];
      osc.Process(buffer);

      foreach (var sample in buffer)
        Assert.InRange(sample, -1f, 1f);
    }

    [Fact]
    public void Generates_Correct_Frequency()
    {
      var osc = new SineOscillator();
      osc.SetFrequency(1000f, AudioSettings.SampleRate);

      var samples = AudioSettings.SampleRate;
      var buffer = new float[samples];
      osc.Process(buffer);

      var zeroCrossings = 0;
      for (var i = 0; i < buffer.Length; i++)
        if (buffer[i - 1] <= 0 && buffer[i] > 0)
          zeroCrossings++;

      // One positive-going zero crossing per cycle
      Assert.InRange(zeroCrossings, 999, 1001);
    }

    [Fact]
    public void Phase_Is_Continuous_Across_Buffers()
    {
      var osc = new SineOscillator();
      osc.SetFrequency(440f, AudioSettings.SampleRate);

      var bufferA = new float[AudioSettings.RecommendedBufferSize / 8];
      var bufferB = new float[AudioSettings.RecommendedBufferSize / 8];

      osc.Process(bufferA);
      var lastSample = bufferA[^1];

      osc.Process(bufferB);
      var firstSample = bufferB[0];

      Assert.True(Math.Abs(lastSample - firstSample) < 0.1f);
    }

    [Fact]
    public void No_Dc_Offset_Over_Time()
    {
      var osc = new SineOscillator();
      osc.SetFrequency(440f, AudioSettings.SampleRate);

      var samples = AudioSettings.SampleRate;
      var buffer = new float[samples];
      osc.Process(buffer);

      var mean = 0f;
      foreach (var sample in buffer)
        mean += sample;

      mean /= samples;
      Assert.InRange(mean, -1e-4f, 1e-4f);
    }

    [Fact]
    public void Reset_Restarts_Wave_At_Zero_Phase()
    {
      var osc = new SineOscillator();
      osc.SetFrequency(440f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.RecommendedBufferSize / 64];
      osc.Process(buffer);

      osc.Reset();
      osc.Process(buffer);

      Assert.InRange(buffer[0], -Tolerance, Tolerance);
    }

    [Fact]
    public void Is_Deterministic()
    {
      var oscA = new SineOscillator();
      var oscB = new SineOscillator();
      oscA.SetFrequency(440f, AudioSettings.SampleRate);
      oscB.SetFrequency(440f, AudioSettings.SampleRate);

      var bufferA = new float[AudioSettings.RecommendedBufferSize];
      var bufferB = new float[AudioSettings.RecommendedBufferSize];

      for (int i = 0; i < 100; i++)
      {
        oscA.Process(bufferA);
        oscB.Process(bufferB);

        for (int j = 0; j < bufferA.Length; j++)
          Assert.Equal(bufferA[j], bufferB[j], Tolerance);
      }
    }
  }
}