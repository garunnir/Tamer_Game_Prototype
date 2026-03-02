# SKILL: Add Effect / Visual Feedback

> Use this template when adding hit feel, taming, death, or other visual feedback

---

## Prompt Template

```
Read CLAUDE.md first.

Add the following visual feedback effect.

## Effect Info
- Effect name: [e.g. HitEffect, TamingSuccess, MonsterDeath]
- Trigger: [when does it fire]
- Effect types (check all that apply):
  - [ ] Particle effect
  - [ ] UI text popup
  - [ ] Camera shake
  - [ ] Hitstop (time freeze)
  - [ ] Hitlag (motion freeze)
  - [ ] Color flash (hit blink)
  - [ ] Sound playback
  - [ ] Screen effect (vignette etc.)
- Intensity / duration: [per item]
- How to call: [which script calls it and how]

## Requirements
- Particles use object pooling
- Hitstop: NO Time.timeScale — affect target objects only
- Add to EffectManager.cs singleton (create if not exists)
- Update CLAUDE.md Memory after completion
```

---

## Example — Hit Effect

```
Read CLAUDE.md first.

Add hit feedback when a monster takes damage.

## Effect Info
- Effect name: HitEffect
- Trigger: immediately after damage dealt in CombatSystem
- Effect types:
  - [x] Particle: spark at hit position (object pooling)
  - [x] Camera shake: light shake for 0.1s
  - [x] Hitstop: both attacker and target freeze for 0.05s (objects only, no timeScale)
  - [x] Hitlag: hit target motion stops for 0.1s
  - [x] Color flash: hit target flashes red for 0.1s
- Call from: CombatSystem.cs immediately after damage
```

---

## Example — Taming Success Effect

```
Read CLAUDE.md first.

Add taming success visual feedback.

## Effect Info
- Effect name: TamingSuccessEffect
- Trigger: immediately after taming confirmed in TamingSystem
- Effect types:
  - [x] Particle: heart or star particles surround monster for 2s (object pooling)
  - [x] UI text popup: "Tamed!" floats up and fades out after 1.5s
  - [x] Sound: success SFX slot (connection only)
- Call via: TamingSystem OnTamingSuccess event
```
