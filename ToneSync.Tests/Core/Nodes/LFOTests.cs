using ToneSync.Core;
using ToneSync.Core.Nodes.Modulators;

namespace ToneSync.Tests.Core.Nodes
{
  public sealed class LFOTests
  {
    private const float Tolerance = 1e-4f;

    [Fact]
    public void Outputs_Are_Within_Minus_One_To_One()
    {
      var lfo = new LFO();
      lfo.SetFrequency(5f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.RecommendedBufferSize * 2];
      lfo.Process(buffer);

      foreach (var sample in buffer)
        Assert.InRange(sample, -1f, 1f);
    }

    [Fact]
    public void Generates_Low_Frequency_Oscillation()
    {
      var lfo = new LFO();
      lfo.SetFrequency(2f, AudioSettings.SampleRate);

      var samples = AudioSettings.SampleRate;
      var buffer = new float[samples];
      lfo.Process(buffer);

      var zeroCrossings = 0;
      for (var i = 1; i < buffer.Length; i++)
      {
        if (buffer[i - 1] <= 0 && buffer[i] > 0)
          zeroCrossings++;
      }

      Assert.InRange(zeroCrossings, 1, 3);
    }

    [Fact]
    public void Interpolation_Prevents_Stepping()
    {
      var lfo = new LFO();
      lfo.SetFrequency(10f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.RecommendedBufferSize / 2];
      lfo.Process(buffer);

      // Ensure no flat steps across the buffer
      var identicalRuns = 0;
      for (var i = 1; i < buffer.Length; i++)
      {
        if (Math.Abs(buffer[i] - buffer[i - 1]) < Tolerance)
          identicalRuns++;
      }

      Assert.True(identicalRuns < buffer.Length / 4);
    }

    [Fact]
    public void Reset_Clears_State()
    {
      var lfo = new LFO();
      lfo.SetFrequency(5f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.RecommendedBufferSize / 16];
      lfo.Process(buffer);

      lfo.Reset();
      lfo.Process(buffer);

      Assert.InRange(buffer[0], -Tolerance, Tolerance);
    }

    [Fact]
    public void Does_Not_Produce_NaNs_Or_Infinities()
    {
      var lfo = new LFO();
      lfo.SetFrequency(2f, AudioSettings.SampleRate);

      var buffer = new float[AudioSettings.RecommendedBufferSize];

      for (var i = 0; i < 10_000; i++)
      {
        lfo.Process(buffer);

        foreach (var sample in buffer)
        {
          Assert.False(float.IsNaN(sample));
          Assert.False(float.IsInfinity(sample));
        }
      }
    }

    [Fact]
    public void Is_Deterministic()
    {
      var lfoA = new LFO();
      var lfoB = new LFO();
      lfoA.SetFrequency(2f, AudioSettings.SampleRate);
      lfoB.SetFrequency(2f, AudioSettings.SampleRate);

      var bufferA = new float[AudioSettings.RecommendedBufferSize];
      var bufferB = new float[AudioSettings.RecommendedBufferSize];

      for (var i = 0; i < 100; i++)
      {
        lfoA.Process(bufferA);
        lfoB.Process(bufferB);

        for (var j = 0; j < bufferA.Length; j++)
          Assert.Equal(bufferA[j], bufferB[j], Tolerance);
      }
    }
  }
}
