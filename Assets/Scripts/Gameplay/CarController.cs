using UnityEngine;
using TypeDrive.Core;
using System;

namespace TypeDrive.Gameplay
{
    /// <summary>Moves forward automatically and changes between the three road lanes.</summary>
    public class CarController : MonoBehaviour
    {
        [Serializable]
        public struct CarPreset
        {
            public string displayName;
            public Color bodyColor;
            [Range(0.7f, 1.5f)] public float handling;
            [Range(0f, 1f)] public float speedBonus;
        }

        public static readonly CarPreset[] Presets =
        {
            new CarPreset { displayName = "VOLT", bodyColor = new Color(0.04f, 0.7f, 0.82f), handling = 1.18f, speedBonus = 0f },
            new CarPreset { displayName = "PULSE", bodyColor = new Color(0.96f, 0.18f, 0.48f), handling = 0.94f, speedBonus = 0.8f },
            new CarPreset { displayName = "NOVA", bodyColor = new Color(0.48f, 0.35f, 1f), handling = 0.82f, speedBonus = 1.4f }
        };
        const string CarPrefKey = "TypeDrive_CarPreset";
        static int selectedPreset;
        static bool presetLoaded;
        public static int SelectedPresetIndex { get { EnsurePresetLoaded(); return selectedPreset; } }
        public static CarPreset SelectedPreset { get { EnsurePresetLoaded(); return Presets[Mathf.Clamp(selectedPreset, 0, Presets.Length - 1)]; } }

        static void EnsurePresetLoaded()
        {
            if (presetLoaded) return;
            selectedPreset = Mathf.Clamp(PlayerPrefs.GetInt(CarPrefKey, 0), 0, Presets.Length - 1);
            presetLoaded = true;
        }

        public static void SelectPreset(int index)
        {
            EnsurePresetLoaded();
            selectedPreset = (index % Presets.Length + Presets.Length) % Presets.Length;
            PlayerPrefs.SetInt(CarPrefKey, selectedPreset);
            PlayerPrefs.Save();
        }

        [Header("Lanes")]
        public float laneWidth = 3f;
        public float laneChangeSpeed = 10f;
        int currentLane = 1;

        [Header("Crash reaction")]
        public float crashSpinDegrees = 360f;
        public float crashDuration = 0.6f;
        bool isCrashing;
        float crashTimer;

        Rigidbody rb;
        Vector3 startPosition;
        Quaternion startRotation;
        Vector3 targetLocalPos;
        Renderer bodyRenderer;
        Material bodyMaterial;
        float baseLaneChangeSpeed;

        void Start()
        {
            EnsurePresetLoaded();
            bodyRenderer = transform.Find("Sculpted lower body")?.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyMaterial = bodyRenderer.material;
                ApplyPreset();
            }
        }

        public void ApplyPreset()
        {
            if (bodyRenderer == null) bodyRenderer = transform.Find("Sculpted lower body")?.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                if (bodyMaterial == null) bodyMaterial = bodyRenderer.material;
                bodyMaterial.color = SelectedPreset.bodyColor;
                if (bodyMaterial.HasProperty("_EmissionColor"))
                {
                    bodyMaterial.EnableKeyword("_EMISSION");
                    bodyMaterial.SetColor("_EmissionColor", SelectedPreset.bodyColor * 0.4f);
                }
            }
            laneChangeSpeed = baseLaneChangeSpeed * SelectedPreset.handling;
        }

        public void RequestLaneChange(int direction)
        {
            currentLane = Mathf.Clamp(currentLane + Math.Sign(direction), 0, 2);
            targetLocalPos.x = (currentLane - 1) * laneWidth;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            baseLaneChangeSpeed = laneChangeSpeed;
            bodyRenderer = transform.Find("Sculpted lower body")?.GetComponent<Renderer>();
            if (rb != null)
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRunStarted += ResetCar;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRunStarted -= ResetCar;
        }

        void ResetCar()
        {
            isCrashing = false;
            crashTimer = 0f;
            currentLane = 1;
            targetLocalPos = Vector3.zero;
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = startPosition;
                rb.rotation = startRotation;
            }
            else
            {
                transform.SetPositionAndRotation(startPosition, startRotation);
            }
            Physics.SyncTransforms();

        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            if (!isCrashing)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    RequestLaneChange(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    RequestLaneChange(1);

                targetLocalPos.x = (currentLane - 1) * laneWidth;
            }

            if (isCrashing) UpdateCrashSpin();
        }

        void FixedUpdate()
        {
            if (rb == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            float forwardSpeed = GameManager.Instance.currentSpeed;
            Vector3 newPos = rb.position + Vector3.forward * forwardSpeed * Time.fixedDeltaTime;
            newPos.x = Mathf.Lerp(rb.position.x, targetLocalPos.x, laneChangeSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }

        public void TriggerCrashReaction()
        {
            if (isCrashing) return;
            isCrashing = true;
            crashTimer = 0f;
        }

        void UpdateCrashSpin()
        {
            crashTimer += Time.deltaTime;
            float t = crashTimer / Mathf.Max(0.01f, crashDuration);
            transform.Rotate(Vector3.up, (crashSpinDegrees / Mathf.Max(0.01f, crashDuration)) * Time.deltaTime, Space.World);
            if (t >= 1f)
            {
                isCrashing = false;
                transform.rotation = startRotation;
            }
        }
    }
}

