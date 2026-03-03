# Known Issues & Incomplete

## Inspector Setup Required (Manual)
- QuarterViewCamera._player must be assigned in Inspector
- PlayerController._cameraTransform must be assigned in Inspector
- FlockManager._playerTransform must be assigned in Inspector
- CombatSystem + ProjectilePool must each be on a GameObject in the scene

## Data
- Existing MonsterData .asset files do not have _attackCooldown set
  → Re-run WildTamer → Generate Monster Data Assets (or set via Inspector)

## Inspector Setup Required — EffectManager
- Add EffectManager component to a scene GameObject
- Assign _camera (QuarterViewCamera) — or leave null, auto-found via FindObjectOfType
- _sparkPrefab is optional — leave null for procedural default spark

## Not Yet Implemented
- MonsterC — implemented; MonsterC.asset values (moveSpeed=2, attackRange=8, attackCooldown=3) must be set manually in Inspector
- MonsterA — implemented; MonsterA.asset values (moveSpeed=4, attackRange=1.5) must be set manually in Inspector
- BossB — implemented; BossB.asset values (attackCooldown=2, attackRange=2, moveSpeed=3, maxHP=350, detectionRange=15) must be set in Inspector
- TamingSystem
- FogOfWar
- MinimapController
