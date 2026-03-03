# Decisions — Camera

- _basePosition tracks unshaken position so Lerp never reads shake-displaced value — prevents cumulative drift
- CameraShake uses Update-loop timer with linear magnitude decay — no coroutine per CLAUDE.md rules
- CameraShake accumulates via Mathf.Max — consecutive hits don't reset shake to zero
- Shake offset uses transform.right + transform.up (camera-local) not world XY — correct for 60deg pitch
- LateUpdate used (not Update) — camera moves after player Transform is updated
