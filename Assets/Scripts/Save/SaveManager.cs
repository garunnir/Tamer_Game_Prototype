using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton MonoBehaviour that orchestrates save and load operations.
    ///
    /// Setup: add to a scene GameObject (or DontDestroyOnLoad object) and assign:
    ///   _player         — the PlayerController in the scene
    ///   _fowController  — the FowController in the scene
    ///   _squadPrefabs   — one MonsterUnit prefab entry per tameable species;
    ///                     each prefab must have MonsterData assigned
    ///
    /// The MonsterData SO's asset file name (e.g. "MonsterData_NormalA") is used as
    /// the save ID — no extra field required on MonsterData.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        public static SaveManager Instance { get; private set; }

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Scene References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private FowController    _fowController;

        [Header("Squad Prefabs — one entry per tameable monster type")]
        [SerializeField] private MonsterUnit[] _squadPrefabs;

        // ── Runtime state ────────────────────────────────────────────────────

        private readonly HashSet<string>                 _globalUnlocks = new HashSet<string>();
        private          Dictionary<string, MonsterUnit> _prefabDict;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "save.json");

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _prefabDict = new Dictionary<string, MonsterUnit>();
            if (_squadPrefabs != null)
            {
                foreach (MonsterUnit prefab in _squadPrefabs)
                {
                    if (prefab != null && prefab.Data != null)
                        _prefabDict[prefab.Data.name] = prefab;
                    else
                        Debug.LogWarning("[SaveManager] A squad prefab entry is null or missing MonsterData — skipped.");
                }
            }

        }

        private void OnEnable()  => GlobalEvents.OnTamingSucceeded += OnTamingSucceeded;
        private void OnDisable() => GlobalEvents.OnTamingSucceeded -= OnTamingSucceeded;

        private void OnTamingSucceeded(ITameable unit)
        {
            if (unit is MonsterUnit m && m.Data != null)
                _globalUnlocks.Add(m.Data.name);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Collects current game state (sync) then writes to disk after async fog readback.
        /// The file is written inside the fog callback — always on the main thread.
        /// </summary>
        public void Save()
        {
            var save = new SaveData();
            save.playerPosition = FromV3(_player.transform.position);

            // All units in the scene
            save.units = new List<UnitData>();
            foreach (MonsterUnit mu in FindObjectsOfType<MonsterUnit>())
            {
                if (mu.Data == null || !mu.IsAlive) continue;

                save.units.Add(new UnitData
                {
                    monsterId  = mu.Data.name,
                    hpFraction = Mathf.Clamp01(mu.CurrentHP / mu.Data.MaxHP),
                    position   = FromV3(mu.transform.position),
                    state      = (int)mu.CurrentState,
                    faction    = (int)mu.Faction
                });
            }

            // Formation
            save.formationIndex = FlockManager.Instance != null
                ? FlockManager.Instance.CurrentFormationIndex
                : 0;

            save.globalUnlocks = new List<string>(_globalUnlocks);

            if (_fowController != null)
            {
                _fowController.RequestFogSnapshot(bytes =>
                {
                    if (bytes != null)
                        save.fog = new FogData
                        {
                            width        = FowController.FogSnapshotSize,
                            height       = FowController.FogSnapshotSize,
                            pixelsBase64 = Convert.ToBase64String(bytes)
                        };

                    WriteSave(save);
                });
            }
            else
            {
                WriteSave(save);
            }
        }

        /// <summary>
        /// Reads save.json and restores player position, fog, squad, and global unlocks.
        /// </summary>
        public void Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveManager] No save file found — nothing to load.");
                return;
            }

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                Debug.LogWarning("[SaveManager] Failed to parse save file.");
                return;
            }

            // Player position
            if (_player != null)
                _player.transform.position = ToV3(data.playerPosition);

            // Fog
            if (_fowController != null && data.fog?.pixelsBase64 != null)
                _fowController.RestoreFogSnapshot(
                    Convert.FromBase64String(data.fog.pixelsBase64),
                    data.fog.width,
                    data.fog.height);

            // Clear all existing units
            foreach (MonsterUnit mu in FindObjectsOfType<MonsterUnit>())
                mu.gameObject.SetActive(false);

            // Build combined prefab lookup (squad + spawn prefabs)
            var prefabLookup = new Dictionary<string, MonsterUnit>(_prefabDict);
            if (RespawnManager.Instance != null)
            {
                foreach (MonsterUnit prefab in RespawnManager.Instance.SpawnPrefabs)
                {
                    if (prefab != null && prefab.Data != null)
                        prefabLookup.TryAdd(prefab.Data.name, prefab);
                }
            }

            // Spawn all saved units
            if (data.units != null)
            {
                foreach (UnitData entry in data.units)
                {
                    if (!prefabLookup.TryGetValue(entry.monsterId, out MonsterUnit prefab))
                    {
                        Debug.LogWarning($"[SaveManager] No prefab for id '{entry.monsterId}' — skipping.");
                        continue;
                    }

                    MonsterUnit unit = Instantiate(prefab, ToV3(entry.position), Quaternion.identity);
                    unit.SetHP(entry.hpFraction * prefab.Data.MaxHP);

                    if ((FactionId)entry.faction == FactionId.Player)
                        unit.SetFaction(FactionId.Player);  // BecomeFlockUnit → FlockManager.AddUnit
                }
            }

            // Formation
            if (FlockManager.Instance != null && data.formationIndex >= 0)
                FlockManager.Instance.SetFormationIndex(data.formationIndex);

            // Global unlocks
            if (data.globalUnlocks != null)
            {
                foreach (string id in data.globalUnlocks)
                    _globalUnlocks.Add(id);
            }

        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void WriteSave(SaveData save)
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(save));
            Debug.Log($"[SaveManager] Saved → {SavePath}");
        }

        private static Vec3S   FromV3(Vector3 v) => new Vec3S { x = v.x, y = v.y, z = v.z };
        private static Vector3 ToV3(Vec3S s)     => new Vector3(s.x, s.y, s.z);
    }
}
