using UnityEditor;
using UnityEngine;

namespace WildTamer
{
    public static class MonsterDataGenerator
    {
        private const string OutputFolder = "Assets/ScriptableObjects";

        [MenuItem("WildTamer/Generate Monster Data Assets")]
        public static void GenerateAllMonsterData()
        {
            EnsureFolderExists(OutputFolder);

            CreateAsset("MonsterData_NormalA", monsterName: "Normal A",
                maxHP: 80,  attackDamage: 10f, moveSpeed: 3.5f,
                attackRange: 1.5f, detectionRange: 8f,  tamingChance: 0.30f);

            CreateAsset("MonsterData_NormalB", monsterName: "Normal B",
                maxHP: 120, attackDamage: 15f, moveSpeed: 2.5f,
                attackRange: 2.0f, detectionRange: 10f, tamingChance: 0.20f);

            CreateAsset("MonsterData_NormalC", monsterName: "Normal C",
                maxHP: 60,  attackDamage: 8f,  moveSpeed: 5.0f,
                attackRange: 1.2f, detectionRange: 6f,  tamingChance: 0.40f);

            CreateAsset("MonsterData_BossA", monsterName: "Boss A",
                maxHP: 500, attackDamage: 40f, moveSpeed: 2.0f,
                attackRange: 3.0f, detectionRange: 15f, tamingChance: 0.05f);

            CreateAsset("MonsterData_BossB", monsterName: "Boss B",
                maxHP: 400, attackDamage: 35f, moveSpeed: 4.0f,
                attackRange: 2.5f, detectionRange: 12f, tamingChance: 0.05f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MonsterDataGenerator] 5 MonsterData assets created in " + OutputFolder);
        }

        private static void CreateAsset(
            string assetFileName,
            string monsterName,
            int    maxHP,
            float  attackDamage,
            float  moveSpeed,
            float  attackRange,
            float  detectionRange,
            float  tamingChance)
        {
            string path = $"{OutputFolder}/{assetFileName}.asset";

            MonsterData existing = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
            if (existing != null)
            {
                Debug.LogWarning($"[MonsterDataGenerator] Asset already exists, skipping: {path}");
                return;
            }

            MonsterData data = ScriptableObject.CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(data, path);

            // Populate via SerializedObject so private [SerializeField] fields are written correctly.
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_monsterName").stringValue    = monsterName;
            so.FindProperty("_maxHP").intValue             = maxHP;
            so.FindProperty("_attackDamage").floatValue    = attackDamage;
            so.FindProperty("_moveSpeed").floatValue       = moveSpeed;
            so.FindProperty("_attackRange").floatValue     = attackRange;
            so.FindProperty("_detectionRange").floatValue  = detectionRange;
            so.FindProperty("_tamingChance").floatValue    = tamingChance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
                string newFolder = System.IO.Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parent, newFolder);
            }
        }
    }
}
