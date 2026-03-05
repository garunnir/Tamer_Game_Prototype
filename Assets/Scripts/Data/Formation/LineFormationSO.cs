using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// [Line] 가로줄(X축) 포메이션.
    /// 전체 길이의 중심이 원점에 오도록 오프셋 적용.
    ///
    /// x = (i - (n-1)/2) × spacing
    /// </summary>
    [CreateAssetMenu(fileName = "LineFormation", menuName = "WildTamer/Formation/Line")]
    public class LineFormationSO : FormationDataSO
    {
        public override void Compute(int unitCount, float spacing, Vector3[] offsets)
        {
            // (n-1)/2f : 전체 배열의 중앙 인덱스 → 중심 정렬
            float halfSpan = (unitCount - 1) * 0.5f;
            for (int i = 0; i < unitCount; i++)
            {
                float x = (i - halfSpan) * spacing;
                offsets[i] = new Vector3(x, 0f, 0f);
            }
        }
    }
}
