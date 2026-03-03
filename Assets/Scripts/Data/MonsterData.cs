using UnityEngine;

namespace WildTamer
{
    [CreateAssetMenu(fileName = "MonsterData", menuName = "WildTamer/Monster Data")]
    public class MonsterData : ScriptableObject
    {
        [SerializeField] private string _monsterName;

        [SerializeField] private int _maxHP;

        [SerializeField] private float _attackDamage;

        [SerializeField] private float _moveSpeed;

        [SerializeField] private float _attackRange;

        [SerializeField] private float _detectionRange;

        [SerializeField, Range(0f, 1f)] private float _tamingChance;

        [SerializeField] private float _attackCooldown = 1.5f;

        [Header("Default Strategies")]
        [Tooltip("Movement logic used unless overridden on the MonsterUnit prefab.")]
        [SerializeField] private MovementLogic _defaultMovementLogic;

        [Tooltip("Attack logic used unless overridden on the MonsterUnit prefab.")]
        [SerializeField] private AttackLogic _defaultAttackLogic;

        public string        MonsterName          => _monsterName;
        public int           MaxHP                => _maxHP;
        public float         AttackDamage         => _attackDamage;
        public float         MoveSpeed            => _moveSpeed;
        public float         AttackRange          => _attackRange;
        public float         DetectionRange       => _detectionRange;
        public float         TamingChance         => _tamingChance;
        public float         AttackCooldown       => _attackCooldown;
        public MovementLogic DefaultMovementLogic => _defaultMovementLogic;
        public AttackLogic   DefaultAttackLogic   => _defaultAttackLogic;
    }
}
