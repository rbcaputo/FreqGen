using Android.Media;
using Android.OS;
using FreqGen.Core;
using Microsoft.Extensions.Logging;

namespace FreqGen.App.Services
{
  /// <summary>
  /// Android-specific audio implementation.
  /// </summary>
  public sealed partial class AudioService
  {
    private AudioTrack? _audioTrack;
    private Thread? _audioThread;
    private volatile bool _isAudioThreadRunning;

    private readonly float[] _floatBuffer = new float[AudioSettings.BufferSize];
    private readonly short[] _pcmBuffer = new short[AudioSettings.BufferSize];

    partial void InitializePlatformAudio()
    {
      int minBufferSize = AudioTrack.GetMinBufferSize(
        AudioSettings.SampleRate,
        ChannelOut.Mono,
        Encoding.Pcm16bit
      );

      int trackBufferSize = Math.Max(
        minBufferSize,
        AudioSettings.BufferSize * sizeof(short) * 4
      );

      AudioAttributes? audioAttributes = new AudioAttributes.Builder()
        .SetUsage(AudioUsageKind.Media)?
        .SetContentType(AudioContentType.Music)?
        .Build();

      AudioFormat? audioFormat = new AudioFormat.Builder()
        .SetSampleRate(AudioSettings.SampleRate)?
        .SetEncoding(Encoding.Pcm16bit)?
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

    partial void StartPlatformAudio()
    {
      if (_isAudioThreadRunning || _audioTrack == null || _engine == null)
        return;

      _isAudioThreadRunning = true;
      _audioTrack.Play();

      _audioThread = new(AudioLoop)
      {
        Name = "FreqGen-AudioThread",
        IsBackground = true,
        Priority = System.Threading.ThreadPriority.Highest
      };

      _audioThread.Start();
    }


    partial void StopPlatformAudio()
    {
      _isAudioThreadRunning = false;
      _audioThread?.Join(TimeSpan.FromSeconds(2));

      _audioTrack?.Stop();
      _audioTrack?.Flush();
    }

    private void AudioLoop()
    {
      // Set thread priority for real-time audio
      Process.SetThreadPriority(Android.OS.ThreadPriority.UrgentAudio);

      while (_isAudioThreadRunning)
      {
        try
        {
          AudioEngine? engine = _engine; // Local copy for thread safety
          AudioTrack? audioTrack = _audioTrack;

          if (engine is null || audioTrack is null || !_isAudioThreadRunning)
            break;

          // Fill buffer from audio engine
          engine.FillBuffer(_floatBuffer);

          // Convert float to PCM16
          for (int i = 0; i < _floatBuffer.Length; i++)
            _pcmBuffer[i] = (short)(_floatBuffer[i] * 32767f);

          // Write to audio track
          audioTrack.Write(_pcmBuffer, 0, _pcmBuffer.Length, WriteMode.Blocking);
        }
        catch (Exception ex)
        {
          _logger.LogError($"Audio loop error: {ex}");
          break;
        }
      }
    }

    partial void DisposePlatformAudio()
    {
      _audioTrack?.Release();
      _audioTrack?.Dispose();
      _audioTrack = null;
    }
  }
}
