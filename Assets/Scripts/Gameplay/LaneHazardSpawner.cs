using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Spawns lane blockers with enough lead time for a quick lane change.</summary>
    public class LaneHazardSpawner : MonoBehaviour
    {
        public Transform car;
        public GameObject hazardPrefab;
        public float spacing = 40f;
        public float laneWidth = 3f;
        public float firstHazardZ = 42f;
        public int hazardsAhead = 6;
        [Tooltip("How far ahead traffic warnings begin, scaled with current speed.")]
        public float warningReactionSeconds = 1.7f;
        public float minWarningDistance = 14f;
        public float maxWarningDistance = 30f;

        readonly System.Collections.Generic.List<GameObject> hazards = new System.Collections.Generic.List<GameObject>();
        float nextZ;
        int previousLane = 1;
        string warningMessage = "";

        public string WarningMessage => warningMessage;

        void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRunStarted += ResetForRun;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRunStarted -= ResetForRun;
        }

        void Start() => ResetForRun();

        public void ResetForRun()
        {
            foreach (GameObject hazard in hazards)
                if (hazard != null) Destroy(hazard);
            hazards.Clear();

            nextZ = firstHazardZ;
            previousLane = 1;
            FillAhead();
        }

        void Update()
        {
            if (car == null || hazardPrefab == null || GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameManager.State.Playing)
                CheckCollisions();

            while (nextZ - car.position.z < hazardsAhead * GetSpawnSpacing())
                SpawnHazard();

            CleanupAndSetWarning();
        }

        void FillAhead()
        {
            if (car == null || hazardPrefab == null) return;
            for (int i = 0; i < hazardsAhead; i++) SpawnHazard();
        }

        void SpawnHazard()
        {
            int lane = Random.Range(0, 3);
            if (lane == previousLane)
                lane = (lane + Random.Range(1, 3)) % 3;
            previousLane = lane;

            float x = (lane - 1) * laneWidth;
            GameObject hazard = Instantiate(hazardPrefab, new Vector3(x, 0f, nextZ), Quaternion.identity, transform);
            hazard.name = "Traffic Barrier";
            hazard.SetActive(true);
            hazards.Add(hazard);
            nextZ += GetSpawnSpacing();
        }

        float GetSpawnSpacing()
        {
            float speed = GameManager.Instance != null && GameManager.Instance.currentSpeed > 0f
                ? GameManager.Instance.currentSpeed
                : (GameManager.Instance != null ? GameManager.Instance.RunStartingSpeed : 8f);
            float modeSpacing = GameManager.Instance != null && !GameManager.Instance.IsEndless
                ? GameManager.Instance.SelectedLevel.HazardSpacing
                : spacing;
            return Mathf.Max(modeSpacing, speed * 2.7f);
        }
        void CheckCollisions()
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                GameObject hazard = hazards[i];
                if (hazard == null) continue;
                float dz = hazard.transform.position.z - car.position.z;
                float dx = Mathf.Abs(hazard.transform.position.x - car.position.x);
                if (Mathf.Abs(dz) > 1.5f || dx > laneWidth * 0.42f) continue;

                hazards.RemoveAt(i);
                hazard.SetActive(false);
                Destroy(hazard);
                bool crashed = GameManager.Instance.RegisterCrash();
                CarController controller = car.GetComponent<CarController>();
                if (crashed && controller != null) controller.TriggerCrashReaction();
                break;
            }
        }

        void CleanupAndSetWarning()
        {
            float nearest = float.MaxValue;
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                GameObject hazard = hazards[i];
                if (hazard == null)
                {
                    hazards.RemoveAt(i);
                    continue;
                }

                float dz = hazard.transform.position.z - car.position.z;
                if (dz < -4f)
                {
                    Destroy(hazard);
                    hazards.RemoveAt(i);
                    continue;
                }
                if (dz >= 0f && dz < nearest) nearest = dz;
            }

            float speed = GameManager.Instance.currentSpeed > 0f
                ? GameManager.Instance.currentSpeed
                : GameManager.Instance.RunStartingSpeed;
            float warningDistance = Mathf.Clamp(speed * warningReactionSeconds,
                minWarningDistance, maxWarningDistance);
            warningMessage = nearest <= warningDistance
                ? "TRAFFIC AHEAD  •  USE  A / D  OR  ← / →  TO DODGE"
                : "";
        }
    }
}




