using System;
using UnityEngine;

namespace TypeDrive.Core
{
    /// <summary>Small PlayerPrefs-backed settings service shared by the menu and runtime.</summary>
    public sealed class GameSettings : MonoBehaviour
    {
        const string VolumeKey = "TypeDrive_MasterVolume";
        const string SfxVolumeKey = "TypeDrive_SfxVolume";
        const string MuteKey = "TypeDrive_Muted";
        const string HintsKey = "TypeDrive_ShowHints";

        public static GameSettings Instance { get; private set; }
        public float MasterVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.85f;
        public bool IsMuted { get; private set; }
        public bool ShowHints { get; private set; } = true;
        public event Action OnChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.85f);
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            ShowHints = PlayerPrefs.GetInt(HintsKey, 1) != 0;
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            Apply();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            Apply();
        }

        public void SetMuted(bool value)
        {
            IsMuted = value;
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            Apply();
        }

        public void ToggleMuted() => SetMuted(!IsMuted);

        public void SetShowHints(bool value)
        {
            ShowHints = value;
            PlayerPrefs.SetInt(HintsKey, ShowHints ? 1 : 0);
            OnChanged?.Invoke();
            PlayerPrefs.Save();
        }

        void Apply()
        {
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
            OnChanged?.Invoke();
            PlayerPrefs.Save();
        }
    }
}
