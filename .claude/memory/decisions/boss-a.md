# Decisions — BossA

- BossA.UpdateAttack() fully overridden (not calling base) — base._attackTimer is private; BossA needs independent _meleeTimer and _slamTimer
- _slamTimer and _meleeTimer are independent — both tick during Attack state; slam does not pause melee
- _slamTimer initialized in Awake() to _slamInterval, never reset in EnterAttack() — carries across Chase↔Attack transitions so 8s counts from game start, not from each attack entry
- Zone pool pre-allocated in BossA.Awake() via AddComponent<AoeSlamZone> on new GameObjects — no separate pool singleton; 3 zones is too few to warrant one
- Zone GameObjects are siblings of BossA (SetParent(transform.parent)) not children — zones should not move when boss moves
- AoeSlamZone.Awake() sets gameObject.SetActive(false) — pool starts inactive; GetAvailableZone() detects availability via !activeInHierarchy
- Zone position snapshotted at spawn time (player's current position) — warning circle shows where player was; player must dodge out
- AoeSlamZone has no reference back to BossA — fully decoupled; returns to pool by deactivating itself after Detonate()
- GroundOffset = 0.02f in AoeSlamZone.Init() — raises cylinder center just above Y ground to prevent Z-fighting
- _mySqrAttackRange cached in Awake() — same pattern as MonsterB/C; base._sqrAttackRange is private
