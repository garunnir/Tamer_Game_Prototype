# Effects System Decisions (2026-03-02)

## EffectManager Architecture
- **Singleton** (no DontDestroyOnLoad — scene-local is fine for a prototype with one scene)
- All five hit effects triggered via `TriggerHitEffect(victim, hitPos, attacker?)` from TakeDamage
- Attacker hitstop triggered explicitly via `TriggerHitstop(attacker)` from melee PerformAttack

## Trigger Points
- **Victim-side** (hitlag + shake + flash + spark): MonsterBase.TakeDamage, FlockUnitCombat.TakeDamage
- **Attacker hitstop**: MonsterA.PerformAttack, BossA.PerformAttack, BossB.PerformAttack, BossB.UpdateCharging
- **Ranged attacker hitstop skipped** (Projectile has no attacker ref; acceptable for prototype)

## IHitReactive Interface
- Added to Scripts/Combat/ — keeps motion suspension decoupled from EffectManager
- Implemented by MonsterBase and FlockUnit (both need motion freeze)
- EffectManager calls via `victim.Transform.GetComponent<IHitReactive>()`
- FlockUnitCombat is the ICombatant, FlockUnit on same GO provides IHitReactive — correct

## Spark Pool
- `ParticleSystem.stopAction = Disable` — auto-returns pool slot when playback ends
- No Update loop needed for recycling
- Procedural default spark if no prefab assigned (yellow-orange burst, 12 particles, 0.35s lifetime)

## Color Flash
- MaterialPropertyBlock — never modifies shared materials
- Coroutine on EffectManager GO (not victim) — survives victim death/disable
- Restores original property block after _flashDuration
