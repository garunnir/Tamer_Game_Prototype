using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Deactivates the GameObject after Delay seconds from the moment it becomes active.
    /// Used by warning-ring objects (AoeExplosionAttackLogic) so they self-clean
    /// regardless of what happens to the owning unit.
    /// </summary>
    public class TimedDeactivate : MonoBehaviour
    {
        public float Delay;

        private float _timer;

        private void OnEnable()
        {
            _timer = Delay;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                gameObject.SetActive(false);
        }
    }
}
