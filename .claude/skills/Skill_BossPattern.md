# SKILL: Add Boss Pattern

> Use this template when adding a new special pattern to a boss monster

---

## Prompt Template

```
Read CLAUDE.md first.

Add a new special pattern to [boss name].

## Pattern Info
- Boss file: [BossA.cs / BossB.cs]
- Pattern name: [e.g. AoeSlam, Charge, SummonMinion]
- Pattern type: [AoE ground / charge / summon / explosion / other]
- Trigger condition: [below X% HP / every Ns cooldown / phase transition]
- Pattern description: [how it works in detail]
- Warning effect: [yes / no — if yes, describe]
- Duration: [Ns]
- Damage: [value]

## Requirements
- Add new state to existing state machine
- Warning effect handled via coroutine
- Invincibility during pattern: [yes / no]
- Use object pooling for any spawned objects (ground markers, projectiles)
- Update CLAUDE.md Memory after completion
```

---

## Example — AoE Slam

```
Read CLAUDE.md first.

Add AoE ground slam pattern to BossA.

## Pattern Info
- Boss file: BossA.cs
- Pattern name: AoeSlam
- Pattern type: AoE ground slam
- Trigger condition: every 8s cooldown
- Pattern description: place 3 ground slam zones sequentially at player position, explode after 1.5s each
- Warning effect: yes — red circle shown at zone for 1.5s before explosion
- Duration: 1.5s warning + 0.3s explosion
- Damage: 25
```

---

## Example — Charge

```
Read CLAUDE.md first.

Add charge pattern to BossB.

## Pattern Info
- Boss file: BossB.cs
- Pattern name: Charge
- Pattern type: charge
- Trigger condition: below 70% HP, every 6s cooldown
- Pattern description: high-speed charge toward player, stops after max 2s or 10f
- Warning effect: yes — vibration effect for 0.8s before charge
- Duration: max 2s during charge
- Damage: 30
```
