# Wild Tamer — Manual Setup Guide

This document records all Inspector / Editor configuration steps that cannot be
automated by code.  Follow each section in order after importing the associated
script or shader.

---

## Step 5 — Minimap UI Shader (MinimapFog)

### What the shader does

`Assets/Shaders/MinimapFog.shader` (`WildTamer/MinimapFog`) is a URP Unlit UI
shader that composites two textures on a Canvas `RawImage`:

| Layer | Source | Effect |
|---|---|---|
| Bottom | `_BackgroundTex` | World/terrain map image |
| Top | `_FogMaskTex` (FoW RT) | Black overlay where mask alpha ≈ 1 |

`smoothstep(_EdgeLow, _EdgeHigh, fogAlpha)` produces a soft gradient at fog
boundaries.  Adjust `_EdgeLow` / `_EdgeHigh` in the Material to taste.

---

### 5-A  Create the Material

1. In the **Project** window, right-click **Assets/Materials** (create the
   folder if absent) → **Create → Material**.
2. Name it `MinimapFogMaterial`.
3. In the Material Inspector, click the **Shader** dropdown and select
   **WildTamer → MinimapFog**.
4. Set initial property values:

   | Property | Recommended value |
   |---|---|
   | Fog Color | `(0, 0, 0, 1)` — opaque black |
   | Edge Smoothstep Low | `0.45` |
   | Edge Smoothstep High | `0.55` |

   Leave `_BackgroundTex` and `_FogMaskTex` empty for now; they are assigned
   via the UI hierarchy (see 5-C).

---

### 5-B  Canvas & Root Panel

1. In the **Hierarchy**, create (or locate) a **Canvas** set to
   **Screen Space – Overlay** (or Camera depending on your HUD setup).
2. Under the Canvas, create an empty **GameObject** named `MinimapRoot`.
   - `RectTransform`: anchor bottom-right, Pos X = −110, Pos Y = 110,
     Width = 200, Height = 200 (adjust to taste).
   - Add a **Canvas Group** if you want to fade the entire minimap.

---

### 5-C  Background RawImage

1. Right-click `MinimapRoot` → **UI → Raw Image**.
2. Name it `MinimapBackground`.
3. `RectTransform`: stretch to fill parent (anchor = stretch/stretch,
   all offsets = 0).
4. **Texture** field: drag in your terrain/world map sprite or Texture2D asset.
   - If the background is static (pre-baked image), assign it directly.
   - If it comes from a separate Camera RenderTexture, assign that RT here.
5. Leave **Material** as **None** (default UI material) — the background is
   just a plain image underneath.

---

### 5-D  Fog Mask RawImage (the shader-driven overlay)

1. Right-click `MinimapRoot` → **UI → Raw Image**.
2. Name it `MinimapFogOverlay`.
3. `RectTransform`: stretch to fill parent (same as background).
4. **Texture** field: assign the Fog-of-War `RenderTexture` produced by
   `FogOfWarPass` / `MinimapController.MinimapRT`.
   - In the Inspector: drag the RT asset from
     `Assets/RenderTextures/FogMask` (or wherever you saved it).
   - Alternatively, assign it at runtime:
     ```csharp
     GetComponent<RawImage>().texture = MinimapController.Instance.MinimapRT;
     ```
5. **Material** field: drag `MinimapFogMaterial` (created in 5-A) into the
   **Material** slot on the `RawImage` component.

   > **Important:** Unity's `RawImage.material` overrides the default UI
   > material.  The shader will read `mainTex` (UV0) from the `RawImage`
   > mesh — this maps to `_FogMaskTex` because the shader treats the
   > Canvas-injected `_MainTex` as the mask input.
   >
   > Assign `_BackgroundTex` explicitly in the Material Inspector (or at
   > runtime via `Material.SetTexture("_BackgroundTex", ...)`) so the shader
   > can sample the map image independently.

6. **Color** field on the `RawImage` component: set to `(255, 255, 255, 255)`
   — full white so vertex colour does not tint the output.

---

### 5-E  Ordering — ensure correct draw order

- `MinimapBackground` must be **above** (earlier sibling) `MinimapFogOverlay`
  in the Hierarchy so it renders first (lower sort order).
- Alternatively, use a single `RawImage` (`MinimapFogOverlay`) with the
  Material and assign the background texture to `_BackgroundTex` — no separate
  background image needed in that case.

---

### 5-F  Runtime texture assignment (optional C# snippet)

If the FoW RenderTexture is created at runtime (e.g., in `MinimapController.Awake`),
add a small initializer component:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// Assigns the runtime FoW RenderTexture to the MinimapFogOverlay RawImage.
    /// Attach to the MinimapFogOverlay GameObject.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MinimapFogUIBinder : MonoBehaviour
    {
        private void Start()
        {
            var rt = MinimapController.Instance?.MinimapRT;
            if (rt == null)
            {
                Debug.LogWarning("[MinimapFogUIBinder] MinimapController not ready.");
                return;
            }

            var img = GetComponent<RawImage>();
            img.texture = rt;

            // Also push into the material's _FogMaskTex slot
            // so the shader property name stays explicit.
            if (img.material != null)
                img.material.SetTexture("_FogMaskTex", rt);
        }
    }
}
```

> Place `MinimapFogUIBinder.cs` in `Assets/Scripts/Minimap/`.

---

### 5-G  Quick smoke-test

1. Enter Play mode.
2. The minimap should initially be **fully black** (fog covers everything).
3. As the player moves, the FoW brush stamps revealed areas and the black
   overlay should dissolve with a soft edge.
4. Adjust `_EdgeLow` / `_EdgeHigh` in `MinimapFogMaterial` at runtime via
   the Inspector to tune the softness of the fog boundary.

---

---

## Step 6 — Minimap Unit Tracking (MinimapUnitTracker)

### What the system does

`MinimapUnitTracker` (singleton, `Assets/Scripts/Minimap/MinimapUnitTracker.cs`) displays
colored dot icons on the minimap for every live unit and the player:

| Icon colour | Condition |
|---|---|
| **Blue** | Player / Ally faction — always visible |
| **Red** | Enemy faction — visible only when the unit's world position falls in a **revealed** area of the Fog of War |

`MonsterUnit.Start()` self-registers; `MonsterUnit.OnDisable()` self-unregisters.
No manual per-unit wiring is needed.

Faction swaps (taming Enemy→Player, releasing Player→Neutral) are detected automatically
each update tick and swap the icon color without extra events.

---

### 6-A  Create the Icon Prefabs

Both prefabs follow the same structure.  Create them once, then duplicate and recolor.

**Step-by-step (Blue Ally dot):**

1. In the **Hierarchy**, right-click anywhere → **UI → Image**.
   Unity creates a Canvas if one doesn't exist — discard it; you only need the result as a
   **Prefab asset**.
2. Rename the Image to `MinimapDot_Ally`.
3. `RectTransform` settings:
   - **Width / Height**: `8` × `8` (pixels at 1:1 canvas scale — adjust to taste)
   - **Pivot**: `0.5, 0.5`
   - **Anchor**: `0.5, 0.5` (these will be overridden by code at runtime, but set them here
     so the prefab is self-consistent)
4. **Image** component:
   - **Source Image**: assign a small circle sprite (Unity's built-in `Knob` from UI Default
     Assets works well, or create a simple round Texture2D).
     Alternatively, leave `None` for a square dot.
   - **Color**: `(0, 120, 255, 255)` — vivid blue.
   - **Raycast Target**: **unchecked** (UI dots must not block minimap clicks).
5. Drag the `MinimapDot_Ally` GameObject from the Hierarchy into
   `Assets/Prefabs/UI/` to create the prefab asset.  Delete the scene instance.

**Red Enemy dot:**

1. Duplicate `MinimapDot_Ally` → rename `MinimapDot_Enemy`.
2. Change **Color** to `(255, 40, 40, 255)` — vivid red.
3. Save as a separate prefab asset.

---

### 6-B  Add MinimapUnitTracker to the Scene

1. Create an empty **GameObject** in the scene — name it `MinimapUnitTracker`.
   - Recommended parent: the same GameObject that holds `MinimapController` or a dedicated
     `Managers` root.
2. Add the **MinimapUnitTracker** component (from `Assets/Scripts/Minimap/`).

---

### 6-C  Assign Inspector Fields

| Field | Value |
|---|---|
| **Minimap Rect** | The `RectTransform` of your minimap panel (e.g., `MinimapRoot` from Step 5) |
| **Player Transform** | Drag the player **GameObject** here (its `Transform` is used directly) |
| **Ally Icon Prefab** | `MinimapDot_Ally` prefab (blue dot) |
| **Enemy Icon Prefab** | `MinimapDot_Enemy` prefab (red dot) |
| **Icon Update Interval** | `0.1` s — how often icon positions refresh (InvokeRepeating) |
| **Fow Sample Interval** | `0.2` s — how often AsyncGPUReadback queries the FoW RT |
| **Reveal Threshold** | `0.05` — R-channel value above which a pixel counts as revealed |

---

### 6-D  UI Layer Order (icon icons draw on top of FoW overlay)

The dot icons must render **above** the `MinimapFogOverlay` RawImage from Step 5.
Ensure the `MinimapRoot` hierarchy is ordered like this (top = rendered last = on top):

```
MinimapRoot  (Panel / RectTransform)
├── MinimapBackground     ← RawImage (world map)
├── MinimapFogOverlay     ← RawImage + MinimapFogMaterial
└── [Dots are spawned here as children by MinimapUnitTracker at runtime]
```

Because `MinimapUnitTracker` parents icons to `_minimapRect` (`MinimapRoot`), they are
added as later siblings and therefore render above the two RawImages automatically.

> **If dots appear below the fog overlay:** Select the newly spawned dot GameObjects at
> runtime and use **Transform → Move to Front** in the context menu, or reorder by script.
> Alternatively set `_minimapRect` to a dedicated `DotsLayer` child panel placed after
> `MinimapFogOverlay` in the hierarchy.

---

### 6-E  FoW Reveal Threshold Tuning

- If enemy icons flicker at fog edges, increase `_revealThreshold` slightly (e.g., `0.1`).
- If enemies show through the fog more than expected, reduce `_revealThreshold` (e.g., `0.02`).
- Reducing `_fowSampleInterval` makes enemy visibility react faster at the cost of more
  GPU-to-CPU readback bandwidth (still async — no GPU stalls).

---

### 6-F  Quick Smoke-Test

1. Enter Play mode.
2. **Blue dot** should appear at the player's position immediately.
3. Move the player — the blue dot moves correspondingly on the minimap.
4. Enemy MonsterUnit instances should spawn **hidden** (red dots not visible).
5. Move the player near an enemy — the FoW brush reveals that area; after up to
   `_fowSampleInterval` (0.2 s) the enemy red dot becomes visible.
6. Tame an enemy — its icon should switch from red to blue within `_iconUpdateInterval`.

---

*Further setup steps (JSON save/load) will be added to this file in subsequent development steps.*
