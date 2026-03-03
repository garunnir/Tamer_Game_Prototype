namespace WildTamer
{
    /// <summary>
    /// Implemented by any MonoBehaviour whose motion can be temporarily suspended
    /// by hit feedback effects (hitlag / hitstop).
    /// Implemented by MonsterBase (enemies) and FlockUnit (allies).
    /// EffectManager calls SuspendMotion() via GetComponent when a hit lands.
    /// </summary>
    public interface IHitReactive
    {
        /// <summary>
        /// Freezes this object's motion for at least <paramref name="duration"/> seconds.
        /// If already suspended, takes the longer of the two durations (Mathf.Max).
        /// </summary>
        void SuspendMotion(float duration);
    }
}
