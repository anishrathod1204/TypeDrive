using UnityEngine;
using TMPro;
using TypeDrive.Core;
using TypeDrive.Gameplay;

namespace TypeDrive.UI
{
    /// <summary>
    /// Drives the in-game HUD: the word being typed (with per-letter colour
    /// for typed / remaining / mistake), score, combo, lives and speed.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        public TypingController typingController;

        [Header("HUD elements")]
        public TMP_Text wordDisplay;
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text speedText;
        public TMP_Text livesText;

        [Header("Colours")]
        public Color typedColor = new Color(0.35f, 0.85f, 0.4f);
        public Color remainingColor = Color.white;

        float typoFlashTimer;
        GameManager subscribedManager;
        TypingController subscribedTyping;

        void OnEnable() => Subscribe();

        void Subscribe()
        {
            if (!isActiveAndEnabled) return;
            if (subscribedTyping != typingController)
            {
                if (subscribedTyping != null)
                {
                    subscribedTyping.OnProgressChanged -= RefreshWordDisplay;
                    subscribedTyping.OnTypo -= FlashTypo;
                }
                subscribedTyping = typingController;
                if (subscribedTyping != null)
                {
                    subscribedTyping.OnProgressChanged += RefreshWordDisplay;
                    subscribedTyping.OnTypo += FlashTypo;
                }
            }
            if (subscribedManager != GameManager.Instance)
            {
                if (subscribedManager != null)
                {
                    subscribedManager.OnScoreChanged -= RefreshScore;
                    subscribedManager.OnLivesChanged -= RefreshLives;
                }
                subscribedManager = GameManager.Instance;
                if (subscribedManager != null)
                {
                    subscribedManager.OnScoreChanged += RefreshScore;
                    subscribedManager.OnLivesChanged += RefreshLives;
                }
            }
        }

        void OnDisable()
        {
            if (subscribedTyping != null)
            {
                subscribedTyping.OnProgressChanged -= RefreshWordDisplay;
                subscribedTyping.OnTypo -= FlashTypo;
                subscribedTyping = null;
            }
            if (subscribedManager != null)
            {
                subscribedManager.OnScoreChanged -= RefreshScore;
                subscribedManager.OnLivesChanged -= RefreshLives;
                subscribedManager = null;
            }
        }

        void Start()
        {
            Subscribe();
            RefreshScore();
            RefreshLives();
        }

        void Update()
        {
            Subscribe();
            if (speedText != null && GameManager.Instance != null)
                speedText.text = $"{GameManager.Instance.currentSpeed:0} m/s";
            if (typoFlashTimer > 0f)
            {
                typoFlashTimer -= Time.unscaledDeltaTime;
                if (typoFlashTimer <= 0f) RefreshWordDisplay(typingController);
            }
        }

        void RefreshWordDisplay(TypingController tc)
        {
            if (wordDisplay == null || tc == null) return;
            wordDisplay.color = Color.white; // clear any typo-flash tint
            if (string.IsNullOrEmpty(tc.FullWord)) { wordDisplay.text = ""; return; }

            string typedPart = tc.FullWord.Substring(0, tc.TypedCount);
            string remainingPart = tc.FullWord.Substring(tc.TypedCount);

            wordDisplay.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(typedColor)}>{typedPart}</color>" +
                $"<color=#{ColorUtility.ToHtmlStringRGB(remainingColor)}>{remainingPart}</color>";
        }

        void FlashTypo()
        {
            typoFlashTimer = 0.12f;
            if (wordDisplay != null) wordDisplay.color = Color.red;
        }

        void RefreshScore()
        {
            if (GameManager.Instance == null) return;
            if (scoreText != null) scoreText.text = $"Score: {GameManager.Instance.score}";
            if (comboText != null) comboText.text = GameManager.Instance.combo > 1 ? $"Combo x{GameManager.Instance.combo}" : "";
        }

        void RefreshLives()
        {
            if (livesText != null && GameManager.Instance != null)
                livesText.text = GameManager.Instance.IsEndless ? "∞" : new string('\u2764', Mathf.Max(0, GameManager.Instance.lives)); // heart glyphs
        }
    }
}
