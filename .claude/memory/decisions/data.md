# Decisions — Monster Data

- No MonsterType enum — MonsterData asset itself is the type identifier (asset name = ID)
- MonsterDataGenerator uses SerializedObject.FindProperty() to set private SerializeField fields — direct assignment after CreateAsset() does not serialize correctly
