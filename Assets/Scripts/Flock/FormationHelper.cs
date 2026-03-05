using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 유닛 포메이션 슬롯 오프셋을 계산하는 순수 수학 유틸리티.
    /// 더티 플래그(dirty flag) 캐싱으로 unitCount, spacing, type이 변경될 때만
    /// 재계산하여 GC 및 CPU 부하를 최소화한다.
    /// 유닛/플레이어 참조 없음 — 순수 오프셋 수학만 담당.
    /// </summary>
    public class FormationHelper : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────
        [Header("Formation")]
        [SerializeField] private FormationType _formationType = FormationType.Circle;
        [SerializeField] private float _spacing = 1.5f;

        // ─── Cache (dirty-flag) ───────────────────────────────────────────────
        private int _cachedUnitCount = -1;
        private float _cachedSpacing = -1f;
        private FormationType _cachedType = (FormationType)(-1);

        private Vector3[] _offsets = System.Array.Empty<Vector3>();

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// 포메이션 슬롯 오프셋 배열을 재계산한다.
        /// unitCount, _spacing, _formationType 중 하나라도 변경되었을 때만 실제로 연산한다.
        /// </summary>
        public void Recalculate(int unitCount)
        {
            // 더티 플래그: 입력이 동일하면 재계산 생략
            if (unitCount == _cachedUnitCount &&
                Mathf.Approximately(_spacing, _cachedSpacing) &&
                _formationType == _cachedType)
            {
                return;
            }

            _cachedUnitCount = unitCount;
            _cachedSpacing   = _spacing;
            _cachedType      = _formationType;

            if (unitCount <= 0)
            {
                _offsets = System.Array.Empty<Vector3>();
                return;
            }

            _offsets = new Vector3[unitCount];

            if (unitCount == 1)
            {
                _offsets[0] = Vector3.zero;
                return;
            }

            // 포메이션 타입에 따라 원시 위치 계산
            switch (_formationType)
            {
                case FormationType.Circle: ComputeCircle(unitCount); break;
                case FormationType.Line:   ComputeLine(unitCount);   break;
                case FormationType.Column: ComputeColumn(unitCount); break;
                case FormationType.Square: ComputeSquare(unitCount); break;
                case FormationType.Wedge:  ComputeWedge(unitCount);  break;
            }

            // 무게중심(centroid)을 원점(0,0,0)으로 정렬
            AlignToCentroid(unitCount);
        }

        /// <summary>index번째 유닛의 로컬 오프셋을 반환한다. 범위 밖이면 Vector3.zero.</summary>
        public Vector3 GetOffset(int index)
        {
            if (index < 0 || index >= _offsets.Length)
                return Vector3.zero;

            return _offsets[index];
        }

        /// <summary>현재 할당된 슬롯 수.</summary>
        public int SlotCount => _offsets.Length;

        // ─── Formation Math ───────────────────────────────────────────────────

        /// <summary>
        /// [Circle] 원형 포메이션.
        /// 유닛 간격(spacing)을 유지하도록 반지름을 자동 계산한다.
        ///
        /// 원의 둘레 = 2π × radius 이므로,
        /// n개 유닛을 spacing 간격으로 배치하려면: radius = spacing × n / (2π)
        /// 각 유닛의 각도: angle = (2π / n) × i
        /// </summary>
        private void ComputeCircle(int n)
        {
            // 반지름: n개 점을 spacing 간격으로 원주에 고르게 배치
            // 2π를 한 번만 계산하고 재사용 (루프 내 반복 계산 방지)
            float twoPi  = 2f * Mathf.PI;
            float radius = _spacing * n / twoPi;
            float step   = twoPi / n; // 유닛 간 각도 간격 (라디안)

            for (int i = 0; i < n; i++)
            {
                // i번째 점의 각도 (라디안)
                float angle = step * i;
                _offsets[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
        }

        /// <summary>
        /// [Line] 가로줄(X축) 포메이션.
        /// 전체 길이의 중심이 원점에 오도록 오프셋 적용.
        ///
        /// x = (i - (n-1)/2) × spacing
        /// </summary>
        private void ComputeLine(int n)
        {
            // (n-1)/2f : 전체 배열의 중앙 인덱스 → 중심 정렬
            float halfSpan = (n - 1) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float x = (i - halfSpan) * _spacing;
                _offsets[i] = new Vector3(x, 0f, 0f);
            }
        }

        /// <summary>
        /// [Column] 세로줄(Z축) 포메이션.
        /// 전체 길이의 중심이 원점에 오도록 오프셋 적용.
        ///
        /// z = (i - (n-1)/2) × spacing
        /// </summary>
        private void ComputeColumn(int n)
        {
            float halfSpan = (n - 1) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float z = (i - halfSpan) * _spacing;
                _offsets[i] = new Vector3(0f, 0f, z);
            }
        }

        /// <summary>
        /// [Square] 격자(Grid) 포메이션.
        /// gridSize = Ceil(Sqrt(n)) 열(column)로 배치.
        ///
        /// row = i / gridSize  (행 번호)
        /// col = i % gridSize  (열 번호)
        /// 중심 정렬: 각 축을 -(gridSize-1)/2 × spacing 만큼 이동
        /// </summary>
        private void ComputeSquare(int n)
        {
            // 정사각형에 가장 가까운 그리드 크기
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(n));
            // 그리드 중심을 원점으로 이동하기 위한 오프셋
            float centerOffset = -(gridSize - 1) * 0.5f * _spacing;

            for (int i = 0; i < n; i++)
            {
                int row = i / gridSize;
                int col = i % gridSize;
                float x = col * _spacing + centerOffset;
                float z = row * _spacing + centerOffset;
                _offsets[i] = new Vector3(x, 0f, z);
            }
        }

        /// <summary>
        /// [Wedge] V자(쐐기) 포메이션.
        /// i=0이 선두(리더)로 z=0에 위치.
        /// i>0인 경우 양쪽 날개로 퍼짐:
        ///   row  = Ceil(i / 2)  → 몇 번째 열(row)인지
        ///   side = i%2==0 ? -1 : 1  → 좌(-) 혹은 우(+)
        ///   x = side × row × spacing
        ///   z = -row × spacing  (리더 뒤로 갈수록 z 감소)
        /// </summary>
        private void ComputeWedge(int n)
        {
            // 선두 유닛은 원점에 배치
            _offsets[0] = Vector3.zero;

            for (int i = 1; i < n; i++)
            {
                // Ceil(i/2): 짝수/홀수 쌍을 같은 열(row)에 배치
                int row  = Mathf.CeilToInt(i * 0.5f);
                // 짝수 인덱스 → 왼쪽(-1), 홀수 인덱스 → 오른쪽(+1)
                int side = (i % 2 == 0) ? -1 : 1;

                float x = side * row * _spacing;
                float z = -row * _spacing; // 리더보다 뒤에 위치
                _offsets[i] = new Vector3(x, 0f, z);
            }
        }

        // ─── Centroid Alignment ────────────────────────────────────────────────

        /// <summary>
        /// 모든 슬롯 위치의 산술 평균(무게중심)을 계산하고,
        /// 각 위치에서 빼 포메이션 전체를 (0,0,0) 중심으로 정렬한다.
        ///
        /// centroid = (1/n) × Σ positions[i]
        /// positions[i] -= centroid
        /// </summary>
        private void AlignToCentroid(int n)
        {
            // 1단계: 무게중심 계산
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < n; i++)
                centroid += _offsets[i];
            centroid /= n;

            // 2단계: 각 슬롯에서 무게중심을 빼 원점 정렬
            for (int i = 0; i < n; i++)
                _offsets[i] -= centroid;
        }
    }

    /// <summary>지원하는 포메이션 타입.</summary>
    public enum FormationType
    {
        Circle,
        Line,
        Column,
        Square,
        Wedge
    }
}
