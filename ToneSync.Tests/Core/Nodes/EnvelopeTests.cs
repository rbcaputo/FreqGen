using ToneSync.Core;
using ToneSync.Core.Nodes;

namespace ToneSync.Tests.Core.Nodes
{
  public sealed class EnvelopeTests
  {
    private const float Tolerance = 1e-5f;

    [Fact]
    public void Starts_At_Zero()
    {
      var env = new Envelope();

      Assert.Equal(0f, env.CurrentValue, Tolerance);
    }

    [Fact]
    public void Attack_Moves_Envelope_Upward()
    {
      var env = new Envelope();
      env.Configure(1f, 1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var buffer = new float[AudioSettings.RecommendedBufferSize];
      env.Process(buffer);

      Assert.True(env.CurrentValue > 0f);
      Assert.True(env.CurrentValue < 1f);
    }

    [Fact]
    public void Release_Moves_Envelope_Downward()
    {
      var env = new Envelope();
      env.Configure(0.1f, 0.1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var bufferA = new float[AudioSettings.RecommendedBufferSize * 4];
      env.Process(bufferA); // reach near 1.0

      var peak = env.CurrentValue;
      Assert.True(peak > 0.9f);

      env.Trigger(false);

      var bufferB = new float[AudioSettings.RecommendedBufferSize];
      env.Process(bufferB);

      Assert.True(env.CurrentValue < peak);
    }

    [Fact]
    public void Envelope_Is_Monotonic_During_Attack()
    {
      var env = new Envelope();
      env.Configure(0.5f, 1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var buffer = new float[AudioSettings.RecommendedBufferSize * 2];
      var last = env.CurrentValue;
      env.Process(buffer);

      foreach (var _ in buffer)
      {
        var current = env.CurrentValue;
        Assert.True(current >= last - Tolerance);

        last = current;
      }
    }

    [Fact]
    public void Envelope_Is_Monotonic_During_Release()
    {
      var env = new Envelope();
      env.Configure(0.1f, 0.5f, AudioSettings.SampleRate);
      env.Trigger(true);

      var bufferA = new float[AudioSettings.RecommendedBufferSize * 4];
      env.Process(bufferA);

      env.Trigger(false);

      var bufferB = new float[AudioSettings.RecommendedBufferSize * 2];
      var last = env.CurrentValue;
      env.Process(bufferB);

      foreach (var _ in bufferB)
      {
        var current = env.CurrentValue;
        Assert.True(current <= last + Tolerance);

        last = current;
      }
    }

    [Fact]
    public void Attack_Is_Faster_Than_Release_When_Configured_So()
    {
      var env = new Envelope();
      env.Configure(0.1f, 1.0f, AudioSettings.SampleRate);
      env.Trigger(true);

      var bufferA = new float[AudioSettings.RecommendedBufferSize];
      env.Process(bufferA);
      var attack = env.CurrentValue;

      env.Trigger(false);

      var bufferB = new float[AudioSettings.RecommendedBufferSize];
      env.Process(bufferB);
      var release = env.CurrentValue;

      // Attack should rise more quickly than release falls
      Assert.True(attack > (1f - release));
    }

    [Fact]
    public void Never_Exceeds_Zero_To_One_Range()
    {
      var env = new Envelope();
      env.Configure(0.1f, 0.1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var buffer = new float[AudioSettings.RecommendedBufferSize * 8];
      env.Process(buffer);

      Assert.InRange(env.CurrentValue, 0f, 1f);

      env.Trigger(false);
      env.Process(buffer);

      Assert.InRange(env.CurrentValue, 0f, 1f);
    }

    [Fact]
    public void Silent_Buffer_Remains_Silent()
    {
      var env = new Envelope();
      env.Configure(0.1f, 0.1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var buffer = new float[AudioSettings.RecommendedBufferSize];
      env.Process(buffer);

      foreach (var sample in buffer)
        Assert.Equal(0f, sample, Tolerance);
    }

    [Fact]
    public void Long_Attack_Does_Not_Jump_Abruptly()
    {
      var env = new Envelope();
      env.Configure(30f, 30f, AudioSettings.SampleRate);
      env.Trigger(true);

      var buffer = new float[AudioSettings.RecommendedBufferSize];
      env.Process(buffer);

      Assert.True(env.CurrentValue < 0.01f);
    }

    [Fact]
    public void Reset_Clears_Envelope_State()
    {
      var env = new Envelope();
      env.Configure(0.1f, 0.1f, AudioSettings.SampleRate);
      env.Trigger(true);

      var bufferA = new float[AudioSettings.RecommendedBufferSize * 2];
      env.Process(bufferA);
      Assert.True(env.CurrentValue > 0f);

      env.Reset();
      Assert.Equal(0f, env.CurrentValue, Tolerance);

      var bufferB = new float[AudioSettings.RecommendedBufferSize / 16];
      env.Process(bufferB);

      foreach (var sample in bufferB)
        Assert.Equal(0f, sample, Tolerance);
    }
  }
}
