# Next Session

## Last Session
- HitEffect system (EffectManager, IHitReactive) (2026-03-02)

## Up Next
- TamingSystem.cs — subscribe to MonsterBase.OnMonsterDied, roll TamingChance, call FlockManager.Instance.AddUnit
- FogOfWar.cs
- MinimapController.cs

## Completed Work Summary
- QuarterViewCamera.cs: fixed quarter-view follow camera with smooth lerp and CameraShake() hook
- PlayerController.cs: Rigidbody-based WASD movement with camera-relative direction and Animator Speed hook
- FlockUnit.cs: separation/alignment/cohesion/target-seek; VelocityDirection property for alignment reads
- FlockManager.cs: singleton; sqrMagnitude neighbor detection; AddUnit()/RemoveUnit(); delegates to FormationHelper
- FormationHelper.cs: pure C# class; Recalculate(count); GetOffset(index) safe-returns zero if out of range
- MonsterData.cs: ScriptableObject; 8 SerializeField fields + read-only properties; asset name = type ID
- MonsterDataGenerator.cs: Editor-only MenuItem; creates 5 .asset files; skips existing
- ICombatant.cs: interface; CombatTeam {Ally,Enemy}; TakeDamage/OnTargetAssigned
- CombatSystem.cs: singleton; ScanTargets() via InvokeRepeating(0.2s)
- Projectile.cs: pool-managed; Init(damage,target); Update-loop movement + sqrMagnitude hit + lifetime timeout
- ProjectilePool.cs: singleton; Prewarm N; Get/Return; CreateFallbackPrefab()
- FlockUnitCombat.cs: companion component; ICombatant Ally; Die() calls FlockManager.RemoveUnit
- MonsterBase.cs: abstract; ICombatant Enemy; MonsterState enum; all state methods virtual; OnMonsterDied static event; PerformAttack() virtual hook
- MonsterA.cs: melee; only overrides PerformAttack() — direct TakeDamage call
- MonsterB.cs: ranged; zigzag approach + retreat after shot; overrides Awake/EnterChase/UpdateChase/PerformAttack
- MonsterC.cs: AoE artillery; slow approach (speed×2 enrage <30% HP); stops at 8u; ground explosion via CombatSystem.DealAoeDamage; overrides Awake/UpdateChase/PerformAttack
- CombatSystem.cs: added DealAoeDamage(center,radius,damage,targetTeam) — iterates snapshot to prevent list-mutation on death
- AoeSlamZone.cs: Scripts/Combat/; pooled; Init(pos,radius,damage,warningDuration); Update countdown→Detonate; Detonate calls DealAoeDamage(Ally) then SetActive(false); cylinder primitive child (red, GroundOffset=0.02 Y lift)
- BossA.cs: Scripts/Monster/; overrides Awake/EnterAttack/UpdateAttack/PerformAttack; dual timers in UpdateAttack (_meleeTimer + _slamTimer independent); slam spawns 3 zones via pre-allocated pool (size=_slamZoneCount); pool allocated in Awake with AddComponent&lt;AoeSlamZone&gt;
- BossB.cs: Scripts/Monster/; overrides Awake/EnterAttack/UpdateAttack/PerformAttack; private ChargePhase enum {None,Windup,Charging,Recovery}; charge triggers below 70% HP every 6s; windup saves _windupAnchor + locks _chargeDirection, vibrates transform; charging moves along locked dir at 15u/s, stops at 2s/10u, deals _chargeDamage once on contact; recovery resets cooldown; stale-phase bug fixed: clear _chargePhase+restore anchor on target-lost
