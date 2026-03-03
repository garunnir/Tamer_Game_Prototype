# Decisions — Player

- Rigidbody.linearVelocity (Unity 6 API); preserves Y for gravity
- FreezeRotation constraint set in Awake — physics can't tip capsule; script controls rotation via RotateTowards
- Animator.StringToHash("Speed") cached as static readonly — no per-frame string allocation
