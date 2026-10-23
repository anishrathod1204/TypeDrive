using System.Collections.Generic;
using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Recycles road segments ahead of the moving car for an endless track.</summary>
    public class RoadManager : MonoBehaviour
    {
        public Transform car;
        public GameObject roadSegmentPrefab;
        public int segmentsAhead = 7;
        public float segmentLength = 30f;

        readonly Queue<GameObject> activeSegments = new Queue<GameObject>();
        float nextSpawnZ;

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
            while (activeSegments.Count > 0)
            {
                GameObject segment = activeSegments.Dequeue();
                if (segment != null) Destroy(segment);
            }
            nextSpawnZ = 0f;
            if (roadSegmentPrefab == null) return;
            for (int i = 0; i < segmentsAhead; i++) SpawnSegment();
        }

        void Update()
        {
            if (car == null || roadSegmentPrefab == null) return;

            while (nextSpawnZ - car.position.z < segmentsAhead * segmentLength)
                SpawnSegment();

            while (activeSegments.Count > 0)
            {
                GameObject oldest = activeSegments.Peek();
                if (oldest == null)
                {
                    activeSegments.Dequeue();
                    continue;
                }
                if (oldest.transform.position.z >= car.position.z - segmentLength * 2f) break;
                activeSegments.Dequeue();
                Destroy(oldest);
            }
        }

        void SpawnSegment()
        {
            GameObject segment = Instantiate(roadSegmentPrefab, new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity, transform);
            segment.name = "Road Segment";
            segment.SetActive(true);
            activeSegments.Enqueue(segment);
            nextSpawnZ += segmentLength;
        }
    }
}
