# Reference Table - Commonly Claimed "Healing" Frequencies \& Their Audio Implementations

| Category | Target Frequency | Refers To | Typical Audio Presentation | Envelope/Modulation | Core Math (Simplified) |
|----------|------------------|-----------|----------------------------|---------------------|------------------------|
| Brainwave (Delta) | 0.5-4 Hz | Neural oscillation band | Audible carrier with slow AM | Smooth sine AM, very slow attack/release | `x(t)=sin(2πf_c t).(1+α.sin(2πf_m t))` where `f_m≈1-4` |
| Brainwave (Theta) | 4-8 Hz | Neural oscillation band | AM pads, binaural beats | Sine AM or binaural phase offset | Same as above, `f_m≈4-8` |\
| Brainwave (Alpha) | 8-13 Hz | Relaxed alertness band | AM tones, binaural beats | Gentle AM, long fades | Same AM equation, `f_m≈8-13` |
| Brainwave (Beta) | 13-30 Hz | Focus/alertness | Faster AM or rhythmic gating | AM or isochronic pulses | `x(t)=sin(2πf_c t).square(2πf_m t)` |
| Brainwave (Gamma) | 30-100 Hz | High-frequency neural sync | Rarely used; can be fatiguing | Shallow AM only | `x(t)=sin(2πf_c t).(1+α.sin(2πf_m t))`, `α` small |

---

## Binaural Beat Implementations

| Category | Target Beat | Carrier Frequencies | Presentation | Envelope | Core Math (Simplified) |
|----------|-------------|---------------------|--------------|----------|------------------------|
| Binaural Beat | 4-8 Hz | ~200-500 Hz | Stereo headphones only | Slow fades | Left: `sin(2πf_1 t)` Right: `sin(2πf_2 t)` Beat = ` |
| Binaural Beat | 8-10 Hz | <1000 Hz | Stereo | ADSR or slow ramp | Same as above |
| Binaural Beat | 12-15 Hz | ~300-600 Hz | Stereo | Moderate envelope | Same as above |

Important: **no modulation exists in the waveform itself.** The beat is a **neural interference effect.**

---

## Solfeggio Frequencies (Cultural/Numerological)

| Named Frequency | Claimed meaning | Actual Use | Presentation | Envelope | Core Math (Simplified) |
|-----------------|-----------------|------------|--------------|----------|------------------------|
| 396 Hz | "Fear release" | Carrier tone | Pad/drone | Long attack & release | `x(t)=A(t).sin(2π.396t)` |
| 417 Hz | "Change" | Carrier tone | AM or ambient layer | Slow macro fade | Same as above |
| 528 Hz | "DNA repair" | Carrier tone | AM pad | Sine AM 4-10 Hz | `sin(2π.528t).(1+α.sin(2πf_m t))` |
| 639 Hz | "Connection" | Carrier tone | Harmonic stack | ADSR | Sum of sines |
| 741 Hz | "Expression" | Carrier tone | Layered | Envelope shaping | Same as above |
| 852 Hz | "Intuition" | Carrier tone | Ambient texture | Envelope smoothing | Same as above |

---

## Isochronic Tones (Hard Modulation)

| Category | Target Frequency | Presentation | Envelope | Core Math (Simplified) |
|----------|------------------|--------------|----------|------------------------|
| Isochronic | 1-4 Hz | Audible pulses | Fade-in/out to avoid shock | `x(t)=sin(2πf_c t).square(2πf_m t)` |
| Isochronic | 8-12 Hz | Pulsed tone | Softened square (rounded edges) | Same as above |
| Isochronic | 15-30 Hz | Rhythmic gating | Short envelopes | Same as above |

Square waves are often **low-pass filtered** to reduce harshness.

---

## Vibroacoustic/Sub-Bass (Edge Case)

| Category | Target Frequency | Medium | Envelope | Core Math (Simplified) |
|----------|------------------|--------|----------|------------------------|
| Vibroacoustic | 20-80 Hz | Physical vibration | Continuous or pulsed | `sin(2πft)` |
| Sub-bass audio | 20-40Hz | Speakers + body coupling | Long ramps | Same as above |

This works more through **mechanical sensation** than hearing.

---

## Envelope Types Used (Across All Categories)

| Envelope Type | Purpose |
|---------------|---------|
| Slow Linear Fade (30-120s) | Prevents startle response |
| ADSR (soft attack) | Musical shaping |
| Macro envelope (minutes) | Long-form sessions |
| No envelope | Almost never used intentionally |

---

## Core Insight (Engineering Reality)

* The "healing" part is never the carrier frequency
* The only cognitively meaningful numbers are:
* Modulation rate
  * Temporal smoothness
  * Stability over time
* The math is trivial; the perception design is not

If we strip away labels, every one of these systems reduces to:
`A stable carrier + slow time-domain modulation + gentle transitions`

---

## 1. The Core Abstraction

Every signal we care about can be modeled as:

```mathematics
x(t) = E(t) i=1∑N w_i C_i (t) M_i (t)
```

Where:

* `E(t)` = macro envelope (minutes-scale shaping)
* `C_i (t)` = carrier signal(s)
* `M_i (t)` = modulation signal(s)
* `w_i` = layer weights
* `N` = number of layers

This alone already subsumes **AM tones**, **binaurals**, **isochronics**, **drones**, **pads**, **noise beds**.

---

## 2. Carrier Model

A carrier is any audible waveform.

### 2.1 Pure sine carrier

```mathematics
C(t) = sin(2πf_c t + ϕ)
```

Parameters:

* `f_c`: carrier frequency (e.g. 200-800 Hz typical)
* `ϕ`: phase (usually 0)

---

### 2.2 Harmonic carrier (preferred in practice)

```mathematics
C(t) = k=1∑K a_k sin(2πkf_c t)
```

Why this matters:

* Reduces listening fatigue
* Sounds "full" instead of clinical
* Still mathematically clean

---

## 3. Modulation Model

This is where the **meaningful** frequencies live.

### 3.1 Amplitude modulation (smooth)

```mathematics
M(t) = 1 + αsin(2πf_m t)
```

Constraints:

* 0 <= `α` <= 1
* `f_m` ∈ \[0.5, 30] Hz typically

This is our **default** modulator.

---

### Isochronic (hard gating)

```mathematics
M(t) = gate(f_m, d)
```

Where:

* `gate` is a square wave
* `d` is duty cycle (0.3-0.7 common)

In practice, we soften this is low-pass filter or smoothstep.

---

### 3.3 Binaural modualtion (stereo only)

Left:

```mathematics
x_L(t) = sin(2πf_L t)
```

Right:

```mathematics
x_R(t) = sin(2πf_R t)
```

Perceived beat:

```mathematics
f_b = |f_L - f_R|
```

Important:

* No explicit `M(t)`
* Envelope still applies

---

## 4. Envelope Model (Non-Negotiable)

No serious system omits this.

### 4.1 Exponential fade-in/fade-out

```mathematics
       |1 - e^-t/τ_a        t < T_on
E(t) = {1                   T_on <= t <= T_off
       |e^-(t-T_off)/τ_r    t > T_off
```

Parameters:

* `τ_a`: attack time constant
* `τ_r`: release time constant

The are **tens of seconds**, not milliseconds.

---

### 4.2 Macro envelopes (session shaping)

We let the amplitude slowly drift over minutes:

```mathematics
E_macro (t) = 0.5 + 0.5 sin(2πf_macro^t)
```

Where:

* `f_macro` << 1 Hz (e.g one cycle every 10-30 minutes)

---

## 5. Layering Model

Now we compose.

```mathematics
x(t) = i=1∑N E_i (t) C_i (t) M_i (t)
```

Typical layer set:

**1.** Low carrier + slow AM (felt)
**2.** Mid carrier + harmonic stack (presence)
**3.** Noise bed (masking)
**4.** Optional binaural layer

Noise example:

```mathematics
C_noise (t) = pink_noise()
```

---

## 6. Stability Constraints (Critical)

A generator that **wanders** breaks effect.

Rules:

* Carrier frequencies are fixed or glide slowly
* Modulation frequency does not jitter
* Phase resets are avoided
* No sudden parameter jumps

Mathematically:

```mathematics
df/dt ≈ 0
```

Unless deliberately ramped.

---

## 7. Minimal Parameter Set (What The Engine Actually Needs)

Strip everything down and we get:

* Carrier frequency `f_c`
* Modualation frequency `f_m`
* Modulation depth `α`
* Envelope attack / release
* Layer weights
* Stereo offset (optional)

---

## 8. Mental Model

We are generating:
`Slowly varying, low-information audio fields that the auditory system can synchronize to without resisting.`

The system works because:

* The signal is predictable
* The changes are slow
* The brain doesn't fight it

---

# DSP Graph

## 1. Global Frame

All nodes operate in discrete time:

* Sample rate: `f_s` (e.g. 44 100 Hz)**
* Time step: `Δt = 1/f_s`**
* Sample index: `n`**
* `t = n/f_s`**

Every node produces one sample per tick.

---

## 2. High-Level DSP Graph (Textual)

```text
[LFO(s) (modulation params)]-->[Modulator Bank]-->[[Carrier Osc(s)]]-->[Multiplier]-->[Envelope]-->[Layer Output]-->(repeat per layer, then sum)-->[Mixer/Sum]-->[Output]
```

---

## 3. Oscillator Nodes (Carriers)

### 3.1 Sine Oscillator

State:

* Phase `ϕ`

Update per sample:

```mathematics
ϕ_n+1 = ϕ_n + 2π(f_c/f_s)
```

Output:

```mathematics
C[n] = sin(ϕ_n)
```

Notes:

* Phase wraps at `2π`
* Frequency changes must be **smoothed**

---

### 3.2 Harmonic Oscillator (Optional)

Multiple sine oscillators summed:

```mathematics
C[n] = k=1∑K a_k sin(kϕ_n)
```

Implemented as:

* One phase accumulator
* Multiple sine calls or lookup table

---

## 4. LFO Nodes (Low-Frequency Oscillators)

Same oscillator structure, but:

* `f_m` << 20 Hz
* Output range usually normalized to [-1, 1]

Common waveforms:

* Sine (default)
* Triangle (slightly more "organic")
* Smoothed square (isochronic)

Example output:

```mathematics
L[n] = sin(2πf_m n/f_s)
```

---

## 5. Modulator Bank

Takes LFO output and converts it into **multipliers**.

### 5.1 Smooth AM

Input:

* `L[n] ∈ [-1, 1]`

Transform:

```mathematics
M[n] = 1 + α.L[n]
```

Guarantees:

* `M[n] >= 0`
* No polarity inversion

---

### 5.2 Isochronic Gate

Input:

* Square LFO

Transform:

```mathematics
       |1   L[n] > 0
M[n] = {
       |0   L[n] <= 0
```

Then:

* Pass through **slew limiter** or low-pass filter
* Prevents clicks

---

## 6. Envelope Node (Macro Control)

This is **not ADSR** in the musical sense. It's slower.

State machine:

* Attack
* Sustain
* Release

### 6.1 Attack phase

```mathematics
E[n] = 1 - e^-n/(𝜏_a f_s)
```

### 6.2 Release phase

```mathematics
E[n] = e^-(n-n_off)/(𝜏_r/f_s)
```

Key property:

* Envelope is **monotonic**
* No fast edges

---

## 7. Layer Output Node

This is where the math becomes literal:

```mathematics
Y[n] = C[n].M[n].E[n]
```

Each layer is **self-contained**:

* One carrier
* One modulation path
* One envelope

This is intentional. No shared state across layers.

---

## 8. Noise Layer (Optional but Useful)

Pink or brown noise generator:

```mathematics
Y_noise [n] = E[n].noise()
```

Used to:

* Mask tonal fatigue
* Reduce perceived repetition

---

## 9. Mixer Node

Summation with normalization:

```mathematics
Y_mix [n] = i=1∑N w_i Y_i [n]
```

Then:

```mathematics
Y_out [n] = clamp(Y_mix [n], -1, 1)
```

Better:

* Soft limiter
* Headroom (-6 dB)

---

## 10. Stereo Graph (If Needed)

For binaural layers:

```text
Carrier --> Envelope --> Left Mixer
Carrier' --> Envelope --> Right Mixer
```

With:

```mathematics
f_R = f_L + Δf
```

Everything else is identical.

---

## 11. Control-Rate vs Audio-Rate

Important design boundary:

* Oscillators, mixers → audio-rate
* LFOs, envelopes → can be control-rate (e.g. 100 Hz)
* Parameter smoothing → control-rate

This saves CPU and improves stability.

---

## 12. Graph Invariants (Rules We Enforce)

* No node introduces discontinuities
* No parameter jumps without smoothing
* All multipliers remain bounded
* Layers are additive, not interdependent

---

# Application Design

## 1. Mobile Audio Constraints

On Andoid and iOS we do **not** own the audio device. We feed buffers to the OS, on its schedule.

That implies:

* We must be real-time safe
* No allocations in the audio callback
* No locks
* No LINQ, no events, no async inside DSP
* Deterministic CPU usage per buffer

Our DSP graph must be ***pull-based and stateless per sample**, with minimal mutable state.

---

## 2. Platform Strategy (Important)

With C# .NET 8, the options are:

* **.NET MAUI** for UI
* **Platform-native audio backends**, bridged into shared DSP code

Typical choices:

* Android → `AudioTrack` (low-latency mode)
* iOS → `AVAudioEngine`/`AVAudioSourceNode`

Our DSP lives in:
`Pure .NET Standard-compatible code`
No platform dependencies. No UI knowledge.

---

## 3. Separation of Concearns (Non-Negotiable)

```text
[MAUI UI Layer]-->[Control/Params (thread-safe, smoothed)]-->[DSP Engine (pure math)]-->[Platform Audio API]
```

The DSP engine:

* Knows nothing about Android or iOS
* Only outputs `float` samples
* Is driven by a "render buffer" call

---

## 4. DSP Graph --> Object Graph

Translate the DSP node into **small**, **predictable classes**.

**Core interfaces**

```csharp
public interface IAudioNode
{
  float NextSample();
}
```

---

## 5. Oscillators (Audio-rate)

```csharp
public sealed class SineOscillator : IAudioNode
{
  private float _phase;
  private float _phaseIncrement;

  public void SetFrequency(float frequency, float sampleRate)
  {
    _phaseIncrement = 2f * Math.PI * frequency / sampleRate;
  }

  public float NextSample()
  {
    var sample = MathF.Sin(_phase);
    _phase += _phaseIncrement;
    if (_phase > Math.PI * 2f)
    {
      _phase -= Math.PI * 2f;
    }

    return sample;
  }
}
```

This runs at 44.1k or 48k on mobile.

---

## 6. LFOs (Control-rate)

Do **not** tick LFOs at audio rate.
Instead:

* Update them every N samples (e.g. every 64)
* Cache their output

```csharp
public sealed class Lfo
{
  private float _sample;
  private int _counter;

  public float Sample => _sample;

  public void Tick()
  {
    if (++counter % 64 != 0)
    {
      return;
    }
    _sample = /* compute slow sine */;
  }
}
```

This saves battery and avoids pointless precision.

---

## 7. Modulators (Multipliers)

These are cheap and stateless.

```csharp
public sealed class AmModulator
{
  public float Depth; // α

  public float Apply(float carrier, float lfo)
  {
    return carrier * (1f + Depth * lfo);
  }
}
```

---

## 8. Envelopes (Stateful, Slow)

One envelope per layer.

```csharp
public sealed class Envelope
{
  private float _sample;
  private float _attackCoef;
  private float _releaseCoef;
  private bool _isActive;

  public float Next()
  {
    _sample += (
      _isActive ? (1f - _sample) * _attackCoef
                : (0f - _sample) * _releaseCoef
    );

    return _sample;
  }
}
```

Attack/release coefficients are precomputed.

---

## 9. Layer = Small DSP Graph

Each layer composes nodes:

```csharp
public sealed class Layer
{
  public SineOscillator Carrier;
  public Lfo Lfo;
  public Envelope Envelope;
  public float Weight;

  public float NextSample()
  {
    var c = Carrier.NextSample();
    var m = 1f + Lfo.Sample;
    var e = Envelope.Next();

    return c * m * e * Weight;
  }
}
```

No dynamic allocation. No branching explosions.

---

## 10. Mixer (Final Stage)

```csharp
public sealed class Mixer
{
  private Layers[] _layers;

  public float NextSample()
  {
    float sum = 0f;
    for (int i = 0; i < _layers.Length; i++)
    {
      sum += _layers[i].NextSample();
    }

    return Math.Clamp(sum, -1f, 1f);
  }
}
```

Optional:

* Soft limiter
* Headroom scaling

---

## 11. Audio Callback Contract

Platform side asks for buffers:

```csharp
public void Render(float[] buffer)
{
  for (int i = 0; i < buffer.Length; i++)
  {
    buffer[i] = _mixer.NextSample();
  }
}
```

This is the **only** place where samples are generated.

---

# 12. Parameter Changes (UI → DSP)

Never write directly into DSP nodes from UI.
Instead:

* UI writes to a thread-safe parameter store
* DSP reads smoothed values

Example:

```csharp
carrierFrequency += (targetFrequency - carrierFrequency) * 0.001f;
```

No locks. No atomics if we can avoid them.

---

## 13. Battery & Thermal

Mobile devices punish:

* Excessive trigger calls
* Per-sample allocations
* High-rate modulation

Mitigations:

* Lookup tables for sine
* Control-rate LFOs
* Few layers (2-4 max)
* Avoid stereo unless needed

This engine can run **for hours** without depleating a phone if done right.

---

## 14. What Is Being Built (Conceptually)

We now aim to have:

* A deterministic DSP graph
* A mobile-safe execution model
* A clean separation between sound design and UI
* A system that can express every "frequency" scheme discusses earlier

---
