using UnityEngine;
using TypeDrive.Core;
using TypeDrive.Gameplay;

namespace TypeDrive.Audio
{
    /// <summary>Plays a generated engine loop and short feedback sounds for typing and crashes.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        [Header("Engine")]
        public AudioSource engineSource;
        public float minPitch = 0.85f;
        public float maxPitch = 1.6f;

        [Header("One shots")]
        public AudioSource sfxSource;
        public AudioClip keystrokeClip;
        public AudioClip wordCompleteClip;
        public AudioClip typoClip;
        public AudioClip crashClip;
        public AudioClip uiClickClip;
        public AudioClip powerUpClip;
        public AudioClip levelCompleteClip;
        public TypingController typingController;
        [Range(0f, 1f)] public float effectsVolume = 0.85f;

        bool subscribed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Configure(AudioSource engine, AudioSource sfx, TypingController typing,
                              AudioClip engineLoop, AudioClip key, AudioClip success,
                              AudioClip typo, AudioClip crash, AudioClip click)
        {
            Unsubscribe();
            engineSource = engine;
            sfxSource = sfx;
            typingController = typing;
            keystrokeClip = key;
            wordCompleteClip = success;
            typoClip = typo;
            crashClip = crash;
            uiClickClip = click;

            if (engineSource != null)
            {
                engineSource.clip = engineLoop;
                engineSource.loop = true;
                engineSource.volume = 0.15f;
                if (!engineSource.isPlaying) engineSource.Play();
            }
            Subscribe();
        }

        void Subscribe()
        {
            if (!isActiveAndEnabled || subscribed || typingController == null) return;
            typingController.OnProgressChanged += HandleProgress;
            typingController.OnWordCompleted += HandleWordCompleted;
            typingController.OnTypo += HandleTypo;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            if (typingController != null)
            {
                typingController.OnProgressChanged -= HandleProgress;
                typingController.OnWordCompleted -= HandleWordCompleted;
                typingController.OnTypo -= HandleTypo;
            }
            subscribed = false;
        }

        void HandleProgress(TypingController controller) => PlayOneShot(keystrokeClip, 0.26f);
        void HandleWordCompleted() => PlayOneShot(wordCompleteClip, 0.65f);
        void HandleTypo() => PlayOneShot(typoClip, 0.55f);

        void Update()
        {
            if (engineSource == null || GameManager.Instance == null) return;
            bool running = GameManager.Instance.CurrentState == GameManager.State.Playing;
            if (running && !engineSource.isPlaying) engineSource.UnPause();
            if (!running && engineSource.isPlaying) engineSource.Pause();

            float t = Mathf.InverseLerp(GameManager.Instance.RunStartingSpeed, GameManager.Instance.maxSpeed,
                                         GameManager.Instance.currentSpeed);
            engineSource.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        }

        public void PlayCrash() => PlayOneShot(crashClip, 0.8f);
        public void PlayUIClick() => PlayOneShot(uiClickClip, 0.35f);
        public void PlayPowerUp() => PlayOneShot(powerUpClip, 0.7f);
        public void PlayLevelComplete() => PlayOneShot(levelCompleteClip, 0.85f);
        public void SetEffectsVolume(float value) => effectsVolume = Mathf.Clamp01(value);

        void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, volume * effectsVolume);
        }
    }
}
