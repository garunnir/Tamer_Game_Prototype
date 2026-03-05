using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// [Square] 격자(Grid) 포메이션.
    /// gridSize = Ceil(Sqrt(n)) 열(column)로 배치.
    ///
    /// row = i / gridSize  (행 번호)
    /// col = i % gridSize  (열 번호)
    /// 중심 정렬: 각 축을 -(gridSize-1)/2 × spacing 만큼 이동
    /// </summary>
    [CreateAssetMenu(fileName = "SquareFormation", menuName = "WildTamer/Formation/Square")]
    public class SquareFormationSO : FormationDataSO
    {
        public override void Compute(int unitCount, float spacing, Vector3[] offsets)
        {
            // 정사각형에 가장 가까운 그리드 크기
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            // 그리드 중심을 원점으로 이동하기 위한 오프셋
            float centerOffset = -(gridSize - 1) * 0.5f * spacing;

            for (int i = 0; i < unitCount; i++)
            {
                int row = i / gridSize;
                int col = i % gridSize;
                float x = col * spacing + centerOffset;
                float z = row * spacing + centerOffset;
                offsets[i] = new Vector3(x, 0f, z);
            }
        }
    }
}
