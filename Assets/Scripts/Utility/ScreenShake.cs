using System.Collections;
using UnityEngine;
using TypeDrive.Gameplay;

namespace TypeDrive.Utility
{
    /// <summary>Brief camera offset used after a missed gate or traffic hit.</summary>
    public class ScreenShake : MonoBehaviour
    {
        public float duration = 0.24f;
        public float magnitude = 0.2f;

        Coroutine running;
        CameraFollow cameraFollow;
        Vector3 originalLocalPosition;

        void Awake()
        {
            cameraFollow = GetComponent<CameraFollow>();
            originalLocalPosition = transform.localPosition;
        }

        public void Shake()
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ShakeRoutine());
        }

        IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float falloff = 1f - elapsed / duration;
                Vector2 offset = Random.insideUnitCircle * magnitude * falloff;
                Vector3 shake = new Vector3(offset.x, offset.y, 0f);
                if (cameraFollow != null) cameraFollow.shakeOffset = shake;
                else transform.localPosition = originalLocalPosition + shake;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (cameraFollow != null) cameraFollow.shakeOffset = Vector3.zero;
            else transform.localPosition = originalLocalPosition;
            running = null;
        }
    }
}
