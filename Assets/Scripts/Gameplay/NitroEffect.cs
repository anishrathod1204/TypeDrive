using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>
    /// Turns on a particle system / trail renderer once the player's combo
    /// crosses a threshold, so a good typing streak visibly looks faster.
    /// Attach to a child object with exhaust/trail particles under the car.
    /// </summary>
    public class NitroEffect : MonoBehaviour
    {
        public ParticleSystem nitroParticles;
        public TrailRenderer[] speedTrails;
        public int comboThreshold = 5;

        void Update()
        {
            if (GameManager.Instance == null) return;
            bool shouldShow = GameManager.Instance.CurrentState == GameManager.State.Playing
                               && GameManager.Instance.combo >= comboThreshold;

            if (shouldShow && nitroParticles != null && !nitroParticles.isPlaying)
                nitroParticles.Play();
            else if (!shouldShow && nitroParticles != null && nitroParticles.isPlaying)
                nitroParticles.Stop();

            if (speedTrails != null)
                foreach (var trail in speedTrails)
                    if (trail != null) trail.emitting = shouldShow;
        }
    }
}
