using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Tracks typed-character accuracy, active-time WPM, and the best combo of this run.</summary>
    public sealed class TypingStatsTracker : MonoBehaviour
    {
        public TypingController typingController;
        public int CorrectCharacters { get; private set; }
        public int AttemptedCharacters { get; private set; }
        public int BestCombo { get; private set; }
        public float ActiveSeconds { get; private set; }
        public int WordsPerMinute => ActiveSeconds <= 0.01f ? 0 : Mathf.RoundToInt((CorrectCharacters / 5f) / (ActiveSeconds / 60f));
        public float AccuracyPercent => AttemptedCharacters == 0 ? 100f : CorrectCharacters * 100f / AttemptedCharacters;

        bool subscribed;

        void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += ResetStats;
                GameManager.Instance.OnScoreChanged += UpdateBestCombo;
            }
            Subscribe();
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= ResetStats;
                GameManager.Instance.OnScoreChanged -= UpdateBestCombo;
            }
            Unsubscribe();
        }

        public void Bind(TypingController source)
        {
            Unsubscribe();
            typingController = source;
            Subscribe();
        }

        void Subscribe()
        {
            if (!isActiveAndEnabled || subscribed || typingController == null) return;
            typingController.OnCharacterAttempt += RecordAttempt;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (subscribed && typingController != null) typingController.OnCharacterAttempt -= RecordAttempt;
            subscribed = false;
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.State.Playing)
                ActiveSeconds += Time.unscaledDeltaTime;
        }

        void RecordAttempt(bool correct)
        {
            AttemptedCharacters++;
            if (correct) CorrectCharacters++;
        }

        void UpdateBestCombo()
        {
            if (GameManager.Instance != null) BestCombo = Mathf.Max(BestCombo, GameManager.Instance.combo);
        }

        void ResetStats()
        {
            CorrectCharacters = 0;
            AttemptedCharacters = 0;
            BestCombo = 0;
            ActiveSeconds = 0f;
        }
    }
}
