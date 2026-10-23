using System.Collections.Generic;
using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Scatters roadside props along both sides and recycles them during a run.</summary>
    public class PropSpawner : MonoBehaviour
    {
        public Transform car;
        public GameObject[] propPrefabs;
        public float spacing = 14f;
        public float sideOffset = 8.5f;
        public int propsAhead = 12;

        readonly List<GameObject> spawned = new List<GameObject>();
        float nextZ;

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

        void ResetForRun()
        {
            foreach (GameObject prop in spawned)
                if (prop != null) Destroy(prop);
            spawned.Clear();
            nextZ = 0f;
            if (propPrefabs == null || propPrefabs.Length == 0) return;
            for (int i = 0; i < propsAhead; i++) SpawnPair();
        }

        void Update()
        {
            if (car == null || propPrefabs == null || propPrefabs.Length == 0) return;

            while (nextZ - car.position.z < propsAhead * spacing)
                SpawnPair();

            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] == null)
                {
                    spawned.RemoveAt(i);
                    continue;
                }
                if (spawned[i].transform.position.z < car.position.z - spacing * 3f)
                {
                    Destroy(spawned[i]);
                    spawned.RemoveAt(i);
                }
            }
        }

        void SpawnPair()
        {
            SpawnOne(-sideOffset);
            SpawnOne(sideOffset);
            nextZ += spacing;
        }

        void SpawnOne(float x)
        {
            GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
            GameObject obj = Instantiate(prefab, new Vector3(x, 0f, nextZ), Quaternion.identity, transform);
            obj.SetActive(true);
            spawned.Add(obj);
        }
    }
}
