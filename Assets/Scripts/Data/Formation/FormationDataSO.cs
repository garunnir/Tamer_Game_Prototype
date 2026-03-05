using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 포메이션 슬롯 오프셋을 계산하는 ScriptableObject 기반 전략 인터페이스.
    /// 각 포메이션 타입은 이 클래스를 상속해 Compute()를 구현한다.
    /// RotationOffset / PositionOffset으로 포메이션별 독립적인 회전·위치 보정을 지원한다.
    /// </summary>
    public abstract class FormationDataSO : ScriptableObject
    {
        [Header("Formation Offset")]
        [Tooltip("포메이션 전체에 적용되는 추가 Y축 회전 (도 단위). PlayerYaw에 합산된다.")]
        [SerializeField] private float _rotationOffset;

        [Tooltip("포메이션 앵커에 적용되는 로컬 위치 오프셋. 플레이어 회전이 적용되어 월드 좌표로 변환된다.")]
        [SerializeField] private Vector3 _positionOffset;

        /// <summary>포메이션 추가 회전 오프셋 (도 단위, Y축).</summary>
        public float RotationOffset => _rotationOffset;

        /// <summary>포메이션 앵커 위치 오프셋 (로컬 공간).</summary>
        public Vector3 PositionOffset => _positionOffset;

        /// <summary>
        /// unitCount개의 슬롯 오프셋을 offsets 배열에 채운다.
        /// 호출 측(FlockManager)이 centroid 정렬을 담당한다.
        /// offsets 배열은 미리 unitCount 크기로 할당되어 전달된다.
        /// </summary>
        public abstract void Compute(int unitCount, float spacing, Vector3[] offsets);
    }
}
