# Architecture Decisions

## Camera
- `_basePosition` = unshaken pos (no Lerp drift). Shake: Update timer, linear decay; offset via `transform.right/up`; `Mathf.Max` so hits don't reset. `LateUpdate`.

## Player
- `Rigidbody.linearVelocity` (Unity 6); Y preserved. `FreezeRotation` in Awake; rotate via `RotateTowards`. Hash: `static readonly StringToHash`.

## Effects
- Singleton; `TriggerHitEffect` from TakeDamage, `TriggerHitstop` from melee PerformAttack. `IHitReactive` in Combat; MonsterUnit/FlockUnit. Spark: stopAction=Disable. Flash: MaterialPropertyBlock + coroutine on EffectManager GO.

## Combat interfaces (Faction)
- **IHasFaction**: `Faction { get; }` only. **ITargetable** = IHasFaction + Transform, IsAlive. **ITeamable** = IHasFaction + SetFaction. Use by role: read-only → IHasFaction; target → ITargetable; change faction → ITeamable.

## Strategy
- **FactionSystem**: static; use `AreHostile(a,b)`, never compare teamID.
- **SO clone**: `Instantiate(source)` in Awake for Movement/AttackLogic.
- **Follow**: FlockManager drives via FlockMoveLogic.TickWithNeighbors; MonsterUnit.IsFollowing; no FlockMoveLogic ref in MonsterUnit.
- OnTargetAssigned: Follow→Chase on target; Chase/Attack→EnterIdleOrFollow on loss. `AttackLogic.Tick(owner,target,inRange)`; inRange from UpdateAttack. `OnEnterAttackState` must reset sequence (_isSlamming, _chargePhase, windup).
- **Taming**: SetFaction(Player) → Unregister → _factionId → Register → FlockManager.AddUnit → EnterFollow.

### Strategy Map
| Movement | Used By |
| DirectChaseMoveLogic | MonsterA, BossA, BossB |
| ZigzagRetreatMoveLogic | MonsterB |
| EnrageChaseMoveLogic | MonsterC |
| FlockMoveLogic | Tamed |

| Attack | Used By | Notes |
| MeleeAttackLogic | MonsterA | |
| ProjectileAttackLogic | MonsterB, Tamed | retreat for B; ignoreRange for tamed |
| AoeExplosionAttackLogic | MonsterC | |
| MeleeAndSlamAttackLogic | BossA | Zone pool in Initialize() |
| MeleeAndChargeAttackLogic | BossB | ApplyDirectVelocity() |

## Flock Initial
- `_initialUnits` = start as full Player flock. FlockManager `[DefaultExecutionOrder(1)]`; Init in Start(). Start() calls SetFaction(Player) per unit. **Constraint**: inspector factionId = Enemy (SetFaction early-returns if already Player).
