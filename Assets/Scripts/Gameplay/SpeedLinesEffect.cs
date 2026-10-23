using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Creates lightweight built-in speed streaks whose density follows car speed.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class SpeedLinesEffect : MonoBehaviour
    {
        ParticleSystem lines;
        ParticleSystem.EmissionModule emission;
        public float maximumRate = 38f;

        void Awake()
        {
            lines = GetComponent<ParticleSystem>();
            emission = lines.emission;
        }

        void Update()
        {
            if (lines == null || GameManager.Instance == null) return;
            bool running = GameManager.Instance.CurrentState == GameManager.State.Playing;
            float speed = GameManager.Instance.currentSpeed;
            float ratio = Mathf.InverseLerp(GameManager.Instance.RunStartingSpeed, GameManager.Instance.maxSpeed, speed);
            emission.rateOverTime = running ? Mathf.Lerp(5f, maximumRate, ratio) : 0f;
        }
    }
}
