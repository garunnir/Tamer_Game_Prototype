using UnityEngine;

namespace WildTamer
{
    [CreateAssetMenu(fileName = "MapData", menuName = "WildTamer/Map Data")]
    public class MapData : ScriptableObject
    {
        [SerializeField] private Vector2 _worldCenter = Vector2.zero;
        [SerializeField] private Vector2 _worldSize   = new Vector2(100f, 100f);

        private const float MinWorldSizeAxis = 1f;

        public Vector2 WorldCenter => _worldCenter;

        /// <summary>
        /// World size clamped to a minimum on each axis to prevent division by zero.
        /// </summary>
        public Vector2 SafeWorldSize => new Vector2(
            Mathf.Max(_worldSize.x, MinWorldSizeAxis),
            Mathf.Max(_worldSize.y, MinWorldSizeAxis)
        );
    }
}
