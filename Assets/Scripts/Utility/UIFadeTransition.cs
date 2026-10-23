using System.Collections;
using UnityEngine;

namespace TypeDrive.Utility
{
    /// <summary>Unscaled-time CanvasGroup fade that remains responsive while gameplay is paused.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIFadeTransition : MonoBehaviour
    {
        public float duration = 0.22f;
        CanvasGroup group;
        Coroutine routine;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetVisible(bool visible)
        {
            if (visible && !gameObject.activeSelf) gameObject.SetActive(true);
            if (!visible && !gameObject.activeSelf) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Fade(visible));
        }

        IEnumerator Fade(bool visible)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (visible) gameObject.SetActive(true);
            float from = group.alpha;
            float to = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : elapsed / duration);
                yield return null;
            }
            group.alpha = to;
            if (!visible) gameObject.SetActive(false);
            routine = null;
        }
    }
}
