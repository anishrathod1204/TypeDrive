using UnityEngine;
using System.Collections;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Animated road pickup; collection is lane-aware and does not need physics setup.</summary>
    public sealed class PowerUpPickup : MonoBehaviour
    {
        public enum Type { Shield, SlowMotion, DoubleScore }

        public Type powerUpType;
        public Transform car;
        public PowerUpManager manager;
        public float pickupRadius = 2.25f;
        public Color shieldColor = new Color(0.22f, 0.95f, 1f);
        public Color slowColor = new Color(0.55f, 0.48f, 1f);
        public Color scoreColor = new Color(1f, 0.72f, 0.18f);

        Vector3 basePosition;
        bool collected;

        public void Configure(Type type, Transform player, PowerUpManager powerUpManager)
        {
            powerUpType = type;
            car = player;
            manager = powerUpManager;
            basePosition = transform.position;
            SetColor();
        }

        void Update()
        {
            if (collected) return;
            float t = Time.time;
            transform.Rotate(0f, 110f * Time.deltaTime, 0f, Space.World);
            transform.position = basePosition + Vector3.up * (0.14f + Mathf.Sin(t * 3f) * 0.14f);
            if (car == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            Vector3 delta = car.position - transform.position;
            if (Mathf.Abs(delta.z) <= pickupRadius && Mathf.Abs(delta.x) <= 2.2f && Mathf.Abs(delta.y) <= 1.5f)
            {
                collected = true;
                if (manager != null) manager.Collect(powerUpType);
                StartCoroutine(CollectPulse());
            }
        }

        IEnumerator CollectPulse()
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.2f);
                float pulse = t < 0.35f ? Mathf.Lerp(1f, 1.45f, t / 0.35f) : Mathf.Lerp(1.45f, 0f, (t - 0.35f) / 0.65f);
                transform.localScale = originalScale * pulse;
                yield return null;
            }
            Destroy(gameObject);
        }

        void SetColor()
        {
            Color color = powerUpType == Type.Shield ? shieldColor : powerUpType == Type.SlowMotion ? slowColor : scoreColor;
            foreach (Renderer visual in GetComponentsInChildren<Renderer>())
            {
                Material material = visual.material;
                material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 1.8f);
                }
            }
        }
    }
}
