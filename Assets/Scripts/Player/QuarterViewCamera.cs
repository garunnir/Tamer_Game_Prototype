using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Fixed quarter-view camera that smoothly follows the player.
    /// Attach to the Main Camera GameObject and assign the Player Transform.
    /// </summary>
    public class QuarterViewCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _player;

        [Header("Follow Settings")]
        [SerializeField] private float _distance = 15.6f;
        [SerializeField] private float _smoothSpeed = 5f;

        [Header("Rotation")]
        [SerializeField] private float _pitchAngle = 60f;
        [SerializeField] private float _yawAngle = 45f;

        private Vector3 _basePosition;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeMagnitude;

        private void Start()
        {
            ApplyRotation();
            _basePosition = transform.position;

            if (_player == null)
            {
                Debug.LogWarning("[QuarterViewCamera] Player Transform is not assigned.");
                return;
            }

            _basePosition = GetTargetPosition();
            transform.position = _basePosition;
        }

        private void LateUpdate()
        {
            if (_player == null) return;

            Vector3 target = GetTargetPosition();
            _basePosition = Vector3.Lerp(_basePosition, target, _smoothSpeed * Time.deltaTime);
            transform.position = _basePosition + CalculateShakeOffset();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        }

        /// <summary>
        /// 플레이어가 화면 정중앙에 오도록, 시선 방향 뒤쪽으로 _distance만큼 떨어진 위치를 반환.
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            Vector3 lookForward = transform.forward;
            return _player.position - lookForward * _distance;
        }

        /// <summary>
        /// Triggers a camera shake. Called by HitEffect for hit feedback.
        /// </summary>
        /// <param name="magnitude">Peak displacement in world units.</param>
        /// <param name="duration">Duration of the shake in seconds.</param>
        public void CameraShake(float magnitude, float duration)
        {
            float safeDuration = duration > 0f ? duration : 0.1f;
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
            _shakeDuration  = safeDuration;
            _shakeTimer     = Mathf.Max(_shakeTimer, safeDuration);
        }

        private Vector3 CalculateShakeOffset()
        {
            if (_shakeTimer <= 0f) return Vector3.zero;

            _shakeTimer -= Time.deltaTime;

            float ratio = Mathf.Clamp01(_shakeTimer / _shakeDuration);
            float currentMagnitude = _shakeMagnitude * ratio;

            return transform.right * (Random.Range(-1f, 1f) * currentMagnitude)
                 + transform.up    * (Random.Range(-1f, 1f) * currentMagnitude);
        }
    }
}
