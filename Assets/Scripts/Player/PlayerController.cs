using UnityEngine;
using UnityEngine.InputSystem;

namespace WildTamer
{
    /// <summary>
    /// Rigidbody-based player controller with camera-relative WASD movement.
    /// Requires a Rigidbody component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("References")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        private Rigidbody _rigidbody;
        private Animator _animator;
        private Vector3 _moveDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            _animator = GetComponent<Animator>();

            if (_cameraTransform == null)
                Debug.LogWarning("[PlayerController] Camera Transform is not assigned.");
        }

        private void OnEnable()
        {
            if (_moveAction != null)
                _moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (_moveAction != null)
                _moveAction.action.Disable();
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            ComputeMoveDirection();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            Vector3 velocity = _moveDirection * _moveSpeed;
            velocity.y = _rigidbody.linearVelocity.y;
            _rigidbody.linearVelocity = velocity;

            RotateTowardsMoveDirection();
        }

        private void ComputeMoveDirection()
        {
            if (_moveAction == null)
            {
                _moveDirection = Vector3.zero;
                return;
            }

            Vector2 input = _moveAction.action.ReadValue<Vector2>();
            float horizontal = input.x;
            float vertical = input.y;

            Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized;

            Vector3 rawDirection = cameraForward * vertical + cameraRight * horizontal;

            _moveDirection = rawDirection.magnitude > 0f ? rawDirection.normalized : Vector3.zero;
        }

        private void RotateTowardsMoveDirection()
        {
            if (_moveDirection == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(
                _rigidbody.rotation,
                targetRotation,
                _rotationSpeed * Time.fixedDeltaTime
            );
            _rigidbody.MoveRotation(next);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            _animator.SetFloat(SpeedHash, _moveDirection.magnitude);
        }
    }
}
