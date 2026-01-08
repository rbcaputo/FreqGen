using AudioToolbox;
using AVFoundation;
using Foundation;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using ToneSync.Core;

namespace ToneSync.App.Services
{
  /// <summary>
  /// iOS-specific audio implementation using AVAudioEngine.
  /// Supports both mono and stereo output with planar-to-interleaved conversion.
  /// </summary>
  public sealed partial class AudioService
  {
    private AVAudioEngine? _avAudioEngine;
    private AVAudioSourceNode? _sourceNode;
    private AVAudioFormat? _audioFormat;

    // Pre-allocated render buffers (planar format, no allocations in callback)
    private readonly float[] _monoRenderBuffer = new float[Core.AudioSettings.MaxBufferSize];
    private readonly float[] _leftRenderBuffer = new float[Core.AudioSettings.MaxBufferSize];
    private readonly float[] _rightRenderBuffer = new float[Core.AudioSettings.MaxBufferSize];

    partial void InitializePlatformAudio()
    {
      _logger.LogInformation("Initializing iOS AVAudioEngine");

      try
      {
        // Determine audio mode from engine
        _channelMode = _engine?.ChannelMode ?? ChannelMode.Mono;
        uint channelCount = (uint)Core.AudioSettings.ChannelSettings.GetChannelCount(_channelMode);

        // Configure audio session for playback
        AVAudioSession audioSession = AVAudioSession.SharedInstance();

        bool categoryResult = audioSession.SetCategory(
          AVAudioSession.CategoryPlayback,
          out NSError sessionError
        );

        if (!categoryResult || sessionError is not null)
          throw new InvalidOperationException(
            $"Failed to set audio category: {sessionError?.LocalizedDescription ?? "Unknown error"}"
          );

        bool activeResult = audioSession.SetActive(true, out sessionError);

        if (!activeResult || sessionError is not null)
          throw new InvalidOperationException(
            $"Failed to activate audio session: {sessionError?.LocalizedDescription ?? "Unknown error"}"
          );

        _logger.LogDebug(
          "Audio session configured: Category=Playback, Mode={Mode}",
          _channelMode
        );

        // Create audio engine
        _avAudioEngine = new();

        // Create audio format (mono or stereo, 48kHz, float32)
        _audioFormat = new(
          sampleRate: Core.AudioSettings.SampleRate,
          channels: channelCount
        );

        if (_audioFormat is null)
          throw new InvalidOperationException("Failed to create audio format");

        _logger.LogDebug(
          "Audio format: SampleRate={SampleRate}, Channels={Channels}",
          _audioFormat.SampleRate,
          _audioFormat.ChannelCount
        );

        // Create source node with render callback
        _sourceNode = new((
          ref isSilence,
          ref timestamp,
          frameCount,
          audioBuffers
        ) => RenderBlock(ref isSilence, ref timestamp, frameCount, audioBuffers));

        if (_sourceNode is null)
          throw new InvalidOperationException("Failed to create source node");

        // Attach node to engine
        _avAudioEngine.AttachNode(_sourceNode);

        // Connect source node to main mixer
        _avAudioEngine.Connect(
          _sourceNode,
          _avAudioEngine.MainMixerNode,
          _audioFormat
        );

        // Prepare engine for playback
        _avAudioEngine.Prepare();

        _logger.LogInformation(
          "iOS AVAudioEngine initialized successfully in {Mode} mode",
          _channelMode
        );
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to initialize iOS audio");
        throw new InvalidOperationException("iOS audio initialization failed", ex);
      }
    }

    partial void StartPlatformAudio()
    {
      if (_avAudioEngine is null || _avAudioEngine.Running)
        return;

      _logger.LogInformation("Starting iOS audio engine");

      try
      {
        _avAudioEngine.StartAndReturnError(out NSError? error);

        if (error is not null)
          throw new InvalidOperationException(
            $"Failed to start audio engine: {error.LocalizedDescription}"
          );

        _logger.LogInformation("iOS audio engine started in {Mode} mode", _channelMode);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to start iOS audio");
        throw new InvalidOperationException("Failed to start iOS audio", ex);
      }
    }

    partial void StopPlatformAudio()
    {
      if (_avAudioEngine is null || !_avAudioEngine.Running)
        return;

      _logger.LogInformation("Stopping iOS audio engine");

      try
      {
        _avAudioEngine.Stop();
        _logger.LogInformation("iOS audio engine stopped");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error stopping iOS audio");
      }
    }

    partial void DisposePlatformAudio()
    {
      try
      {
        if (_avAudioEngine?.Running == true)
          _avAudioEngine.Stop();

        _sourceNode?.Dispose();
        _avAudioEngine?.Dispose();

        _sourceNode = null;
        _avAudioEngine = null;
        _audioFormat = null;

        _logger.LogInformation("iOS audio resources disposed");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error disposing iOS audio");
      }
    }


    /// <summary>
    /// Render block for AVAudioSourceNode.
    /// Invoked by Core Audio on real-time thread for audio generation.
    /// This is the HOT PATH - must be allocation-free and deterministic.
    /// </summary>
    /// <returns>0 for success (noErr), non-zero for error.</returns>
    private unsafe int RenderBlock(
      ref bool isSilence,
      ref AudioTimeStamp timestamp,
      uint frameCount,
      AudioBuffers audioBuffers
    )
    {
      // Fast path: return silence if requested
      if (isSilence || _engine is null)
        return 0; // noErr

      try
      {
        // Validate frame count
        if (frameCount > _monoRenderBuffer.Length)
        {
          // Log on background thread
          Task.Run(() => _logger.LogWarning(
            "Frame count {FrameCount} exceeds buffer size {BufferSize}",
            frameCount, _monoRenderBuffer.Length
          ));

          frameCount = (uint)_monoRenderBuffer.Length;
        }

        if (_channelMode == ChannelMode.Stereo)
          return RenderStereo(frameCount, audioBuffers);
        else
          return RenderMono(frameCount, audioBuffers);
      }
      catch (Exception ex)
      {
        // CRITICAL: Never throw from audio callback
        Task.Run(() => _logger.LogError(ex, "iOS render callback error"));
        return -1; // Error
      }
    }

    /// <summary>
    /// Renders mono audio into Core Audio buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int RenderMono(uint frameCount, AudioBuffers audioBuffers)
    {
      AudioBuffer buffer = audioBuffers[0];

      if (buffer.Data == IntPtr.Zero)
        return -1;

      float* outputPtr = (float*)buffer.Data;

      // Generate mono audio
      Span<float> renderSpan = _monoRenderBuffer.AsSpan(0, (int)frameCount);
      _engine?.FillMonoBuffer(renderSpan);

      // Copy to Core audio buffer
      for (int i = 0; i < frameCount; i++)
        outputPtr[i] = _monoRenderBuffer[i];

      return 0;
    }

    /// <summary>
    /// Renders stereo audio (planar) and converts to interleaved format.
    /// Core Audio expects interleaved stereo: [L, R, L, R, ...]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe int RenderStereo(uint frameCount, AudioBuffers audioBuffers)
    {
      // For stereo, Core audio uses one buffer with interleaved samples
      AudioBuffer buffer = audioBuffers[0];

      if (buffer.Data == IntPtr.Zero)
        return -1;

      float* outputPrt = (float*)buffer.Data;

      // Generate stereo audio (planar format)
      Span<float> leftSpan = _leftRenderBuffer.AsSpan(0, (int)frameCount);
      Span<float> rightSpan = _rightRenderBuffer.AsSpan(0, (int)frameCount);
      _engine?.FillStereoBuffer(leftSpan, rightSpan);


      // Convert planar to interleaved
      for (int i = 0; i < frameCount; i++)
      {
        outputPrt[i * 2] = _leftRenderBuffer[i]; // Left channel
        outputPrt[i * 2 + 1] = _rightRenderBuffer[i]; // Right channel
      }

      return 0;
    }
  }
}
