# Decisions — MonsterC

- AoE attack dispatched via CombatSystem.DealAoeDamage() — CombatSystem owns both registries so it is the correct place for multi-target damage; no new pool or component needed
- DealAoeDamage iterates a `new List<ICombatant>(registry)` snapshot — TakeDamage can kill a unit mid-loop causing OnDisable → UnregisterCombatant → List.Remove → InvalidOperationException; snapshot decouples iteration from mutation
- TakeDamage() is not virtual in MonsterBase — enrage speed computed dynamically in UpdateChase() each frame: `_currentHP / (float)_data.MaxHP < _enrageHpThreshold`; no MonsterBase change needed
- _data.MaxHP is int; must cast to float before division to prevent integer truncation to 0
- "Stops at fixed distance" is the natural result of the base UpdateChase → EnterAttack transition at attackRange=8f combined with UpdateAttack never calling MoveToward; no extra override needed
- _sqrAttackRange private in MonsterBase — cached locally as _mySqrAttackRange in Awake(); same pattern as MonsterB
- MonsterC inspector: _data → MonsterC.asset; moveSpeed=2, attackRange=8, detectionRange=12, attackCooldown=3
- _explosionRadius (default 3f) is separate from attackRange (8f) — monster stands at 8u but the blast only reaches 3u around target; _explosionRadius is Inspector-tunable
