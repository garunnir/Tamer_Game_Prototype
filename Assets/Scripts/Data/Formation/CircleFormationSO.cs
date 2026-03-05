using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// [Circle] 원형 포메이션.
    /// 유닛 간격(spacing)을 유지하도록 반지름을 자동 계산한다.
    ///
    /// 원의 둘레 = 2π × radius 이므로,
    /// n개 유닛을 spacing 간격으로 배치하려면: radius = spacing × n / (2π)
    /// 각 유닛의 각도: angle = (2π / n) × i
    /// </summary>
    [CreateAssetMenu(fileName = "CircleFormation", menuName = "WildTamer/Formation/Circle")]
    public class CircleFormationSO : FormationDataSO
    {
        public override void Compute(int unitCount, float spacing, Vector3[] offsets)
        {
            if (unitCount == 1)
            {
                offsets[0] = Vector3.zero;
                return;
            }

            // 반지름: n개 점을 spacing 간격으로 원주에 고르게 배치
            float twoPi  = 2f * Mathf.PI;
            float radius = spacing * unitCount / twoPi;
            float step   = twoPi / unitCount;

            for (int i = 0; i < unitCount; i++)
            {
                float angle = step * i;
                offsets[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
        }
    }
}
