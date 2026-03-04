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
        /// Converts a world position (XZ plane) to a minimap UV in [0, 1] × [0, 1].
        /// The rectangle is defined by MapData.WorldCenter ± WorldSize/2.
        /// </summary>
        public Vector2 WorldToMinimapUV(Vector3 worldPos)
        {
            Vector2 center = _mapData.WorldCenter;
            Vector2 size   = _mapData.SafeWorldSize;

            float u = Mathf.Clamp01((worldPos.x - center.x) / size.x + 0.5f);
            float v = Mathf.Clamp01((worldPos.z - center.y) / size.y + 0.5f);

            return new Vector2(u, v);
        }
    }
}
