using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TypeDrive.UI
{
    /// <summary>Counts final typing results smoothly into three legacy UI.Text fields.</summary>
    public sealed class AnimatedRunStats : MonoBehaviour
    {
        public Text wpmText;
        public Text accuracyText;
        public Text bestComboText;
        Coroutine countUpRoutine;

        public void Show(int wpm, float accuracy, int bestCombo)
        {
            if (countUpRoutine != null) StopCoroutine(countUpRoutine);
            countUpRoutine = StartCoroutine(CountUp(wpm, accuracy, bestCombo));
        }

        IEnumerator CountUp(int wpm, float accuracy, int combo)
        {
            const float duration = 0.9f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (wpmText != null) wpmText.text = $"{Mathf.RoundToInt(wpm * p)} WPM";
                if (accuracyText != null) accuracyText.text = $"{accuracy * p:0}% ACCURACY";
                if (bestComboText != null) bestComboText.text = $"BEST COMBO  x{Mathf.RoundToInt(combo * p)}";
                yield return null;
            }
            if (wpmText != null) wpmText.text = $"{wpm} WPM";
            if (accuracyText != null) accuracyText.text = $"{accuracy:0}% ACCURACY";
            if (bestComboText != null) bestComboText.text = $"BEST COMBO  x{combo}";
            countUpRoutine = null;
        }
    }
}
