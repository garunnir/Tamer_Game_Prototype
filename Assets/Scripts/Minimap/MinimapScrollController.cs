using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// Pushes the four local-view scroll properties (_PlayerPos, _ViewRadius,
    /// _WorldSize, _IsLocalView) to the WildTamer/MinimapFog material every frame.
    ///
    /// Attach to the same GameObject as the minimap RawImage that uses the
    /// MinimapFog shader (e.g. "MinimapFogOverlay").
    ///
    /// The material is cloned in Awake() so that MinimapFogUIBinder.Start()
    /// (which sets _FogMaskTex) operates on the per-instance copy.
    ///
    /// _PlayerPos encoding: global-map UV — same formula as WorldToMinimapUV
    /// global path:  u = (worldPos.x - worldCenter.x) / worldSize.x + 0.5
    ///               v = (worldPos.z - worldCenter.y) / worldSize.y + 0.5
    /// This matches the shader's clamped-window math exactly.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MinimapScrollController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Tooltip("The player's Transform. Used to compute the normalised map position.")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("MapData ScriptableObject — provides WorldCenter, SafeWorldSize, "
               + "ViewRadius, and UseLocalView.")]
        [SerializeField] private MapData _mapData;

        // ── Runtime state ────────────────────────────────────────────────────

        private RawImage _rawImage;
        private Material _material;

        // Cached Shader property IDs (avoids string lookup every frame).
        private static readonly int PropPlayerPos   = Shader.PropertyToID("_PlayerPos");
        private static readonly int PropViewRadius  = Shader.PropertyToID("_ViewRadius");
        private static readonly int PropWorldSize   = Shader.PropertyToID("_WorldSize");
        private static readonly int PropIsLocalView = Shader.PropertyToID("_IsLocalView");

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            // Clone the shared material asset into a per-instance copy.
            // Done in Awake (before any Start) so MinimapFogUIBinder.Start()
            // writes _FogMaskTex onto this instance, not the shared asset.
            if (_rawImage.material != null)
            {
                _material          = Instantiate(_rawImage.material);
                _rawImage.material = _material;
            }
        }

        private void Start()
        {
            if (_mapData == null)
            {
                Debug.LogError("[MinimapScrollController] MapData is not assigned.");
                return;
            }

            // Push initial values so the shader is correct on frame 0.
            UpdateMaterial();
        }

        private void LateUpdate()
        {
            if (_material == null || _mapData == null) return;
            UpdateMaterial();
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

        // ── Material update ──────────────────────────────────────────────────

        private void UpdateMaterial()
        {
            Vector2 worldSize = _mapData.SafeWorldSize;

            _material.SetVector(PropWorldSize,   new Vector4(worldSize.x, worldSize.y, 0f, 0f));
            _material.SetFloat (PropViewRadius,  _mapData.ViewRadius);
            _material.SetFloat (PropIsLocalView, _mapData.UseLocalView ? 1f : 0f);

            if (_playerTransform != null)
            {
                // Normalise player world position to [0,1] global-map UV space.
                // Mirrors WorldToMinimapUV global path in MinimapController.
                Vector2 wc  = _mapData.WorldCenter;
                Vector3 pos = _playerTransform.position;

                float u = Mathf.Clamp01((pos.x - wc.x) / worldSize.x + 0.5f);
                float v = Mathf.Clamp01((pos.z - wc.y) / worldSize.y + 0.5f);

                _material.SetVector(PropPlayerPos, new Vector4(u, v, 0f, 0f));
            }
        }
    }
}
