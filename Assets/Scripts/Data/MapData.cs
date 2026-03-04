using UnityEngine;

namespace WildTamer
{
    [CreateAssetMenu(fileName = "MapData", menuName = "WildTamer/Map Data")]
    public class MapData : ScriptableObject
    {
        [SerializeField] private Vector2 _worldCenter = Vector2.zero;
        [SerializeField] private Vector2 _worldSize   = new Vector2(100f, 100f);

        [Header("Local View")]
        [Tooltip("When true the minimap displays a radius around the player (Local mode) "
               + "instead of the entire world (Global mode).")]
        [SerializeField] private bool  _useLocalView = false;

        [Tooltip("World-space radius shown around the player in Local mode. "
               + "Has no effect when UseLocalView is false.")]
        [SerializeField, Min(1f)] private float _viewRadius = 30f;

        private const float MinWorldSizeAxis = 1f;

        public Vector2 WorldCenter   => _worldCenter;
        public bool    UseLocalView  => _useLocalView;
        public float   ViewRadius    => _viewRadius;

        /// <summary>
        /// World size clamped to a minimum on each axis to prevent division by zero.
        /// </summary>
        public Vector2 SafeWorldSize => new Vector2(
            Mathf.Max(_worldSize.x, MinWorldSizeAxis),
            Mathf.Max(_worldSize.y, MinWorldSizeAxis)
        );
    }
}
