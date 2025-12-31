using AudioToolbox;
using AVFoundation;
using Foundation;
using Microsoft.Extensions.Logging;

namespace FreqGen.App.Services
{
  /// <summary>
  /// iOS-specific audio implementation using AVAudioEngine.
  /// </summary>
  public sealed partial class AudioService
  {
    private AVAudioEngine? _avAudioEngine;
    private AVAudioSourceNode? _sourceNode;
    private AVAudioFormat? _audioFormat;

    private float[] _iosRenderBuffer = new float[Core.AudioSettings.BufferSize * 4];

    partial void InitializePlatformAudio()
    {
      // Configure audio session
      AVAudioSession audioSession = AVAudioSession.SharedInstance();
      audioSession.SetCategory(AVAudioSessionCategory.Playback);
      audioSession.SetActive(true, out NSError error);

      if (error is not null)
        throw new InvalidOperationException($"Failed to activate audio session: {error}");

      // Create audio engine
      _avAudioEngine = new();

      // Create audio format (mono 44.1kHz, float32)
      _audioFormat = new(
        sampleRate: Core.AudioSettings.SampleRate,
        channels: 1
      );

      if (_audioFormat is null)
        throw new InvalidOperationException("Failed to create audio format");

      // Create source node
      _sourceNode = new(_audioFormat, (isSilencePtr, timestampPtr, frameCount, audioBufferListPtr) =>
      {
        unsafe
        {
          bool isSilence = *(bool*)isSilencePtr;
          AudioTimeStamp timestamp = *(AudioTimeStamp*)timestampPtr;
          AudioBufferList audioBufferList = *(AudioBufferList*)audioBufferListPtr;

          return RenderAudio(isSilence, timestamp, frameCount, audioBufferList);
        }
      });

      // Connect nodes
      _avAudioEngine.AttachNode(_sourceNode);
      _avAudioEngine.Connect(
        _sourceNode,
        _avAudioEngine.MainMixerNode,
        _audioFormat
      );

      // Prepare engine
      _avAudioEngine.Prepare();
    }

    partial void StartPlatformAudio()
    {
      if (_avAudioEngine is null || _avAudioEngine.Running)
        return;

      try
      {
        _avAudioEngine.StartAndReturnError(out NSError error);

        if (error is not null)
          throw new InvalidOperationException($"Failed to start audio engine: {error}");
      }
      catch (Exception ex)
      {
        _logger.LogError($"iOS audio start failed: {ex}");
        throw;
      }
    }

    partial void StopPlatformAudio()
    {
      if (_avAudioEngine is null || !_avAudioEngine.Running)
        return;

      _avAudioEngine.Stop();
    }

    partial void DisposePlatformAudio()
    {
      if (_avAudioEngine?.Running == true)
        _avAudioEngine.Stop();

      _sourceNode?.Dispose();
      _avAudioEngine?.Dispose();

      _sourceNode = null;
      _avAudioEngine = null;
      _audioFormat = null;
    }

    private unsafe int RenderAudio(
      bool isSilence,
      AudioTimeStamp timeStamp,
      uint frameCount,
      AudioBufferList audioBufferList
    )
    {
      if (_engine is null || isSilence)
        return 0;

      try
      {
        // Get the audio buffer
        AudioBuffer* buffer = audioBufferList.GetBuffer(0);
        if (buffer is null || buffer->Data == IntPtr.Zero)
          return -1;

        float* floatPtr = (float*)buffer->Data;

        // Validate frameCount
        if (frameCount > _iosRenderBuffer.Length)
          frameCount = (uint)_iosRenderBuffer.Length;

        // Fill temporary buffer from engine
        _engine.FillBuffer(_iosRenderBuffer.AsSpan(0, (int)frameCount));

        // Copy to output buffer
        for (int i = 0; i < frameCount; i++)
          floatPtr[i] = _iosRenderBuffer[i];

        return 0;
      }
      catch (Exception ex)
      {
        _logger.LogError($"iOS render error: {ex}");
        return -1;
      }
    }
  }
}
