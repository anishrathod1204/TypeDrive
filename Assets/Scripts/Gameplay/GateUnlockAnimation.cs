using System.Collections;
using UnityEngine;

namespace TypeDrive.Gameplay
{
    /// <summary>Plays a short world-space unlock pulse when a gate is typed correctly.</summary>
    public sealed class GateUnlockAnimation : MonoBehaviour
    {
        Coroutine routine;

        public void Play()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(UnlockPulse());
        }

        IEnumerator UnlockPulse()
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < 0.48f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.48f);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                transform.localScale = originalScale * pulse;
                yield return null;
            }
            transform.localScale = originalScale;
            routine = null;
        }
    }
}
