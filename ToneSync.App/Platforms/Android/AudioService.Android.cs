using Android.Media;
using Android.OS;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using ToneSync.Core;
using ToneSync.Core.Engine;

namespace ToneSync.App.Services
{
  /// <summary>
  /// Android-specific audio implementation using AudioTrack.
  /// Supports both mono and stereo output with planar-to-interleaved conversion.
  /// </summary>
  public sealed partial class AudioService
  {
    private AudioTrack? _audioTrack;
    private Thread? _audioThread;
    private volatile bool _isAudioThreadRunning;

    // Device derived buffer size in frames
    private int _bufferFrames;

    // Pre-allocated buffers (planar format for processing, no allocations in audio loop)
    private float[]? _monoBuffer;
    private float[]? _leftBuffer;
    private float[]? _rightBuffer;
    // Interleaved output buffer for AudioTrack
    private short[]? _pcmBuffer;

    partial void InitializePlatformAudio()
    {
      _logger.LogInformation("Initializing Android AudioTrack");

      try
      {
        // Determine channel mode from engine
        _channelMode = _engine?.ChannelMode ?? ChannelMode.Mono;

        ChannelOut channelOut = _channelMode == ChannelMode.Stereo
          ? ChannelOut.Stereo
          : ChannelOut.Mono;

        int channelCount = AudioSettings.ChannelSettings
          .GetChannelCount(_channelMode);

        // Query minimum buffer size in bytes
        int minBufferSizeBytes = AudioTrack.GetMinBufferSize(
          AudioSettings.SampleRate,
          channelOut,
          Encoding.Pcm16bit
        );

        if (minBufferSizeBytes == (int)TrackStatus.ErrorBadValue ||
            minBufferSizeBytes <= 0)
          throw new InvalidOperationException("Invalid AudioTrack buffer size");

        // PCM16 → 2 bytes per sample per channel
        int minFrames = minBufferSizeBytes / (sizeof(short) * channelCount);

        // Use larger buffer for stability
        _bufferFrames = minFrames * 2;

        // Use larger buffer for stability (4x minimum)
        _bufferFrames = minFrames * 2;
        int bufferSizeBytes = _bufferFrames * sizeof(short) * channelCount;

        _logger.LogDebug(
          "AudioTrack buffer: mode={Mode}, minFrames={Min}, usingFrames={Used}, channels={Channels}",
          _channelMode, minFrames,
          _bufferFrames, channelCount
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
          .SetChannelMask(channelOut)?
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

        // Allocate buffers based on channel mode
        if (_channelMode == ChannelMode.Stereo)
        {
          _leftBuffer = new float[_bufferFrames];
          _rightBuffer = new float[_bufferFrames];
        }
        else
          _monoBuffer = new float[_bufferFrames];

        // Interleaved PCM buffer (mono: 1x frames, stereo: 2x frames)
        _pcmBuffer = new short[_bufferFrames * channelCount];

        _logger.LogInformation(
          "Android AudioTrack initialized successfully in {Mode} mode",
          _channelMode
        );
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
          Name = "ToneSync-Android-AudioThread",
          IsBackground = true,
          Priority = System.Threading.ThreadPriority.Highest
        };

        _audioThread.Start();
        _logger.LogInformation("Android audio thread started in {Mode} mode", _channelMode);
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
        _monoBuffer = null;
        _leftBuffer = null;
        _rightBuffer = null;
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

      _logger.LogInformation("Audio thread loop started in {Mode} mode", _channelMode);

      try
      {
        while (_isAudioThreadRunning)
        {
          // Local copies for thread safety (avoid torn reads)
          AudioEngine? engine = _engine;
          AudioTrack? audioTrack = _audioTrack;
          short[]? pcmBuffer = _pcmBuffer;

          if (engine is null || audioTrack is null || pcmBuffer is null)
            break;

          // Render audio based on channel mode

          if (_channelMode == ChannelMode.Stereo)
            RenderStereo(engine, pcmBuffer);
          else
            RenderMono(engine, pcmBuffer);

          // Write entire buffer to AudioTrack, handling partial writes
          int samplesWritten = 0;
          int totalSamples = pcmBuffer.Length;

          while (samplesWritten < totalSamples && _isAudioThreadRunning)
          {
            int written = audioTrack.Write(
              pcmBuffer,
              samplesWritten,
              totalSamples - samplesWritten,
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

    /// <summary>
    /// Renders mono audio and converts to PCM16.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderMono(AudioEngine engine, short[] pcmBuffer)
    {
      float[]? monoBuffer = _monoBuffer;

      if (monoBuffer is null)
        return;

      // Generate mono audio
      engine.FillMonoBuffer(monoBuffer.AsSpan());

      // Convert float [-1, 1] to PCM16 [-32768, 32767]
      for (int i = 0; i < _bufferFrames; i++)
      {
        float sample = Math.Clamp(monoBuffer[i], -0.98f, 0.98f);
        pcmBuffer[i] = (short)(sample * 32767f);
      }
    }

    /// <summary>
    /// Renders stereo audio (planar) and converts to interleaved PCM16.
    /// Interleaved format: [L, R, L, R, L, R, ...]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderStereo(AudioEngine engine, short[] pcmBuffer)
    {
      float[]? leftBuffer = _leftBuffer;
      float[]? rightBuffer = _rightBuffer;

      if (leftBuffer is null || rightBuffer is null)
        return;

      // Generate stereo audio (planar format)
      engine.FillStereoBuffer(leftBuffer.AsSpan(), rightBuffer.AsSpan());

      // Convert planar float to interleaved PCM16
      for (int i = 0; i < _bufferFrames; i++)
      {
        // Left channel
        float leftSample = Math.Clamp(leftBuffer[i], -0.98f, 0.98f);
        pcmBuffer[i * 2] = (short)(leftSample * 32767f);

        // Right channel
        float rightSample = Math.Clamp(rightBuffer[i], -0.98f, 0.98f);
        pcmBuffer[i * 2 + 1] = (short)(rightSample * 32767f);
      }
    }
  }
}
