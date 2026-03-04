# Manual Setting — MapData Local View

## 1. Create the MapData ScriptableObject asset

If a MapData asset does not yet exist in the project:

1. In the **Project** window, navigate to `Assets/ScriptableObjects/` (create the folder if absent).
2. Right-click → **Create → WildTamer → Map Data**.
3. Name the asset `MapData`.

---

## 2. Assign MapData to the scene

| Scene object | Component | Field | Value |
|---|---|---|---|
| `MinimapController` (GameObject) | `MinimapController` | `Map Data` | drag `MapData` asset |

---

## 3. Inspector values for the new Local View fields

Open the `MapData` asset and set the values under the **Local View** header:

| Field | Type | Recommended value | Notes |
|---|---|---|---|
| `Use Local View` | bool | `false` (Global) / `true` (Local) | Toggle to switch modes at design time. Can also be toggled at runtime via `_useLocalView`. |
| `View Radius` | float | `30` | World-space units. The minimap will show a square of side `2 × viewRadius` centred on the player. Increase for a wider view, decrease for a tighter zoom. Minimum enforced: `1`. |

### Existing fields — do NOT change these for Local View

| Field | Notes |
|---|---|
| `World Center` | Still used in Global mode. Unchanged. |
| `World Size` | Still used in Global mode. Unchanged. |

---

## 4. Behaviour reference

| `Use Local View` | What the minimap shows |
|---|---|
| `false` (Global) | Full world map bounded by `WorldCenter ± WorldSize/2`. |
| `true`  (Local)  | Clamped View Window centred on the player (see §4a). |

### 4a. Clamped View Window (Local mode)

Each frame `MinimapUnitTracker` calls `MinimapController.SetViewCenter(playerPosition)`.
`SetViewCenter` computes the View Window per axis:

1. **Ideal window**: `[playerPos - viewRadius, playerPos + viewRadius]`
2. **Clamp center** so the window never exits world bounds:
   `clampedCenter = Clamp(playerPos, worldMin + r, worldMax - r)`
3. **Edge case** – if `viewRadius ≥ worldHalfExtent` on an axis, the window simply shows the whole world on that axis (same as Global mode for that axis).

Result: UV `(0,0)` → window bottom-left; UV `(1,1)` → window top-right.
The window is always fully inside the world — UV edges always show real map content.

The FoW stamping (`FowController`) and unit tracker (`MinimapUnitTracker`) automatically use
`MinimapController.WorldToMinimapUV`, so both systems respond to the mode toggle without
any additional setup.

---

## 5. Shader material — property mapping and application

### 5a. Create the MinimapFog material

1. In the **Project** window, navigate to `Assets/Materials/` (create if absent).
2. Right-click → **Create → Material**.
3. Name it `MinimapFogMat`.
4. In the material Inspector, set the **Shader** to `WildTamer/MinimapFog`.

### 5b. Assign textures in the material Inspector

| Material property | What to assign | Notes |
|---|---|---|
| `Background Map` | Your world map image (e.g. `MinimapBackground.png`) | Static asset; set once. Texture **Import Settings**: Wrap Mode = **Clamp**, Filter Mode = Bilinear. |
| `Fog Mask RT` | Leave empty | `MinimapFogUIBinder` sets this at runtime via `_FogMaskTex`. |

### 5c. Assign the material to the UI element

1. Select the **MinimapFogOverlay** GameObject (the `RawImage` that shows the minimap overlay).
2. In the **RawImage** component, set **Material** to `MinimapFogMat`.

### 5d. Add MinimapScrollController to the scene

Add the `MinimapScrollController` component to the **same** GameObject as the minimap RawImage.

| Inspector field | Value |
|---|---|
| `Player Transform` | Drag the Player GameObject (the one `MinimapUnitTracker._playerTransform` also references). |
| `Map Data` | Drag the same `MapData` asset used by `MinimapController`. |

`MinimapScrollController` clones the material in `Awake()`, so `MinimapFogUIBinder.Start()` correctly writes `_FogMaskTex` onto the per-instance copy — no manual ordering required.

### 5e. Script-driven properties (do NOT set manually)

These four properties are overwritten every frame by `MinimapScrollController.LateUpdate()`.
Setting them by hand in the material Inspector has no lasting effect at runtime.

| Shader property | Type | Script source | Encoding |
|---|---|---|---|
| `_PlayerPos` | Vector (xy used) | `_playerTransform.position` | Global-map UV: `(pos - worldCenter) / worldSize + 0.5` |
| `_ViewRadius` | Float | `MapData.ViewRadius` | World units, min 1 |
| `_WorldSize` | Vector (xy used) | `MapData.SafeWorldSize` | World units |
| `_IsLocalView` | Float (0 or 1) | `MapData.UseLocalView` | 0 = global, 1 = local |

### 5f. Clamp wrap mode — why saturate() is used

The shader uses `saturate(bgUV)` rather than relying on the texture sampler's wrap mode.
This means the texture's **Wrap Mode** setting is irrelevant to tiling artifacts — the HLSL
`saturate` call hard-clamps the UV before the sample, so edge pixels repeat cleanly without
tiling. Set the background texture's Wrap Mode to **Clamp** in Import Settings anyway as a
belt-and-suspenders measure.

---

## 6. System sync — coordinate spaces (read before changing anything)

This section explains the single coordinate system that ties all minimap subsystems together
after Task 4. Every subsystem uses a specific UV space; mixing them causes visual misalignment.

### 6a. Coordinate space map

| Subsystem | Method / API | UV space | Notes |
|---|---|---|---|
| **Background texture** (`_BackgroundTex`) | `bgUV` computed in shader | **Global** | Remapped by clamped-window math when `_IsLocalView = 1`. |
| **FoW stamp** (`FowController`) | `WorldToGlobalUV()` | **Global** | Always global — the FoW RT is a persistent world map. |
| **FoW mask display** (`_FogMaskTex`) | Sampled at `bgUV` in shader | **Global** | Must match background — changed from `IN.uv` in Task 4. |
| **Icon placement** (`MinimapUnitTracker`) | `WorldToMinimapUV()` | **Local / Global** | Returns local UV when `UseLocalView = true`, global otherwise. |
| **Enemy FoW visibility** (`IsRevealed`) | `WorldToGlobalUV()` | **Global** | FoW cache is in global space; must use matching UV. |
| **View-window gate** (`IsInViewWindow`) | World-space comparison | **World** | Fires before any UV lookup; returns `true` in Global mode. |

### 6b. Icon prefab requirement

Icon prefabs used by `MinimapUnitTracker` **must have an `Image` component** on the root
`RectTransform`. The tracker caches this `Image` in `TrackedUnit.IconImage` and uses
`Image.enabled` for per-tick visibility, **not** `SetActive`, to avoid Canvas layout rebuilds.

| Behavior | Implementation | Reason |
|---|---|---|
| Per-tick hide/show (in-view, FoW, dead) | `Image.enabled = false/true` | No Canvas rebuild |
| First activation out of pool | `SetActive(true)` once per unit | GO must be active for Image to render |
| Return to pool / spawn | `SetActive(false)` | Icon is fully inactive while pooled |

If an icon prefab lacks an `Image` component, `IconImage` will be `null` and the icon will
remain permanently hidden (silently ignored, no error thrown). Always verify the prefab.

### 6c. FoW RT coordinate space — why it must stay global

The FoW RenderTexture accumulates explored areas **additively and persistently**. If stamps
were placed in local-view UV space, the same world position would map to a different RT pixel
as the player moves, breaking the persistence. `WorldToGlobalUV` produces the fixed mapping
(same formula as `WorldToMinimapUV` in Global mode) that keeps the RT correct.

**Do not change `FowController.StampAtCamera()` back to `WorldToMinimapUV`.**

---

## 7. Tuning guide

- **Too zoomed in / out in Local mode?** Adjust `View Radius` on `MapData`.
  - Small map area (≤ 50 units across) → try `View Radius = 15–25`.
  - Large open world (≥ 500 units) → try `View Radius = 50–100`.
- **FoW brush looks too large/small in Local mode?**
  The `Brush Radius UV` on `FowController` is relative to the RT, so it automatically
  scales with Local mode. Lower it (e.g. `0.08`) for a tighter reveal circle in Local mode.
