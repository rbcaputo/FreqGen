# ToneSync
ToneSync is a cross-platform, real-time audio signal generation engine focused on **precise, controllable, and deterministic DSP (Digital Signal Processing).**

It is designed as a **general-purpose tone, frequency, and modulation engine**, suitable for:
* Audio experimentation
* Sound design and synthesis
* Signal research and education
* Generative audio systems
* Prototyping DSP pipelines for mobile and desktop

> The *therapeutic/frequency-based* use case is **not a claim**, product promise, or built-in assumption.\
> It is presented purely as **one possible application** of the DSP capabilities.

ToneSync prioritizes **clarity, correctness, and architectural honesty** over marketing abstractions.

---

## Core Principles
ToneSync is built around a few non-negotiable ideas:
* **DSP first** – The signal chain is explicit and inspectable
* **No magical behavior** – Initialization, playback, and state transitions are user-controlled
* **Separation of concerns** – Core DSP, application logic, and UI are cleanly isolated
* **Deterministic audio** – Sample-accurate, predictable behavior
* **Cross-platform by design** – Targeting Android and iOS via .NET

If the engine does something, you should be able to point to the line of code that makes it happen.

---

## Architecture Overview
ToneSync follows a layered architecture:

### Core Layer (`ToneSync.Core`)
The Core layer is platform-agnostic and contains **no UI or device code.**

Responsibilities:
* Oscillators (sine, composite, future extensibility)
* Modulation and envelopes
* Gain staging (master vs output)
* Signal graph composition
* Sample-accurate processing

This layer is fully testable and portable.

### App Layer (`ToneSync.App`)
The App layer coordinates **user intent** with the DSP engine.

Responsibilities:
* Audio service lifecycle
* Preset orchestration
* Output routing (headphones, external speaker, device speaker)
* State management (initialized, playing, stopped)

It does **not** implement DSP logic.

### UI Layer
The UI reflects engine state honestly:
* Explicit initialization
* Clear playback state
* No hidden automation

The UI does not "drive" the DSP – it requests actions.

---

## Quick Example
```csharp
// Core DSP (platform-agnostic)
var engine = new AudioEngine(AudioSettings.SampleRate);

// Define layer configuration
var config = new LayerConfiguration
{
  CarrierFrequency = 440.0f,  // A4 note
  ModulatorFrequency = 10.0f, // 10Hz alpha wave
  ModulatorDepth = 0.6f,      // 60% modulation depth
  Weight = 1.0f,              // Full volume
  IsActive = true
};

// Initialize engine with configuration
engine.Initialize([config], attackSeconds: 10f, releaseSeconds: 30f);

// Start playback
engine.Start();

// Platform audio callback (hot path - runs on audio thread)
float[] buffer = new float[1024];
engine.FillBuffer(buffer);

// Stop playback (triggers release phase)
engine.Stop();
```

For preset-based workflows:
```csharp
// Using the Presets layer
var engine = new AudioEngine();
var presetEngine = new PresetEngine(engine);

// Load and play a brainwave preset
var preset = BrainwavePresets.Bw_Alpha_Relaxation;
presetEngine.LoadPreset(preset);
presetEngine.StartPlayback();

// Stop gracefully
presetEngine.StopPlayback();
```

## Gain Model (Important)
ToneSync intentionally separates gain in two concepts:
* **Master Gain**
  * Overall amplitude control
  * User-facing volume
  * Applied late in the signal chain
* **Output Gain/Profile**
  * Hardware or routing compensation
  * Mono behavior
  * Channel balancing

This separation avoids common audio pitfalls and keeps signal math sane.

---

## Presets
Presets are **data**, not behavior.

A preset may define:
* Base frequency
* Modulation parameters
* Suggested envelopes
* Descriptive metadata

What the sound *means* is left entirely to the application or the user.

---

## Therapeutic Use Disclaimer
ToneSync does **not:**
* Claim medical efficacy
* Provide diagnosis or treatment
* Replace professional healthcare

Any references to relaxation, focus, or therapeutic contexts are **illustrative only** – examples of how controlled audio signals *might* be applied.
The engine itself is neutral.

---

## Technology Stack
* .NET 10
* C# 14
* Platform-specific audio APIs:
  * Android: AudioTrack/OpenSL ES
  * iOS: AVAudioEngine
* .NET MAUI for UI (App layer only)

The Core DSP layer has **zero dependencies** on MAUI or platform APIs.

---

## Status
ToneSync is currently in **prototype stage:**
* Core DSP API is stabilizing
* Breaking changes may occur until v1.0
* Suitable for experimentation, extension, and learning
* Not yet production-ready for consumer applications

---

## Contributing
Contributors are welcome, especially in:
* DSP correctness and algorithm extensions
* Unit testing and validation
* Platform audio backend optimization
* Documentation and examples
* Performance profiling

**Design discussions should focus on signal integrity and architectural simplicity.**
Please open an issue before major refactoring to discuss approach.

---

## Philosophy
ToneSync exists because audio systems are often:
* Over-abstracted
* Under-documented
* Magically stateful

This project aims to be the opposite.\
If you are curious about sounds, signals, and structure – you are in the right place.

---

## Documentation
For detailed technical documentation:
* [DSP_REFERENCE.md](Docs/DSP_REFERENCE.md) – Complete DSP theory, mathematical foundations, and implementation details.
* [PRESET_REFERENCE.md](Docs/PRESET_REFERENCE.md) – Frequency applications and cultural context

The DSP reference covers:
* Signal processing mathematics and formulas
* Oscillator, envelope, and modulation theory
* Mobile audio constraints and optimization strategies
* DSP graph architecture and real-time safety principles

The Preset reference covers:
* Brainwave, Solfeggio, and isochronic frequency interpretations
* Cultural and historical context for "healing frequencies"
* Design principles for creating effective presets
* Beginner-friendly preset guidelines

**Contributors working on Core DSP should read the reference documentation first.**

---

## License
Open source.\
MIT License as defined in the repository [LICENSE](/LICENSE).
