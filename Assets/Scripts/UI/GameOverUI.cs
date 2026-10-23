using UnityEngine;
using TMPro;
using TypeDrive.Core;

namespace TypeDrive.UI
{
    /// <summary>Controls the Game Over panel: final score, distance, best score and a new-best callout.</summary>
    public class GameOverUI : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text finalScoreText;
        public TMP_Text distanceText;
        public TMP_Text newBestLabel; // small "NEW BEST!" text, hidden unless true
        GameManager subscribedManager;

        void OnEnable() => Subscribe();

        void Start() => Subscribe();

        void Subscribe()
        {
            if (!isActiveAndEnabled || subscribedManager == GameManager.Instance) return;
            if (subscribedManager != null) subscribedManager.OnStateChanged -= HandleStateChanged;
            subscribedManager = GameManager.Instance;
            if (subscribedManager != null) subscribedManager.OnStateChanged += HandleStateChanged;
        }
        void OnDisable()
        {
            if (subscribedManager != null) subscribedManager.OnStateChanged -= HandleStateChanged;
            subscribedManager = null;
        }

        void HandleStateChanged(GameManager.State state)
        {
            if (panel != null) panel.SetActive(state == GameManager.State.GameOver);
            if (state != GameManager.State.GameOver) return;
            if (GameManager.Instance == null) return;

            if (finalScoreText != null) finalScoreText.text = $"Score: {GameManager.Instance.score}";
            if (distanceText != null) distanceText.text = $"Distance: {GameManager.Instance.distanceTravelled:0} m";
            if (newBestLabel != null)
                newBestLabel.gameObject.SetActive(HighScoreManager.Instance != null && HighScoreManager.Instance.LastRunWasNewBest);
        }

        /// <summary>Hooked up to the Restart button's OnClick in the Inspector.</summary>
        public void OnRestartPressed() { if (GameManager.Instance != null) GameManager.Instance.RestartRun(); }

        public void OnMainMenuPressed() { if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.State.MainMenu); }
    }
}
