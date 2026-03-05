using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace WildTamer
{
    /// <summary>
    /// Burst-compiled parallel job that computes Boids steering for all flock units.
    /// Runs on worker threads; outputs desired velocities consumed by FlockManager on main thread.
    ///
    /// Per-unit:
    ///   1. Separation     — repulsion from nearby flock members
    ///   2. PlayerSep      — repulsion from player character
    ///   3. TargetSeek     — attraction toward assigned formation slot
    ///   4. MoveTowards    — smooth velocity integration (mirrors Vector3.MoveTowards)
    /// </summary>
    [BurstCompile]
    public struct FlockSteeringJob : IJobParallelFor
    {
        // ── Inputs (read-only) ────────────────────────────────────────────────

        [ReadOnly] public NativeArray<float3> Positions;       // world pos per unit
        [ReadOnly] public NativeArray<float3> TargetSlots;     // formation slot per unit
        [ReadOnly] public NativeArray<bool>   Suspended;       // skip flag (motion paused / not following)
        [ReadOnly] public NativeArray<float3> CurrentVelocities;

        [ReadOnly] public float3 PlayerPosition;

        // ── Steering parameters ───────────────────────────────────────────────

        [ReadOnly] public float SeparationRadiusSqr;
        [ReadOnly] public float PlayerSeparationRadiusSqr;
        [ReadOnly] public float SeparationWeight;
        [ReadOnly] public float PlayerSeparationWeight;
        [ReadOnly] public float TargetWeight;
        [ReadOnly] public float MaxSpeed;
        [ReadOnly] public float SlowingDistance;
        [ReadOnly] public float Acceleration;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public int   UnitCount;

        // ── Output ────────────────────────────────────────────────────────────

        [WriteOnly] public NativeArray<float3> OutVelocities;

        // ── Execute ───────────────────────────────────────────────────────────

        public void Execute(int i)
        {
            // Suspended units (dead, hit-stunned, not in Follow state) pass through unchanged.
            if (Suspended[i])
            {
                OutVelocities[i] = CurrentVelocities[i];
                return;
            }

            float3 pos  = Positions[i];
            float3 slot = TargetSlots[i];

            // Arrival damping: linear falloff as unit approaches its slot.
            float distToSlot = math.distance(pos, slot);
            float jitter     = math.clamp(distToSlot / math.max(SlowingDistance, 0.0001f), 0f, 1f);

            // ── 1. Separation ─────────────────────────────────────────────────
            float3 sep = float3.zero;
            for (int j = 0; j < UnitCount; j++)
            {
                if (j == i) continue;

                float3 diff   = pos - Positions[j];
                float  sqrDst = math.lengthsq(diff);

                if (sqrDst < SeparationRadiusSqr && sqrDst > 0.00001f)
                    sep += math.normalize(diff) / math.sqrt(sqrDst);
            }

            float sepMag = math.length(sep);
            if (sepMag > 0f)
                sep = sep / sepMag; // normalize

            // ── 2. Player separation ──────────────────────────────────────────
            float3 playerSep = float3.zero;
            float3 pDiff     = pos - PlayerPosition;
            float  pSqr      = math.lengthsq(pDiff);

            if (pSqr < PlayerSeparationRadiusSqr && pSqr > 0.0001f)
                playerSep = math.normalize(pDiff) / math.sqrt(pSqr);

            // ── 3. Target seek ────────────────────────────────────────────────
            float3 toTarget   = slot - pos;
            float  targetDist = math.length(toTarget);
            float3 seek       = targetDist > 0.001f ? toTarget / targetDist : float3.zero;

            // ── 4. Combine & clamp ────────────────────────────────────────────
            float3 desired    = sep * SeparationWeight * jitter
                              + playerSep * PlayerSeparationWeight
                              + seek * TargetWeight;

            float desiredMag = math.length(desired);
            if (desiredMag > 0.001f)
                desired = (desired / desiredMag) * (MaxSpeed * jitter);

            // ── 5. MoveTowards (mirrors Vector3.MoveTowards) ─────────────────
            float3 current  = CurrentVelocities[i];
            float  maxDelta = Acceleration * DeltaTime;
            float3 delta    = desired - current;
            float  deltaMag = math.length(delta);

            OutVelocities[i] = deltaMag <= maxDelta || deltaMag < 0.00001f
                ? desired
                : current + (delta / deltaMag) * maxDelta;
        }
    }
}
