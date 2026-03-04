using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Owns the minimap RenderTexture and the world-to-UV coordinate conversion.
    /// Other minimap systems (FoW pass, unit tracker) reference this singleton.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static MinimapController Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [SerializeField] private MapData _mapData;
        [SerializeField] private int     _renderTextureSize = 256;

        // ── Public ───────────────────────────────────────────────────────────

        public RenderTexture MinimapRT { get; private set; }

        // ── Runtime state ────────────────────────────────────────────────────

        // View Window computed in SetViewCenter(); used only in Local view mode.
        // _viewWindowMin  : bottom-left corner of the window in world-XZ space.
        // _viewWindowSize : width / height of the window in world-XZ space.
        private Vector2 _viewWindowMin;
        private Vector2 _viewWindowSize;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_mapData == null)
            {
                Debug.LogError("[MinimapController] MapData is not assigned.");
                return;
            }

            int size = Mathf.Max(_renderTextureSize, 1);
            MinimapRT            = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            MinimapRT.filterMode = FilterMode.Bilinear;
            MinimapRT.Create();
        }

        private void OnDestroy()
        {
            if (MinimapRT != null)
            {
                MinimapRT.Release();
                Destroy(MinimapRT);
            }
        }

        // ── Coordinate conversion ────────────────────────────────────────────

        /// <summary>
        /// Computes and caches the Local View Window from the player's world position.
        /// The window is centred on <paramref name="worldPos"/> ± <c>ViewRadius</c>,
        /// then clamped so it never extends beyond the world bounds defined in MapData.
        ///
        /// Call once per update tick before any <see cref="WorldToMinimapUV"/> queries.
        /// Has no effect in Global mode.
        /// </summary>
        public void SetViewCenter(Vector3 worldPos)
        {
            if (!_mapData.UseLocalView) return;

            Vector2 worldCenter = _mapData.WorldCenter;
            Vector2 worldHalf   = _mapData.SafeWorldSize * 0.5f;   // (halfW, halfH)
            float   r           = _mapData.ViewRadius;

            // ── X axis ───────────────────────────────────────────────────────
            float winMinX, winSizeX;
            if (r >= worldHalf.x)
            {
                // Radius covers the whole world on this axis — show everything.
                winMinX  = worldCenter.x - worldHalf.x;
                winSizeX = worldHalf.x * 2f;
            }
            else
            {
                // Slide window center so [center-r, center+r] stays inside world bounds.
                float cx = Mathf.Clamp(worldPos.x,
                    worldCenter.x - worldHalf.x + r,
                    worldCenter.x + worldHalf.x - r);
                winMinX  = cx - r;
                winSizeX = r * 2f;
            }

            // ── Z axis (stored in worldHalf.y / worldCenter.y) ───────────────
            float winMinZ, winSizeZ;
            if (r >= worldHalf.y)
            {
                winMinZ  = worldCenter.y - worldHalf.y;
                winSizeZ = worldHalf.y * 2f;
            }
            else
            {
                float cz = Mathf.Clamp(worldPos.z,
                    worldCenter.y - worldHalf.y + r,
                    worldCenter.y + worldHalf.y - r);
                winMinZ  = cz - r;
                winSizeZ = r * 2f;
            }

            _viewWindowMin  = new Vector2(winMinX, winMinZ);
            _viewWindowSize = new Vector2(winSizeX, winSizeZ);
        }

        /// <summary>
        /// Converts a world position (XZ plane) to a minimap UV in [0, 1] × [0, 1].
        ///
        /// Global mode: delegates to <see cref="WorldToGlobalUV"/> (full world, unchanged).
        /// Local mode:  maps against the clamped View Window computed in
        ///              <see cref="SetViewCenter"/>. UV 0/1 = window edges (never empty space).
        /// </summary>
        public Vector2 WorldToMinimapUV(Vector3 worldPos)
        {
            if (_mapData.UseLocalView)
            {
                float u = Mathf.Clamp01((worldPos.x - _viewWindowMin.x) / _viewWindowSize.x);
                float v = Mathf.Clamp01((worldPos.z - _viewWindowMin.y) / _viewWindowSize.y);
                return new Vector2(u, v);
            }

            return WorldToGlobalUV(worldPos);
        }

        /// <summary>
        /// Always returns the full-world global UV regardless of <c>UseLocalView</c>.
        /// Used by <see cref="FowController"/> so the FoW RT is stamped in a stable,
        /// persistent coordinate space, and by unit visibility checks against that RT.
        /// </summary>
        public Vector2 WorldToGlobalUV(Vector3 worldPos)
        {
            Vector2 center = _mapData.WorldCenter;
            Vector2 size   = _mapData.SafeWorldSize;

            float u = Mathf.Clamp01((worldPos.x - center.x) / size.x + 0.5f);
            float v = Mathf.Clamp01((worldPos.z - center.y) / size.y + 0.5f);

            return new Vector2(u, v);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="worldPos"/> lies inside the current
        /// clamped View Window (Local mode), or always <c>true</c> in Global mode.
        /// Call after <see cref="SetViewCenter"/> to get the correct result for the frame.
        /// </summary>
        public bool IsInViewWindow(Vector3 worldPos)
        {
            if (!_mapData.UseLocalView) return true;

            return worldPos.x >= _viewWindowMin.x
                && worldPos.x <= _viewWindowMin.x + _viewWindowSize.x
                && worldPos.z >= _viewWindowMin.y
                && worldPos.z <= _viewWindowMin.y + _viewWindowSize.y;
        }
    }
}
