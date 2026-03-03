# Decisions — MonsterA

- PerformAttack() virtual hook added to MonsterBase between UpdateAttack() and FireProjectile() — melee subclasses override it; ranged subclasses inherit default (FireProjectile()); cooldown/range logic stays in one place
- MonsterA only overrides PerformAttack() — all state transitions, movement, detection, death are inherited
- MonsterA inspector: _data → MonsterA.asset; moveSpeed=4, attackRange=1.5, detectionRange=8 (set manually after generating assets)
