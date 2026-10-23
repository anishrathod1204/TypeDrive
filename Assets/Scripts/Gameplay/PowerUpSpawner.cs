using System.Collections.Generic;
using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Recycles pickups by distance and restores a deterministic start after restart.</summary>
    public sealed class PowerUpSpawner : MonoBehaviour
    {
        public Transform car;
        public PowerUpManager manager;
        public GameObject pickupPrefab;
        public float firstPickupDistance = 34f;
        public float pickupSpacing = 64f;
        public float laneWidth = 3f;
        readonly List<GameObject> active = new List<GameObject>();
        float nextZ;

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnRunStarted += ResetForRun;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnRunStarted -= ResetForRun;
        }

        void Start() => ResetForRun();

        public void ResetForRun()
        {
            for (int i = 0; i < active.Count; i++) if (active[i] != null) Destroy(active[i]);
            active.Clear();
            nextZ = (car != null ? car.position.z : 0f) + firstPickupDistance;
            FillAhead();
        }

        void Update()
        {
            if (car == null || pickupPrefab == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            while (nextZ < car.position.z + pickupSpacing * 3f) SpawnOne();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] == null) { active.RemoveAt(i); continue; }
                if (active[i].transform.position.z < car.position.z - 8f)
                {
                    Destroy(active[i]);
                    active.RemoveAt(i);
                }
            }
        }

        void FillAhead()
        {
            if (car == null || pickupPrefab == null) return;
            for (int i = 0; i < 3; i++) SpawnOne();
        }

        void SpawnOne()
        {
            if (pickupPrefab == null) return;
            int lane = Random.Range(0, 3) - 1;
            var obj = Instantiate(pickupPrefab, new Vector3(lane * laneWidth, 0.65f, nextZ), Quaternion.identity, transform);
            obj.SetActive(true);
            var pickup = obj.GetComponent<PowerUpPickup>();
            if (pickup != null)
            {
                var type = (PowerUpPickup.Type)Random.Range(0, 3);
                pickup.Configure(type, car, manager);
            }
            active.Add(obj);
            nextZ += pickupSpacing;
        }
    }
}
