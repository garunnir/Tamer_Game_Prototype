using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// [Wedge] V자(쐐기) 포메이션.
    /// i=0이 선두(리더)로 z=0에 위치.
    /// i>0인 경우 양쪽 날개로 퍼짐:
    ///   row  = Ceil(i / 2)  → 몇 번째 열(row)인지
    ///   side = i%2==0 ? -1 : 1  → 좌(-) 혹은 우(+)
    ///   x = side × row × spacing
    ///   z = -row × spacing  (리더 뒤로 갈수록 z 감소)
    /// </summary>
    [CreateAssetMenu(fileName = "WedgeFormation", menuName = "WildTamer/Formation/Wedge")]
    public class WedgeFormationSO : FormationDataSO
    {
        public override void Compute(int unitCount, float spacing, Vector3[] offsets)
        {
            // 선두 유닛은 원점에 배치
            offsets[0] = Vector3.zero;

            for (int i = 1; i < unitCount; i++)
            {
                // Ceil(i/2): 짝수/홀수 쌍을 같은 열(row)에 배치
                int row  = Mathf.CeilToInt(i * 0.5f);
                // 짝수 인덱스 → 왼쪽(-1), 홀수 인덱스 → 오른쪽(+1)
                int side = (i % 2 == 0) ? -1 : 1;

                float x = side * row * spacing;
                float z = -row * spacing;
                offsets[i] = new Vector3(x, 0f, z);
            }
        }
    }
}
