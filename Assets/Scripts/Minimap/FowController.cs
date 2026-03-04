using UnityEngine;
using UnityEngine.Rendering;

namespace WildTamer
{
    /// <summary>
    /// Owns the Fog-of-War RenderTexture and stamps a soft brush onto it whenever
    /// the camera moves more than <see cref="_movementThreshold"/> units.
    ///
    /// Stamping uses a CommandBuffer + Graphics.ExecuteCommandBuffer — no direct
    /// RenderTexture.active manipulation, Unity 6 URP compliant.
    ///
    /// RT convention: 0 = fogged, 1 = revealed (additive accumulation).
    /// </summary>
    public class FowController : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static FowController Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [SerializeField] private Camera _camera;

        [SerializeField]
        private Shader _stampShader;

        [SerializeField, Range(0.01f, 0.5f)]
        private float _brushRadiusUV = 0.15f;

        [SerializeField]
        private float _movementThreshold = 0.5f;

        [SerializeField]
        private int _fowTextureSize = 256;

        // ── Public ───────────────────────────────────────────────────────────

        public RenderTexture FowRT { get; private set; }

        // ── Runtime state ────────────────────────────────────────────────────

        private Material      _stampMaterial;
        private Texture2D     _brushTexture;
        private CommandBuffer _cmd;
        private Vector2       _lastCameraXZ;

        private static readonly int PropStampUV     = Shader.PropertyToID("_StampUV");
        private static readonly int PropBrushRadius = Shader.PropertyToID("_BrushRadius");
        private static readonly int PropBrushTex    = Shader.PropertyToID("_BrushTex");

        private const int   BrushResolution = 64;
        private const float MinThreshold    = 0.001f;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_stampShader == null)
            {
                Debug.LogError("[FowController] Stamp shader is not assigned.");
                return;
            }

            int size = Mathf.Max(_fowTextureSize, 1);
            FowRT            = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            FowRT.filterMode = FilterMode.Bilinear;
            FowRT.Create();

            _brushTexture  = GenerateBrushTexture(BrushResolution);
            _stampMaterial = new Material(_stampShader) { hideFlags = HideFlags.HideAndDontSave };
            _stampMaterial.SetTexture(PropBrushTex, _brushTexture);

            _cmd = new CommandBuffer { name = "FoW Brush Stamp" };
        }

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera != null)
                _lastCameraXZ = CameraXZ();
        }

        private void LateUpdate()
        {
            if (_camera == null || MinimapController.Instance == null || FowRT == null) return;

            Vector2 camXZ    = CameraXZ();
            float   sqrDelta = (camXZ - _lastCameraXZ).sqrMagnitude;
            float   sqrThreshold = _movementThreshold * _movementThreshold;

            if (sqrDelta <= Mathf.Max(sqrThreshold, MinThreshold)) return;

            _lastCameraXZ = camXZ;
            StampAtCamera();
        }

        private void OnDestroy()
        {
            _cmd?.Dispose();
            if (_stampMaterial != null) Destroy(_stampMaterial);
            if (_brushTexture  != null) Destroy(_brushTexture);
            if (FowRT != null) { FowRT.Release(); Destroy(FowRT); }
        }

        // ── Stamping ─────────────────────────────────────────────────────────

        private void StampAtCamera()
        {
            Vector2 stampUV = MinimapController.Instance.WorldToMinimapUV(_camera.transform.position);

            _stampMaterial.SetVector(PropStampUV,     new Vector4(stampUV.x, stampUV.y, 0f, 0f));
            _stampMaterial.SetFloat (PropBrushRadius, _brushRadiusUV);

            _cmd.Clear();
            // whiteTexture is a dummy source; the shader ignores it and uses _BrushTex.
            _cmd.Blit(Texture2D.whiteTexture, FowRT, _stampMaterial);
            Graphics.ExecuteCommandBuffer(_cmd);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private Vector2 CameraXZ()
        {
            Vector3 p = _camera.transform.position;
            return new Vector2(p.x, p.z);
        }

        /// <summary>
        /// Generates a soft radial gradient (white center → transparent edge).
        /// Stored in the R channel; quadratic falloff for a smooth fog brush.
        /// </summary>
        private static Texture2D GenerateBrushTexture(int resolution)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.R8, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp
            };

            var  pixels = new Color[resolution * resolution];
            float center = (resolution - 1) * 0.5f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float t    = Mathf.Clamp01(1f - dist / center);
                    float soft = t * t;     // quadratic falloff → soft edge
                    pixels[y * resolution + x] = new Color(soft, soft, soft, soft);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }
    }
}
