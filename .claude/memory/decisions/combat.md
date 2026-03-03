# Decisions — Combat System

- CombatSystem.ScanTargets() via InvokeRepeating(0.2s) — never per frame
- GetNearestInRange seeds nearestSqrDist = source.DetectionRange² — no separate range filter step needed
- Combatants register in Start() not Awake() — CombatSystem singleton Instance guaranteed to exist first
- Dead combatants remain in list until OnDisable() fires — IsAlive check prevents targeting dead units
- Unregistration on death: EnterDead → OnDeath → SetActive(false) → OnDisable → UnregisterCombatant
- MonsterBase._sqrAttackRange cached in Awake — avoids per-frame multiply in hot loop
- MonsterBase.EnterAttack sets _attackTimer=0 — monster fires immediately on entering range (responsive feel)
- FlockUnitCombat SRP: FlockUnit owns movement, FlockUnitCombat owns HP/attack — separate components
- ProjectilePool.CreateFallbackPrefab adds Projectile component to primitive — no Inspector dependency needed
- MonsterBase.OnMonsterDied is static event — TamingSystem subscribes once, not per-monster
- MonsterBase patrol uses Random.insideUnitCircle (flat XZ) — monsters stay grounded without physics
- MoveToward ignores Y delta and normalises direction — monsters stay grounded
