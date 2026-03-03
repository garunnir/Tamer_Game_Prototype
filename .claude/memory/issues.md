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
- TamingSystem
- FogOfWar
- MinimapController
