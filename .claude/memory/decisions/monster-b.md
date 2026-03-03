# Decisions — MonsterB

- _sqrAttackRange is private in MonsterBase — MonsterB caches its own _mySqrAttackRange in Awake() using _data.AttackRange; no MonsterBase change needed
- Retreat uses dynamic retreatPoint = transform.position - toTarget.normalized * (retreatDistance * 2) — recalculated each frame so monster always flees directly away even if target moves
- Zigzag anchor is _currentTarget.Transform.position (not monster position) — offset oscillates around target so approach is net-convergent; monster always closes distance on average
- PerformAttack() calls EnterChase() mid-UpdateAttack() — harmless: _attackTimer assignment after return is irrelevant because EnterAttack() resets timer to 0 on next entry
- _zigzagTimer reset in EnterChase() override — clean wave start each time approach begins, avoids mid-cycle continuation artifacts
- MonsterB inspector: _data → MonsterB.asset; moveSpeed=7, attackRange=4, detectionRange=10; _zigzagAmplitude must remain < attackRange (default 3 < 4)
