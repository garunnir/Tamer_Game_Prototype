# Decisions — BossB

- BossB.UpdateAttack() fully overridden — base._attackTimer is private; need independent _meleeTimer and charge cooldown (same pattern as BossA)
- ChargePhase private enum inside BossB class — no new MonsterState needed; charge is a sub-state within Attack, not a top-level state
- Charge triggers only from Attack state (boss must first enter melee range) — keeps state machine simple; charge still travels 10u past the player
- _chargeCooldown initialized in Awake(), reset in UpdateChargeRecovery() — never reset in EnterAttack() so cooldown carries across Chase↔Attack transitions
- Charge direction locked at WINDUP start (_windupAnchor saved, _chargeDirection normalized) — gives player 0.8s to sidestep the incoming dash
- UpdateChargeWindup uses Random.Range per frame (chaotic rumble) not sin-wave — better telegraphs burst, simpler code
- transform.position = _windupAnchor snap at windup-end before EnterCharging() — prevents drift accumulation from per-frame vibration
- _chargeDirection.y = 0 enforced at windup entry (toTarget.y = 0f before normalize) — boss stays grounded during charge
- _chargeMaxDistanceSqr pre-cached in Awake() — avoids per-frame multiply in UpdateCharging's hot path
- Contact damage check (sqrDist <= _mySqrAttackRange) runs every charge frame; _chargeDamageDealt flag ensures single hit per charge even if boss stays in range
- Stale _chargePhase bug fix: when target dies/is-lost at top of UpdateAttack, clear _chargePhase = None and restore _windupAnchor if in Windup — prevents old windup resuming on next engagement
- After charge ends, boss is likely out of melee range → sqrDist > _mySqrAttackRange → EnterChase(); boss runs back — creates dodge-counter-chase rhythm
