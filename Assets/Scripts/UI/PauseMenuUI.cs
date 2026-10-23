using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.UI
{
    /// <summary>Displays the pause panel; Escape is routed centrally by the scene bootstrap.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject panel;
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
            if (panel != null) panel.SetActive(state == GameManager.State.Paused);
        }

        /// <summary>Hooked up to the Resume button's OnClick in the Inspector.</summary>
        public void OnResumePressed() { if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.State.Playing); }
    }
}
