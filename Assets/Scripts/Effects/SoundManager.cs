using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// Singleton audio manager. Plays one-shot clips for Attack, Hit, and Taming Success.
    /// Scene setup: add this component to any scene GameObject.
    /// Assign audio clips in the Inspector.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _attackClip;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _tamingSuccessClip;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _attackVolume      = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float _hitVolume         = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float _tamingSuccessVolume = 1.0f;

        private AudioSource _audioSource;

        // ── Unity Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        private void Start()
        {
            GlobalEvents.OnTamingSucceeded += OnTamingSucceeded;
        }

        private void OnDestroy()
        {
            GlobalEvents.OnTamingSucceeded -= OnTamingSucceeded;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void PlayAttack()
        {
            Play(_attackClip, _attackVolume);
        }

        public void PlayHit()
        {
            Play(_hitClip, _hitVolume);
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void OnTamingSucceeded(ITameable _)
        {
            Play(_tamingSuccessClip, _tamingSuccessVolume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, volume);
        }
    }
}
