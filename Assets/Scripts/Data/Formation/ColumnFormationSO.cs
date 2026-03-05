using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// [Column] 세로줄(Z축) 포메이션.
    /// 전체 길이의 중심이 원점에 오도록 오프셋 적용.
    ///
    /// z = (i - (n-1)/2) × spacing
    /// </summary>
    [CreateAssetMenu(fileName = "ColumnFormation", menuName = "WildTamer/Formation/Column")]
    public class ColumnFormationSO : FormationDataSO
    {
        public override void Compute(int unitCount, float spacing, Vector3[] offsets)
        {
            float halfSpan = (unitCount - 1) * 0.5f;
            for (int i = 0; i < unitCount; i++)
            {
                float z = (i - halfSpan) * spacing;
                offsets[i] = new Vector3(0f, 0f, z);
            }
        }
    }
}
