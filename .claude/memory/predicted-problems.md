# Predicted Problems

## MonsterB (2026-03-02)

| # | Problem | Mitigation |
|---|---------|------------|
| 1 | `_sqrAttackRange` is `private` in MonsterBase — MonsterB.UpdateChase() overrides the base entirely and needs it | Cache locally as `_mySqrAttackRange = _data.AttackRange * _data.AttackRange` in MonsterB.Awake() after base.Awake() |
| 2 | `_data` may be null at Awake if inspector not set — dereferencing `_data.AttackRange` would throw NullRef | Guard: `if (_data == null) return;` in MonsterB.Awake(), after calling base.Awake() (base already logs warning) |
| 3 | Zigzag amplitude ≥ attack range means monster circles target without ever entering attack range | Keep default amplitude (3f) well below attack range (4f); document in Inspector tooltip |
| 4 | PerformAttack() calls EnterChase() mid-UpdateAttack() — then UpdateAttack() continues to run `_attackTimer = _data.AttackCooldown` on a now-Chase state | Harmless: EnterAttack() always resets timer to 0 on re-entry; the cooldown assignment is a no-op |
| 5 | Retreat direction while target is moving — retreat point is calculated relative to current position, not world-fixed | Recalculate each frame (dynamic): `retreatPoint = transform.position - toTarget.normalized * retreatDistance * 2f`; monster always flees directly away |
| 6 | Zigzag timer not reset on EnterChase() — if monster re-enters Chase mid-wave, it continues mid-cycle, which is fine but start-of-cycle is cleaner | Reset `_zigzagTimer = 0f` in EnterChase() override |

## MonsterC (2026-03-02)

| # | Problem | Mitigation |
|---|---------|------------|
| 1 | AoE needs to hit multiple allies — `Projectile` only hits one `ICombatant` | Added `CombatSystem.DealAoeDamage()` — iterates registry, applies radius check, calls TakeDamage |
| 2 | `DealAoeDamage` iterates `_allies`; TakeDamage can kill units → `UnregisterCombatant` → `List.Remove` mid-foreach → `InvalidOperationException` | Iterate `new List<ICombatant>(registry)` snapshot inside the method |
| 3 | `TakeDamage()` not virtual — cannot override to detect 30% HP threshold | Compute `_currentHP / (float)_data.MaxHP < _enrageHpThreshold` dynamically each frame in `UpdateChase()` |
| 4 | `_data.MaxHP` is `int`; `_currentHP / _data.MaxHP` → integer division always 0 | Cast: `_currentHP / (float)_data.MaxHP` |
| 5 | `_sqrAttackRange` private in MonsterBase | Cache locally as `_mySqrAttackRange` in Awake() — same pattern as MonsterB |

## BossA (2026-03-02)

| # | Problem | Mitigation |
|---|---------|------------|
| 1 | `_attackTimer` is `private` in MonsterBase — BossA needs independent melee + slam timers | Override `UpdateAttack()` entirely; use `_meleeTimer` (local field) instead of base `_attackTimer` |
| 2 | `_sqrAttackRange` private in MonsterBase | Cache `_mySqrAttackRange = _data.AttackRange²` in `Awake()` — same pattern as MonsterB/C |
| 3 | AoeSlamZone pool exhaustion — 3 zones in use when slam triggers again | Pool sized to `_slamZoneCount`; `_isSlamming` gate prevents re-triggering while zones are still spawning |
| 4 | Zone spawns at player position snapshotted at spawn time — if player moves, explosion hits original spot | Intentional: warning circle appears where player was; player must dodge out of the zone |
| 5 | Boss dies mid-slam — orphaned active AoeSlamZones have no parent reference | Zones are self-contained (no BossA reference), they finish their timer and deactivate normally |
| 6 | Transparent material setup in Built-in RP requires several property flags | Use opaque red material (prototype); set `Color.red` on `Standard` shader cylinder |
| 7 | `base.EnterAttack()` sets private `_attackTimer = 0` which BossA doesn't use | Harmless; BossA's `_meleeTimer` is reset separately in the override |
| 8 | `_slamTimer` resets in `EnterAttack()` — boss re-entering attack after brief chase wipes the countdown | Initialize `_slamTimer = _slamInterval` only in `Awake()`, never reset in `EnterAttack()`; timer carries over across state changes |

## HitEffect / EffectManager (2026-03-02)

| # | Problem | Mitigation |
|---|---------|------------|
| 1 | `_Color` missing on default pink/error material — flash invisible | No crash; effect silently skipped. Assign Standard-shader material to prefabs. |
| 2 | Coroutine runs after victim GO deactivated on death | Coroutine lives on EffectManager; renderer ref stays valid; SetPropertyBlock on disabled GO is harmless |
| 3 | `FindObjectOfType<QuarterViewCamera>()` returns null if not in scene | Null-guarded: `_camera?.CameraShake(...)` |
| 4 | Spark pool all-slots-active edge case | Silently skipped; pool size 10 is well above realistic simultaneous hits |
| 5 | `stopAction = Disable` re-entry: re-enable + Play() on a stopped-and-disabled PS | Unity clears particles on disable; Play() restarts fresh |

## BossB (2026-03-02)

| # | Problem | Mitigation |
|---|---------|------------|
| 1 | `_attackTimer` private in MonsterBase — BossB needs independent melee + charge timers | Override `UpdateAttack()` entirely; use own `_meleeTimer` field (same pattern as BossA) |
| 2 | `_sqrAttackRange` private in MonsterBase | Cache `_mySqrAttackRange` in `Awake()` — same pattern as BossA/C |
| 3 | `_data.MaxHP` is `int`; `_currentHP / _data.MaxHP` → integer division always 0 | Cast: `_currentHP / (float)_data.MaxHP` |
| 4 | Vibration displaces `transform.position` each frame — boss drifts from anchor if not snapped back | Store `_windupAnchor` at windup entry; restore exact anchor at windup end before entering Charging |
| 5 | Charge direction locked at windup START — target may die before charge begins | Null/alive check in `EnterChargeWindup()`; if target invalid, cancel (don't set phase, don't charge) |
| 6 | During charge, `UpdateAttack()` skips the melee range check → boss can't EnterChase mid-charge | Intentional: charge runs to completion; range check resumes only after `_chargePhase = None` |
| 7 | After charge ends, boss has moved up to 10u past target — sqrDist > _mySqrAttackRange → EnterChase | Correct/intended: boss chases back; creates dodge-counter-chase loop |
| 8 | `_chargeCooldown` initialized in Awake; if `_data == null` return-early fires, cooldown stays 0 | Guard: `if (_data == null) return;` before cooldown init — same as other monsters |
| 9 | Charge distance check uses sqrMagnitude — must pre-cache `_chargeMaxDistanceSqr` | Compute `_chargeMaxDistance * _chargeMaxDistance` in `Awake()` to avoid per-frame multiply |
