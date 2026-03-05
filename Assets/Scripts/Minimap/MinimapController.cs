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

        [Header("Isometric Alignment")]
        [Tooltip("Rotation applied to all UV coordinates to align the camera's forward direction "
               + "with minimap-up. Matches the camera's Y rotation offset (negative sign applied "
               + "internally). Flip sign here if icon movement direction is inverted.")]
        [SerializeField] private float _isometricRotationOffset = 45f;

        [Tooltip("When true, the minimap rotates with the player so their facing direction is "
               + "always minimap-up. Only affects icon placement (WorldToMinimapUV); the FoW RT "
               + "always uses a fixed coordinate space (WorldToGlobalUV) for persistence.")]
        [SerializeField] private bool _rotatingMap = false;

        // ── Public ───────────────────────────────────────────────────────────

        public RenderTexture MinimapRT { get; private set; }

        // ── Runtime state ────────────────────────────────────────────────────

        // View Window computed in SetViewCenter(); used only in Local view mode.
        // _viewWindowMin  : bottom-left corner of the window in world-XZ space.
        // _viewWindowSize : width / height of the window in world-XZ space.
        private Vector2 _viewWindowMin;
        private Vector2 _viewWindowSize;

        // Player yaw in degrees. Updated each tick via SetPlayerYaw().
        // Only applied in WorldToMinimapUV (Rotating mode). WorldToGlobalUV ignores it
        // so the FoW RT remains in a stable, player-rotation-independent UV space.
        private float _playerYaw;

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

        // ── Player yaw (Rotating mode) ───────────────────────────────────────

        /// <summary>
        /// Updates the player's world-space Y rotation used by <see cref="WorldToMinimapUV"/>
        /// in Rotating mode. Has no effect in Fixed mode or when used by
        /// <see cref="WorldToGlobalUV"/> (FoW coordinate space is always fixed).
        /// Call once per update tick from <see cref="MinimapUnitTracker"/>.
        /// </summary>
        public void SetPlayerYaw(float yawDegrees)
        {
            _playerYaw = _rotatingMap ? yawDegrees : 0f;
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
        /// Converts a world position (XZ plane) to a minimap UV in [0, 1] × [0, 1],
        /// applying the isometric coordinate transform (rotation + V-compression).
        ///
        /// Global mode: delegates to the isometric global UV path.
        /// Local mode:  maps against the clamped View Window computed in
        ///              <see cref="SetViewCenter"/>. In Rotating mode, also applies the
        ///              current player yaw so the player always faces minimap-up.
        /// </summary>
        public Vector2 WorldToMinimapUV(Vector3 worldPos)
        {
            if (_mapData.UseLocalView)
            {
                // Use the clamped window center as the reference origin.
                Vector2 windowCenter = _viewWindowMin + _viewWindowSize * 0.5f;

                // Normalize offset to [-0.5, 0.5] relative to the window size.
                float nx = (worldPos.x - windowCenter.x) / _viewWindowSize.x;
                float nz = (worldPos.z - windowCenter.y) / _viewWindowSize.y;

                Vector2 iso = ApplyIsometricTransform(nx, nz, _playerYaw);
                return new Vector2(Mathf.Clamp01(iso.x + 0.5f), Mathf.Clamp01(iso.y + 0.5f));
            }

            // Global view also includes player yaw in Rotating mode.
            Vector2 center = _mapData.WorldCenter;
            Vector2 size   = _mapData.SafeWorldSize;
            {
                float nx = (worldPos.x - center.x) / size.x;
                float nz = (worldPos.z - center.y) / size.y;
                Vector2 iso = ApplyIsometricTransform(nx, nz, _playerYaw);
                return new Vector2(Mathf.Clamp01(iso.x + 0.5f), Mathf.Clamp01(iso.y + 0.5f));
            }
        }

        /// <summary>
        /// Returns the full-world global UV with a fixed isometric offset only —
        /// player yaw is intentionally excluded so this coordinate space is stable
        /// across player rotations.
        ///
        /// Used by <see cref="FowController"/> (FoW stamp positions must persist across
        /// sessions and player turns) and by <see cref="MinimapUnitTracker"/> for FoW
        /// visibility sampling.
        /// </summary>
        public Vector2 WorldToGlobalUV(Vector3 worldPos)
        {
            Vector2 center = _mapData.WorldCenter;
            Vector2 size   = _mapData.SafeWorldSize;

            float nx = (worldPos.x - center.x) / size.x;
            float nz = (worldPos.z - center.y) / size.y;

            // extraYaw = 0 → FoW space is always aligned to the fixed isometric offset.
            Vector2 iso = ApplyIsometricTransform(nx, nz, 0f);
            return new Vector2(Mathf.Clamp01(iso.x + 0.5f), Mathf.Clamp01(iso.y + 0.5f));
        }

        /// <summary>
        /// Applies the isometric 2D rotation and vertical compression to a normalised
        /// world-XZ offset (both axes in the [-0.5, 0.5] range).
        ///
        /// Rotation angle = -(isometricRotationOffset + extraYawDeg).
        ///   • Fixed mode:    extraYawDeg = 0  → aligns camera-forward with minimap-up.
        ///   • Rotating mode: extraYawDeg = playerYaw → player always faces minimap-up.
        ///
        /// The V component is multiplied by 0.5 afterwards to simulate the isometric
        /// floor's diamond-shaped projection (vertical foreshortening). The result is
        /// centered around (0, 0); add 0.5 to convert to [0, 1] UV space.
        ///
        /// Note on sign: a negative angle rotates the coordinate frame clockwise, which
        /// visually rotates the rendered map counter-clockwise. If icon movement appears
        /// mirrored, negate <c>_isometricRotationOffset</c> in the Inspector.
        /// </summary>
        private Vector2 ApplyIsometricTransform(float nx, float nz, float extraYawDeg)
        {
            float rad = -(_isometricRotationOffset + extraYawDeg) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            float rotU = nx * cos - nz * sin;
            float rotV = nx * sin + nz * cos;

            // Isometric vertical compression: the floor appears foreshortened in the
            // Y direction when viewed at an isometric angle.
            rotV *= 0.5f;

            return new Vector2(rotU, rotV);
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
