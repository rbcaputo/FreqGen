# Preset Reference – Frequency Applications & Interpretations
This document describes commonly claimed therapeutic and cultural interpretations of specific audio frequencies.

**DISCLAIMER:**
> The information in this document represents **historical claims, cultural traditions, and reported subjective experiences** – not medical facts or scientific consensus.\
> ToneSync is a general-purpose audio signal generator. Any therapeutic effects are **user interpretations** of the DSP capabilities described in [DSP_REFERENCE](DSP_REFERENCE.md).\
> This is **not medical advice**. Do not use audio signals as a substitute for professional healthcare.

---

## Understanding the Terminology
When people refer to "healing frequencies", they typically mean one of three things:
1. **Modulation Rate** – The speed of amplitude variation (e.g. "10Hz alpha wave")
2. **Carrier Frequency** – The audible pitch (e.g. "528Hz Solfeggio tone")
3. **Binaural Beat** - The perceived difference between stereo frequencies

The actual audio implementation is described in the DSP reference. This document focuses on **what these frequencies claim to represent.**

---

## 1. Brainwave Entrainment Frequencies
These refer to **modulation rates** (not carrier frequencies) that correspond to documented neural oscillation bands.

### Reference Table – Brainwave Bands
| Band | Frequency Range | Claimed Effect | Implementation |
|------|-----------------|--------------------------|------------------------------|
| **Delta** | 0.5-4Hz | Deep sleep, physical restoration, unconscious processing | AM with slow modulation |
| **Theta** | 4-8Hz | Deep meditation, creativity, hypnagogic states, memory consolidation | AM or binaural beats |
| **Alpha** | 8-13Hz | Relaxed alertness, "flow-states", stress reduction, light meditation | AM or binaural beats |
| **Beta** | 13-30Hz | Active thinking, focus, analytical processing, alertness | AM with faster modulation |
| **Gamma** | 30-100Hz | High-level information processing, peak awareness, insight | Shallow AM (low-depth) |

### Implementation notes
** Carrier frequencies** for brainwave presets are typically:
* 100-500Hz range
* Often harmonically related
* The carrier is **not** the therapeutic claim – the **modulation rate** is

**Example:**
* Carrier: 220Hz (audible tone)
* Modulation: 10Hz (alpha band)
* Implementation: `x(t) = sin(2π·220t) · (1 + 0.6·sin(2π·10t))`

The "10Hz" refers to the amplitude variation, not the pitch.

### Envelope Characteristics
All brainwave presets use:
* **Attack:** 10-30 seconds (prevents startle response)
* **Release:** 30-60 seconds (gradual exit)

---

## 2. Solfeggio Frequencies
These refer to **carrier frequencies** (audible pitches) claimed to have specific effects. The origins are numerological/spiritual, not scientific.

### Reference Table - Solfeggio Scale
| Frequency | Claimed Effect | Common Usage | Implementation |
|-----------|-------------------|--------------|----------------|
| **174Hz** | Foundation, pain reduction, physical grounding | Restorative sessions | Pure tone or gentle harmonic |
| **285Hz** | Cellular healing, tissue repair, energy field work | Recovery-focused listening | Pure tone |
| **396Hz** | Liberation from guilt and fear, emotional release | Emotional processing work | Pure tone or layered |
| **417Hz** | Facilitating change, clearing negative patterns | Transition periods, new beginnings | Pure tone |
| **528Hz** | "Miracle tone", DNA repair, transformation, love | Most researched frequency, general wellness | Pure tone or harmonic stack |
| **639Hz** | Harmonizing relationships, communication, connection | Social harmony, empathy work | Pure tone |
| **741Hz** | Expression, problem-solving, awakening intuition | Creative work, self-expression | Pure tone |
| **852Hz** | Spiritual order, returning to truth, intuition | Spiritual practice, inner awareness | Pure tone |
| **963Hz** | Divine consciousness, unity, enlightenment | Advanced spiritual work | Pure tone (high frequency) |

### Implementation Notes
**Solfeggio presets typically use:**
* No modulation (pure tones)
* Single carrier or small harmonic stack
* Long envelopes (20-30 minute sessions)
* No rhythmic pulsing (continuous drone)

**Example:**
* Frequency: 528Hz
* No amplitude modulation, just envelope shaping
* Implementation: `x(t) = sin(2π·528t) · E(t)`

## Cultural Context
The Solfeggio scale has origins in:
* Gregorian chants
* Sacred music traditions
* Numerological interpretations (e.g. digit reduction to 3, 6, 9)

Modern claims about DNA repair and cellular effects are **not scientifically validated.**

---

## 3. Binaural Beats
These refer to **stereo frequency offsets** that create a perceived "beat" in the brain.

### Reference Table – Binaural Beat Applications
| Target Beat | Carrier Range | Claimed Effect | Presentation |
|-------------|---------------|--------------|----------------|
| **1-4Hz** (Delta) | 100-500Hz | Deep relaxation, sleep induction | Stereo headphones required |
| **4-8Hz** (Theta) | 200-500Hz | Meditation, creativity, memory | Stereo headphones required |
| **8-13Hz** (Alpha) | 200-600Hz | Stress relief, relaxed focus | Stereo headphones required |
| **13-30Hz** (Beta) | 300-600Hz | Alertness, concentration | Stereo headphones required |

### Implementation Notes
* **Left ear:** `L(t) = sin(2π·f_L·t)`
* **Right ear:** `R(t) = sin(2π·f_R·t)`
* **Perceived beat:** `f_beat = |f_R - f_L|`

**Example:**
* Left: 200Hz
* Right: 210Hz
* Perceived beat: 10Hz (Alpha range)

**Critical constraints:**
* **Must use headphones** (no speakers)
* Carrier frequencies typically below 1000Hz
* Beat frequency usually under 30Hz
* No modulation in waveform itself – the beat is a **perceptual phenomenon**

---

## 4. Isochronic Tones
These refer to **rhythmic pulsing** using high-depth amplitude modulation or square wave gating.

### Reference Table – Isochronic Applications
| Pulse Rate | Claimed effect | Common Usage | Implementation |
|------------|----------------|--------------|----------------|
| **1-4Hz** | Power naps, deep recovery | 10-20 minute session | High-depth (0.9-1.0) |
| **4-8Hz** | Creative surge, problem-solving | Brainstorming, artistic work | Hard gating or deep AM |
| **10-15Hz** | Mental clarity, stress relief | Post-work unwinding | Rhythmic pulses |
| **13-20Hz** | Focus, concentration, study | Cognitive performance | Sharp pulses |

### Implementation Notes
**Isochronic tones use:**
* High modulation depth (0.8-1.0)
* Sharp transitions (softened to avoid clicks)
* Shorter sessions (10-20 minutes typical)
* More intense than gentle brainwave AM

**Example (sine-based):**
* Carrier: 240Hz
* Modulation: 10Hz at 0.95 depth
* Result: Sharp pulsing without square wave harshness

**Example (true isochronic):**
* Square wave gating at target frequency
* Low-pass filtering to prevent clicks
* More aggressive than sine AM

### Key Difference from Brainwave AM
* **Brainwave AM:** Gentle, smooth (depth 0.3-0.7)
* **Isochronic:** Sharp, rhythmic (depth 0.8-1.0)

---

## 5. Vibroacoustic/Sub-Bass (edge case)
Frequencies below 80Hz that work through **mechanical sensation** more than hearing.

### Reference Table – Vibroacoustic Applications
| Frequency Range | Claimed Effect | Medium | Notes |
|-----------------|----------------|--------|-------|
| **20-40Hz** | Physical relaxation, muscle tension | Speakers + body contact | Felt more than heard |
| **40-80Hz** | Deep meditation, altered states | Subwoofers or specialized equipment | Requires specific hardware |

### Implementation Notes
* Pure sine waves at sub-bass frequencies
* Requires speakers capable of reproducing low frequencies
* Often combined with higher-frequency carriers

---

## 6. Design Principles for Preset Creation

### 6.1 Envelope Requirements (universal)
| Envelope Type | Typical Duration | Purpose |
|---------------|------------------|---------|
| **Slow Linear Fade** | 30-120 seconds | Prevents startle response |
| **ADSR (soft attack)** | 10-30 seconds | Musical shaping for tonal presets |
| **Macro envelope** | Minutes-scale | Long-form session variation |

**Rule:** Never use instant on/off. The human auditory system requires gradual transitions.

### 6.2 Layer Combinations
**Typical Layer Strategies:**
1. **Single pure tone** – Solfeggio frequencies, simplicity
2. **Carrier + AM** – Brainwave entrainment, therapeutic sessions
3. **Multi-layer blend** – Harmonic richness, complex soundscapes
4. **With noise bed** – Masks repetition, reduces fatigue

### 6.3 Session Duration Guidelines
| Preset Type | Recommended Duration | Rationale |
|-------------|----------------------|-----------|
| **Brainwave** | 20-45 minutes | Relaxation and meditation sessions |
| **Solfeggio** | 20-30 minutes | Continuous tones require longer exposure |
| **Binaural** | 15-30 minutes | Headphone fatigue consideration |
| **Isochronic** | 10-20 minutes | More intense, shorter bursts |

---

## 7. What Makes a Preset "Beginner-Friendly"
**Safe presets for first-time users:**
* Low modulation depth (0.3-0.6)
* Slow modulation rates (under 15Hz)
* Long envelopes (30+ seconds)
* Mid-range carriers (200-400Hz)
* No isochronic pulsing

**Avoid for beginners:**
* High-depth AM (0.8+)
* Fast modulation (20Hz+)
* High carrier frequencies (above 800Hz)
* Aggressive isochronic pulsing

---

## 8. Core Engineering Insight
**Strip away the labels, and every system reduces to:**
> A stable carrier + slow time-domain modulation + gentle transitions

The "healing" part is **never** the carrier frequency.\
The only **cognitively meaningful** parameters are:
1. **Modulation rate** (the rhythm of change)
2. **Temporal smoothness** (no sudden jumps)
3. **Stability over time** (no drift or jitter)

 The math is trivial. The **perceptual design** is not.

 ---

 ## 9. Further Reading
 For the pure mathematical implementation of these concepts, see:
 * [DSP_REFERENCE.md](DSP_REFERENCE.md) – Signal processing foundations
 * [README.md](/README.md) – Architecture and philosophy

For scientific research on auditory entrainment:
* Search academic databases for "auditory driving", "brainwave entrainment", "binaural beats"
* **Note:** Research quality varies significantly in this field

---

## Final Note on Interpretation
ToneSync provides the **tools** to generate these signals with precision and clarity.\
What they **mean** to you – whether therapeutic, meditative, creative, or simply interesting sounds - is entirely your choice.\
The engine doesn't make claims. It makes **signals.**
