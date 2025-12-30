using Android.Media;
using Android.OS;
using FreqGen.Core;

namespace FreqGen.PoC
{
  public sealed class AndroidAudioPlayer
  {
    private readonly AudioEngine _engine = new();
    private readonly AudioTrack _audioTrack;

    private readonly float[] _floatBuffer = new float[AudioSettings.BufferSize];
    private readonly short[] _pcmBuffer = new short[AudioSettings.BufferSize];

    private volatile bool _isPlaying;
    private Thread? _thread;

    public AndroidAudioPlayer()
    {
      int minBufferSize = AudioTrack.GetMinBufferSize(
        AudioSettings.SampleRate,
        ChannelOut.Mono,
        Encoding.Pcm16bit
      );

      int trackBufferSize = Math.Max(minBufferSize, AudioSettings.BufferSize * sizeof(short) * 4);

      AudioAttributes? audioAttributes = new AudioAttributes.Builder()!
        .SetUsage(AudioUsageKind.Media)!
        .SetContentType(AudioContentType.Music)!
        .Build();

      AudioFormat? audioFormat = new AudioFormat.Builder()!
        .SetSampleRate(AudioSettings.SampleRate)!
        .SetEncoding(Encoding.Pcm16bit)!
        .SetChannelMask(ChannelOut.Mono)
        .Build();

      AudioTrack.Builder builder = new AudioTrack.Builder()
        .SetAudioAttributes(audioAttributes!)
        .SetAudioFormat(audioFormat!)
        .SetBufferSizeInBytes(trackBufferSize)
        .SetTransferMode(AudioTrackMode.Stream);

      if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        builder.SetPerformanceMode(AudioTrackPerformanceMode.LowLatency);

      _audioTrack = builder.Build();
    }

    public void SetFrequency(double frequency) =>
      _engine.SetFrequency(frequency);

    public void Start()
    {
      if (_isPlaying)
        return;

      _isPlaying = true;
      _audioTrack.Play();

      _thread = new(AudioLoop)
      {
        Name = "AudioThread",
        IsBackground = true,
        Priority = System.Threading.ThreadPriority.Highest
      };

      _thread.Start();
    }

    public void Stop()
    {
      _isPlaying = false;
      _thread?.Join();

      _audioTrack.Stop();
      _audioTrack.Flush();
    }

    private void AudioLoop()
    {
      Process.SetThreadPriority(Android.OS.ThreadPriority.UrgentAudio);

      while (_isPlaying)
      {
        _engine.FillBuffer(_floatBuffer);

        for (int i = 0; i < _floatBuffer.Length; i++)
          _pcmBuffer[i] = (short)(_floatBuffer[i] * 32767f);

        _audioTrack.Write(_pcmBuffer, 0, _pcmBuffer.Length, WriteMode.Blocking);
      }
    }
  }
}
