using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace WildTamer
{
    /// <summary>
    /// Displays colored dot icons on the minimap for all live MonsterUnits and the player.
    ///
    /// Icon colours:
    ///   Blue  — Player / Ally faction (always visible on the minimap).
    ///   Red   — Enemy faction (visible only when the unit's world position falls in a
    ///           FoW-revealed area, i.e. the R channel of the FoW RenderTexture > threshold).
    ///
    /// Performance rules (CLAUDE.md):
    ///   - Icon positions update via InvokeRepeating every _iconUpdateInterval (default 0.1 s).
    ///   - FoW pixel data is fetched asynchronously with AsyncGPUReadback every
    ///     _fowSampleInterval (default 0.2 s); the result is cached in a managed Color32[]
    ///     so visibility checks are pure CPU array reads with no GPU stalls.
    ///
    /// Registration:
    ///   MonsterUnit.Start()     calls MinimapUnitTracker.Instance?.Register(this).
    ///   MonsterUnit.OnDisable() calls MinimapUnitTracker.Instance?.Unregister(this).
    ///   The player is tracked via a serialized Transform reference (no MonsterUnit needed).
    ///
    /// Faction changes (taming / release) are detected automatically each position-update
    /// tick and trigger an icon pool swap — no additional event subscriptions required.
    /// </summary>
    public class MinimapUnitTracker : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static MinimapUnitTracker Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Scene References")]
        [Tooltip("RectTransform of the minimap panel. Icons are parented and positioned here.")]
        [SerializeField] private RectTransform _minimapRect;

        [Tooltip("The player's Transform. A permanent blue icon tracks this position.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Icon Prefabs")]
        [Tooltip("Prefab for Player / Ally dots (blue). RectTransform with an Image component.")]
        [SerializeField] private RectTransform _allyIconPrefab;

        [Tooltip("Prefab for Enemy dots (red). RectTransform with an Image component.")]
        [SerializeField] private RectTransform _enemyIconPrefab;

        [Header("Timing")]
        [SerializeField, Range(0.05f, 0.5f)] private float _iconUpdateInterval = 0.1f;
        [SerializeField, Range(0.10f, 1.0f)] private float _fowSampleInterval  = 0.2f;

        [Header("FoW Visibility")]
        [Tooltip("R-channel threshold above which a pixel counts as 'revealed' "
               + "(FoW RT uses additive accumulation: 0 = fogged, 1 = fully clear).")]
        [SerializeField, Range(0f, 1f)] private float _revealThreshold = 0.05f;

        // ── Inner type ───────────────────────────────────────────────────────

        private sealed class TrackedUnit
        {
            public MonsterUnit   Unit;
            public RectTransform Icon;
            public bool          IsAllyIcon; // which pool this icon came from
        }

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly List<TrackedUnit>    _tracked   = new List<TrackedUnit>();
        private RectTransform                 _playerIcon;

        // Async FoW CPU cache (written in readback callback, read in UpdateIconPositions)
        private Color32[] _fowCache;
        private int       _fowCacheWidth;
        private int       _fowCacheHeight;
        private bool      _fowReadPending;

        // Pooled inactive icons — keyed by ally vs enemy prefab type
        private readonly Queue<RectTransform> _allyPool  = new Queue<RectTransform>();
        private readonly Queue<RectTransform> _enemyPool = new Queue<RectTransform>();

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (_playerTransform != null)
                _playerIcon = SpawnIcon(isAlly: true);

            // Stagger readback slightly so it doesn't coincide with the first icon update.
            InvokeRepeating(nameof(UpdateIconPositions), 0f,    _iconUpdateInterval);
            InvokeRepeating(nameof(RequestFowReadback),  0.05f, _fowSampleInterval);

            GlobalEvents.OnUnitDied += HandleUnitDied;
        }

        private void OnDestroy()
        {
            GlobalEvents.OnUnitDied -= HandleUnitDied;
        }

        // ── Registration API (called by MonsterUnit) ─────────────────────────

        /// <summary>Begin tracking <paramref name="unit"/> on the minimap.</summary>
        public void Register(MonsterUnit unit)
        {
            if (unit == null) return;

            // Guard against duplicate registration.
            foreach (TrackedUnit t in _tracked)
                if (t.Unit == unit) return;

            bool isAlly = unit.Faction == FactionId.Player;
            _tracked.Add(new TrackedUnit
            {
                Unit       = unit,
                Icon       = GetPooledIcon(isAlly),
                IsAllyIcon = isAlly
            });
        }

        /// <summary>Stop tracking <paramref name="unit"/> and return its icon to the pool.</summary>
        public void Unregister(MonsterUnit unit)
        {
            if (unit == null) return;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (_tracked[i].Unit != unit) continue;

                ReturnToPool(_tracked[i].Icon, _tracked[i].IsAllyIcon);
                _tracked.RemoveAt(i);
                return;
            }
        }

        // ── Icon Position Update (InvokeRepeating) ───────────────────────────

        private void UpdateIconPositions()
        {
            if (_minimapRect == null || MinimapController.Instance == null) return;

            // Player icon — permanent, always blue, always visible.
            if (_playerIcon != null && _playerTransform != null)
            {
                Vector2 uv = MinimapController.Instance.WorldToMinimapUV(_playerTransform.position);
                PlaceIcon(_playerIcon, uv);
                _playerIcon.gameObject.SetActive(true);
            }

            // Unit icons
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                TrackedUnit entry = _tracked[i];

                if (entry.Unit == null || !entry.Unit.IsAlive)
                {
                    // Dead units will be removed by HandleUnitDied; just hide for now.
                    if (entry.Icon != null)
                        entry.Icon.gameObject.SetActive(false);
                    continue;
                }

                // ── Detect faction swap (taming Enemy→Player or release Player→Neutral) ──
                bool shouldBeAlly = entry.Unit.Faction == FactionId.Player;
                if (shouldBeAlly != entry.IsAllyIcon)
                {
                    ReturnToPool(entry.Icon, entry.IsAllyIcon);
                    entry.Icon       = GetPooledIcon(shouldBeAlly);
                    entry.IsAllyIcon = shouldBeAlly;
                }

                if (entry.Icon == null) continue;

                // ── Position ──
                Vector2 uv = MinimapController.Instance.WorldToMinimapUV(entry.Unit.Transform.position);
                PlaceIcon(entry.Icon, uv);

                // ── Visibility ──
                // Allies are always visible.
                // Enemies are only visible when their position is revealed in the FoW mask.
                bool visible = shouldBeAlly || IsRevealed(uv);
                entry.Icon.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Positions <paramref name="icon"/> within the minimap panel using UV [0,1] coordinates.
        /// anchorMin/Max must be (0.5, 0.5) so anchoredPosition is relative to the panel center.
        /// </summary>
        private void PlaceIcon(RectTransform icon, Vector2 uv)
        {
            Vector2 panelSize     = _minimapRect.rect.size;
            icon.anchoredPosition = new Vector2(
                (uv.x - 0.5f) * panelSize.x,
                (uv.y - 0.5f) * panelSize.y);
        }

        // ── FoW AsyncGPUReadback (InvokeRepeating) ───────────────────────────

        private void RequestFowReadback()
        {
            RenderTexture rt = FowController.Instance?.FowRT;
            if (rt == null || _fowReadPending) return;

            _fowReadPending = true;
            AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, OnFowReadback);
        }

        private void OnFowReadback(AsyncGPUReadbackRequest request)
        {
            _fowReadPending = false;

            if (request.hasError)
            {
                Debug.LogWarning("[MinimapUnitTracker] AsyncGPUReadback failed — FoW visibility unchanged.");
                return;
            }

            _fowCacheWidth  = request.width;
            _fowCacheHeight = request.height;

            // GetData is only valid within this callback — copy immediately.
            NativeArray<Color32> raw = request.GetData<Color32>();

            if (_fowCache == null || _fowCache.Length != raw.Length)
                _fowCache = new Color32[raw.Length];

            raw.CopyTo(_fowCache);
        }

        /// <summary>
        /// Samples the CPU-side FoW cache at <paramref name="uv"/> and returns true
        /// if the R channel exceeds <see cref="_revealThreshold"/> (i.e., area is revealed).
        /// Returns false until the first readback completes (safe default: hide enemies).
        /// </summary>
        private bool IsRevealed(Vector2 uv)
        {
            if (_fowCache == null || _fowCacheWidth == 0 || _fowCacheHeight == 0)
                return false;

            int px = Mathf.Clamp(Mathf.FloorToInt(uv.x * _fowCacheWidth),  0, _fowCacheWidth  - 1);
            int py = Mathf.Clamp(Mathf.FloorToInt(uv.y * _fowCacheHeight), 0, _fowCacheHeight - 1);

            float r = _fowCache[py * _fowCacheWidth + px].r / 255f;
            return r > _revealThreshold;
        }

        // ── Event Handlers ───────────────────────────────────────────────────

        private void HandleUnitDied(ITargetable unit)
        {
            if (unit is MonsterUnit mu) Unregister(mu);
        }

        // ── Icon Pool ────────────────────────────────────────────────────────

        private RectTransform GetPooledIcon(bool isAlly)
        {
            Queue<RectTransform> pool = isAlly ? _allyPool : _enemyPool;

            if (pool.Count > 0)
            {
                RectTransform recycled = pool.Dequeue();
                recycled.gameObject.SetActive(false);
                return recycled;
            }

            return SpawnIcon(isAlly);
        }

        private void ReturnToPool(RectTransform icon, bool wasAlly)
        {
            if (icon == null) return;
            icon.gameObject.SetActive(false);
            if (wasAlly) _allyPool.Enqueue(icon);
            else         _enemyPool.Enqueue(icon);
        }

        /// <summary>
        /// Instantiates an icon from the appropriate prefab, parents it to the minimap panel,
        /// and sets pivot / anchor to center so <see cref="PlaceIcon"/> works correctly.
        /// </summary>
        private RectTransform SpawnIcon(bool isAlly)
        {
            RectTransform prefab = isAlly ? _allyIconPrefab : _enemyIconPrefab;

            if (prefab == null)
            {
                Debug.LogError($"[MinimapUnitTracker] {(isAlly ? "Ally" : "Enemy")} icon prefab is not assigned.");
                return null;
            }

            RectTransform icon = Instantiate(prefab, _minimapRect);
            icon.anchorMin        = new Vector2(0.5f, 0.5f);
            icon.anchorMax        = new Vector2(0.5f, 0.5f);
            icon.pivot            = new Vector2(0.5f, 0.5f);
            icon.gameObject.SetActive(false);
            return icon;
        }
    }
}
