# Progress

## MapData Local View (2026-03-04)
- Added `UseLocalView` (bool) and `ViewRadius` (float, default 30, Min 1) to `MapData.cs`.
- `_worldCenter` / `_worldSize` / `SafeWorldSize` are untouched.
- `MinimapController.WorldToMinimapUV` branches on `UseLocalView`; global path unchanged.
- `MinimapController.SetViewCenter(Vector3)` caches player XZ for local mode.
- `MinimapUnitTracker.UpdateIconPositions` calls `SetViewCenter` once per tick.
- Manual setup instructions → `.claude/memory/manual_setting.md`.
