using UnityEngine;
using TypeDrive.Core;
using TypeDrive.Audio;

namespace TypeDrive.Utility
{
    /// <summary>Connects every crash to camera shake and a crash sound.</summary>
    public class CrashFeedback : MonoBehaviour
    {
        public ScreenShake screenShake;
        public AudioManager audioManager;

        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCrash += HandleCrash;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnCrash -= HandleCrash;
        }

        void HandleCrash()
        {
            screenShake?.Shake();
            audioManager?.PlayCrash();
        }
    }
}