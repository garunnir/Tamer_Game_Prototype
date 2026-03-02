using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Calculates per-unit circular formation offsets around the player.
    /// Call Recalculate() whenever the flock size changes, then query
    /// GetOffset(index) each frame to obtain each unit's personal target slot.
    /// Contains no unit or player references — pure offset math only.
    /// </summary>
    public class FormationHelper : MonoBehaviour
    {
        [Header("Formation")]
        [SerializeField] private float _radius = 2f;

        private Vector3[] _offsets = System.Array.Empty<Vector3>();

        /// <summary>
        /// Rebuilds the offset array for <paramref name="unitCount"/> units.
        /// Must be called whenever a unit is added or removed.
        /// </summary>
        public void Recalculate(int unitCount)
        {
            if (unitCount <= 0)
            {
                _offsets = System.Array.Empty<Vector3>();
                return;
            }

            _offsets = new Vector3[unitCount];

            if (unitCount == 1)
            {
                // Single unit follows directly on the player position
                _offsets[0] = Vector3.zero;
                return;
            }

            float angleStep = (Mathf.PI * 2f) / unitCount;

            for (int i = 0; i < unitCount; i++)
            {
                float angle = angleStep * i;
                _offsets[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _radius;
            }
        }

        /// <summary>
        /// Returns the world-space offset for the unit at <paramref name="index"/>.
        /// Returns <see cref="Vector3.zero"/> if the index is out of range so callers
        /// never receive a stale or invalid value.
        /// </summary>
        public Vector3 GetOffset(int index)
        {
            if (index < 0 || index >= _offsets.Length)
                return Vector3.zero;

            return _offsets[index];
        }

        /// <summary>Number of slots currently allocated.</summary>
        public int SlotCount => _offsets.Length;
    }
}
