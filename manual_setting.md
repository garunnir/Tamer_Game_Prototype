# Wild Tamer — Manual Editor Setup Guide

---

## Step 3: MapData ScriptableObject

### Create the MapData asset

1. In the **Project** window, right-click the folder `Assets/Data/` (create it if absent).
2. Choose **Create → WildTamer → Map Data**.
3. Name the asset **`MapData`**.
4. Select the asset and configure in the **Inspector**:

| Field | Recommended value | Notes |
|---|---|---|
| World Center | `(0, 0)` | XZ center of your playable area |
| World Size | `(200, 200)` | Total width × depth of the playable area |

> **World Size minimum:** Each axis is clamped internally to ≥ 1 to prevent division-by-zero. Keep values well above 1 in production.

---

### Set up the MinimapController scene object

1. In the **Hierarchy**, create an empty GameObject named **`MinimapController`**.
2. Attach the **`MinimapController`** component (`Scripts/Minimap/MinimapController.cs`).
3. In the Inspector, assign:

| Field | Value |
|---|---|
| Map Data | The `MapData` asset created above |
| Render Texture Size | `256` (power-of-two; increase for sharper minimap) |

4. The component creates its own `RenderTexture` at runtime (`FilterMode.Bilinear`).
   Do **not** assign a RenderTexture manually — it is allocated and released automatically.

---

### Verify coordinate conversion

In Play mode, open the Console and call from any MonoBehaviour:

```csharp
Vector2 uv = MinimapController.Instance.WorldToMinimapUV(transform.position);
Debug.Log($"UV: {uv}");
```

- A unit at `WorldCenter` should return approximately `(0.5, 0.5)`.
- A unit at the far edge should clamp to `0` or `1`.
