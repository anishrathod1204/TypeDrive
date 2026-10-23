using UnityEngine;
using TMPro;
using TypeDrive.Core;

namespace TypeDrive.UI
{
    /// <summary>Controls the main menu panel: shows the saved best score and starts a run.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text bestScoreText;
        GameManager subscribedManager;

        void OnEnable() => Subscribe();

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

        void Start()
        {
            Subscribe();
            if (bestScoreText != null && HighScoreManager.Instance != null)
                bestScoreText.text = $"Best Score: {HighScoreManager.Instance.BestScore}";
            if (panel != null && GameManager.Instance != null)
                panel.SetActive(GameManager.Instance.CurrentState == GameManager.State.MainMenu);
        }

        void HandleStateChanged(GameManager.State state)
        {
            if (panel != null) panel.SetActive(state == GameManager.State.MainMenu);
        }

        /// <summary>Hooked up to the Start button's OnClick in the Inspector.</summary>
        public void OnPlayPressed() { if (GameManager.Instance != null) GameManager.Instance.StartRun(); }

        public void OnQuitPressed() => Application.Quit();
    }
}
