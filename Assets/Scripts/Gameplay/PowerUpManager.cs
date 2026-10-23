using System;
using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Owns temporary run effects and safely composes them with pause/restart.</summary>
    public sealed class PowerUpManager : MonoBehaviour
    {
        public float slowMotionDuration = 5f;
        public float doubleScoreDuration = 8f;
        public float slowMotionScale = 0.55f;
        public float doubleScoreMultiplier = 2f;

        public bool HasShield { get; private set; }
        public float SlowMotionRemaining { get; private set; }
        public float DoubleScoreRemaining { get; private set; }
        public event Action OnEffectsChanged;
        public event Action<PowerUpPickup.Type> OnCollected;

        void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += ResetEffects;
                GameManager.Instance.OnCrashProtectionRequested += TryAbsorbCrash;
            }
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= ResetEffects;
                GameManager.Instance.OnCrashProtectionRequested -= TryAbsorbCrash;
                GameManager.Instance.SetGameplayTimeScale(1f);
                GameManager.Instance.SetScoreMultiplier(1f);
            }
        }

        public void ResetEffects()
        {
            HasShield = false;
            SlowMotionRemaining = 0f;
            DoubleScoreRemaining = 0f;
            ApplyEffects();
        }

        public void Collect(PowerUpPickup.Type type)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            switch (type)
            {
                case PowerUpPickup.Type.Shield: HasShield = true; break;
                case PowerUpPickup.Type.SlowMotion: SlowMotionRemaining = slowMotionDuration; break;
                case PowerUpPickup.Type.DoubleScore: DoubleScoreRemaining = doubleScoreDuration; break;
            }
            ApplyEffects();
            OnCollected?.Invoke(type);
        }

        bool TryAbsorbCrash()
        {
            if (!HasShield) return false;
            HasShield = false;
            ApplyEffects();
            return true;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            float dt = Time.unscaledDeltaTime;
            bool changed = false;
            if (SlowMotionRemaining > 0f)
            {
                SlowMotionRemaining = Mathf.Max(0f, SlowMotionRemaining - dt);
                changed = true;
            }
            if (DoubleScoreRemaining > 0f)
            {
                DoubleScoreRemaining = Mathf.Max(0f, DoubleScoreRemaining - dt);
                changed = true;
            }
            if (changed) ApplyEffects();
        }

        void ApplyEffects()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameplayTimeScale(SlowMotionRemaining > 0f ? slowMotionScale : 1f);
                GameManager.Instance.SetScoreMultiplier(DoubleScoreRemaining > 0f ? doubleScoreMultiplier : 1f);
            }
            OnEffectsChanged?.Invoke();
        }
    }
}
