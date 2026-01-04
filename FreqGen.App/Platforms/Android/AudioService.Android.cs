using Android.Media;
using Android.OS;
using FreqGen.Core;
using Microsoft.Extensions.Logging;

namespace FreqGen.App.Services
{
  /// <summary>
  /// Android-specific audio implementation using AudioTrack.
  /// Optimized for low-latency real-time audio playback.
  /// </summary>
  public sealed partial class AudioService
  {
    private AudioTrack? _audioTrack;
    private Thread? _audioThread;
    private volatile bool _isAudioThreadRunning;

    // Device derived buffer size in frames
    private int _bufferFrames;

    // Pre-allocated buffers (no allocations in audio loop)
    private float[]? _floatBuffer;
    private short[]? _pcmBuffer;

    partial void InitializePlatformAudio()
    {
      _logger.LogInformation("Initializing Android AudioTrack");

      try
      {
        // Query minimum buffer size in bytes
        int minBufferSizeBytes = AudioTrack.GetMinBufferSize(
          AudioSettings.SampleRate,
          ChannelOut.Mono,
          Encoding.Pcm16bit
        );

        if (minBufferSizeBytes == (int)TrackStatus.ErrorBadValue ||
            minBufferSizeBytes <= 0)
          throw new InvalidOperationException("Invalid AudioTrack buffer size");

        // PCM16 → 2 bytes per frame
        int minFrames = minBufferSizeBytes / sizeof(short);

        // Choose a stable working size
        _bufferFrames = minFrames * 2;

        // Use larger buffer for stability (4x minimum)
        int bufferSizeBytes = _bufferFrames * sizeof(short);

        _logger.LogDebug(
          "AudioTrack buffer: minFrames={Min}, usingFrames={Used}",
          minFrames,
          _bufferFrames
        );

        // Configure audio attributes
        AudioAttributes? audioAttributes = new AudioAttributes.Builder()?
          .SetUsage(AudioUsageKind.Media)?
          .SetContentType(AudioContentType.Music)?
          .Build();

        // Configure audio format
        AudioFormat? audioFormat = new AudioFormat.Builder()?
          .SetSampleRate(AudioSettings.SampleRate)?
          .SetEncoding(Encoding.Pcm16bit)?
          .SetChannelMask(ChannelOut.Mono)?
          .Build();

        if (audioAttributes is null || audioFormat is null)
          throw new InvalidOperationException("Failed to create audio configuration");

        // Build AudioTrack
        _audioTrack = new AudioTrack.Builder()
          .SetAudioAttributes(audioAttributes)
          .SetAudioFormat(audioFormat)
          .SetBufferSizeInBytes(bufferSizeBytes)
          .SetTransferMode(AudioTrackMode.Stream)
          .Build() ??
            throw new InvalidOperationException("Failed to create AudioTrack");

        // Enable low-latency mode on Android 8.0+
        //if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        //{
        //  builder.SetPerformanceMode(AudioTrackPerformanceMode.LowLatency);
        //  _logger.LogDebug("Low-latency mode enabled");
        //}

        // Allocate buffers once, based on device
        _floatBuffer = new float[_bufferFrames];
        _pcmBuffer = new short[_bufferFrames];

        _logger.LogInformation("Android AudioTrack initialized successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to initialize Android audio");
        throw;
      }
    }

    partial void StartPlatformAudio()
    {
      if (_audioTrack is null || _engine is null || _isAudioThreadRunning)
        return;

      _logger.LogInformation("Starting Android audio thread");

      try
      {
        // Start AudioTrack playback
        _audioTrack.Play();

        // Start audio rendering thread
        _isAudioThreadRunning = true;
        _audioThread = new(AudioThreadLoop)
        {
          Name = "FreqGen-Android-AudioThread",
          IsBackground = true,
          Priority = System.Threading.ThreadPriority.Highest
        };

        _audioThread.Start();
        _logger.LogInformation("Android audio thread started");
      }
      catch (Exception ex)
      {
        _isAudioThreadRunning = false;
        _logger.LogError(ex, "Failed to start Android audio");
        throw;
      }
    }

    partial void StopPlatformAudio()
    {
      if (!_isAudioThreadRunning)
        return;

      _logger.LogInformation("Stopping Android audio thread");

      try
      {
        // Signal thread to stop
        _isAudioThreadRunning = false;

        // Wait for thread to exit (with timeout)
        if (_audioThread is not null && _audioThread.IsAlive)
          if (!_audioThread.Join(TimeSpan.FromSeconds(2)))
            _logger.LogWarning("Audio thread did not stop gracefully");

        // Stop and flush AudioTrack
        _audioTrack?.Stop();
        _audioTrack?.Flush();

        _logger.LogInformation("Android audio thread stopped");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error stopping Android audio");
      }
    }

    partial void DisposePlatformAudio()
    {
      try
      {
        _audioTrack?.Release();
        _audioTrack?.Dispose();
        _audioTrack = null;
        _audioThread = null;
        _floatBuffer = null;
        _pcmBuffer = null;

        _logger.LogInformation("Android audio resources disposed");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error disposing Android audio");
      }
    }

    /// <summary>
    /// Audio rendering loop running on dedicated high-priority thread.
    /// This is the HOT PATH - must be allocation-free and deterministic.
    /// </summary>
    private void AudioThreadLoop()
    {
      // Attempt to elevate thread priority for real-time audio
      try
      {
        Process.SetThreadPriority(Android.OS.ThreadPriority.UrgentAudio);
        _logger.LogDebug("Audio thread priority set to URGENT_AUDIO");
      }
      catch { /* Best effort only */ }

      _logger.LogInformation("Audio thread loop started");

      try
      {
        while (_isAudioThreadRunning)
        {
          // Local copies for thread safety (avoid torn reads)
          AudioEngine? engine = _engine;
          AudioTrack? audioTrack = _audioTrack;
          float[]? floatBuffer = _floatBuffer;
          short[]? pcmBuffer = _pcmBuffer;

          if (engine is null || audioTrack is null ||
              floatBuffer is null || pcmBuffer is null)
            break;

          // Generate what device expects (HOT PATH)
          engine.FillBuffer(floatBuffer.AsSpan());

          // Convert float [-1.0, 1.0] to PCM16 [-32768, 32767]
          for (int i = 0; i < _bufferFrames; i++)
          {
            // Safety clamp (should already be clamped by engine)
            float sample = Math.Clamp(floatBuffer[i], -1f, 1f);

            // Convert to PCM16
            pcmBuffer[i] = (short)(sample * 32767f);
          }

          // Write entire buffer, handling partial writes
          int samplesWritten = 0;

          while (samplesWritten < _bufferFrames && _isAudioThreadRunning)
          {
            int written = audioTrack.Write(
              pcmBuffer,
              samplesWritten,
              _bufferFrames - samplesWritten,
              WriteMode.Blocking
            );

            if (written < 0)
              throw new InvalidOperationException(
                $"AudioTrack write error: {written}"
              );

            samplesWritten += written;
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Fatal error in audio thread loop");
      }
    }
  }
}
