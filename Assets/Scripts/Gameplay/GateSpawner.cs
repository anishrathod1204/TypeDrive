using System.Collections.Generic;
using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Maintains the upcoming word gates and exposes the nearest unfinished gate.</summary>
    public class GateSpawner : MonoBehaviour
    {
        public Transform car;
        public GameObject gatePrefab;
        public float gateSpacing = 24f;
        public int gatesAhead = 5;

        readonly List<WordGate> gates = new List<WordGate>();
        float nextGateZ = 25f;
        int spawnedNormalGates;
        bool bossSpawned;

        static readonly string[] BossSentences =
        {
            "NEON ROADS LEAD TO NEW ADVENTURES",
            "SPEED AND FOCUS WILL WIN THIS NIGHT RACE",
            "PRECISION TYPING POWERS EVERY FAST ESCAPE",
            "STAY SHARP WHEN THE CITY LIGHTS BEGIN TO FLICKER",
            "THE FASTEST DRIVER TYPES EACH WORD WITH PERFECT CONTROL"
        };
        static readonly float[] BossTimers = { 32f, 35f, 38f, 42f, 46f };

        public WordGate CurrentGate { get; private set; }
        public System.Action<WordGate> OnNewActiveGate;

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

        void Start()
        {
            if (gates.Count == 0)
            {
                nextGateZ = GetFirstGatePosition();
                FillAhead();
            }
            AdvanceActiveGate();
        }

        void Update()
        {
            if (car == null || gatePrefab == null || GameManager.Instance == null) return;

            while (!bossSpawned && nextGateZ - car.position.z < gatesAhead * GetSpawnSpacing())
                SpawnGate();

            if (CurrentGate == null || CurrentGate.Completed)
                AdvanceActiveGate();

            CleanUpPassedGates();
        }

        public void ResetForRun()
        {
            foreach (WordGate gate in gates)
                if (gate != null) Destroy(gate.gameObject);

            gates.Clear();
            CurrentGate = null;
            spawnedNormalGates = 0;
            bossSpawned = false;
            nextGateZ = GetFirstGatePosition();
            FillAhead();
            AdvanceActiveGate();
        }

        void FillAhead()
        {
            for (int i = 0; i < gatesAhead; i++) SpawnGate();
        }

        void SpawnGate()
        {
            if (car == null || gatePrefab == null || GameManager.Instance == null) return;

            GameObject obj = Instantiate(gatePrefab, new Vector3(0f, 0f, nextGateZ), Quaternion.identity, transform);
            obj.SetActive(true);
            WordGate gate = obj.GetComponent<WordGate>();
            if (gate == null)
            {
                Destroy(obj);
                nextGateZ += GetSpawnSpacing();
                return;
            }

            if (!GameManager.Instance.IsEndless && spawnedNormalGates >= GameManager.Instance.WordGoal)
            {
                int level = Mathf.Clamp(GameManager.Instance.SelectedLevelIndex, 0, BossSentences.Length - 1);
                gate.Setup(BossSentences[level], car, true, BossTimers[level]);
                bossSpawned = true;
            }
            else
            {
                string word = WordBank.GetRandomWord(GameManager.Instance.CurrentDifficulty());
                gate.Setup(word, car);
                spawnedNormalGates++;
            }
            gates.Add(gate);
            float spacing = GetSpawnSpacing();
            if (gate.IsBoss)
                spacing = Mathf.Max(spacing, GameManager.Instance.currentSpeed * (gate.BossTimeLimit + 8f));
            nextGateZ += spacing;

            if (CurrentGate == null) AdvanceActiveGate();
        }

        float GetSpawnSpacing()
        {
            if (GameManager.Instance == null) return gateSpacing;
            float speed = GameManager.Instance.currentSpeed > 0f
                ? GameManager.Instance.currentSpeed
                : GameManager.Instance.RunStartingSpeed;
            float secondsToNextGate = GameManager.Instance.IsEndless
                ? 3.2f
                : GameManager.Instance.SelectedLevel.GateSeconds;
            return Mathf.Max(gateSpacing, speed * secondsToNextGate);
        }

        public string GetBossTimerText()
        {
            if (CurrentGate == null || !CurrentGate.IsBoss) return "";
            return $"STORM GATE  •  {Mathf.CeilToInt(CurrentGate.BossTimeRemaining):00}s";
        }

        float GetFirstGatePosition()
        {
            float carZ = car != null ? car.position.z : 0f;
            return carZ + GetSpawnSpacing();
        }

        void AdvanceActiveGate()
        {
            foreach (WordGate gate in gates)
            {
                if (gate == null || gate.Completed) continue;
                if (CurrentGate != gate)
                {
                    CurrentGate = gate;
                    OnNewActiveGate?.Invoke(CurrentGate);
                }
                return;
            }
            CurrentGate = null;
        }

        void CleanUpPassedGates()
        {
            for (int i = gates.Count - 1; i >= 0; i--)
            {
                WordGate gate = gates[i];
                if (gate == null)
                {
                    gates.RemoveAt(i);
                    continue;
                }

                bool wellBehind = gate.transform.position.z < car.position.z - gateSpacing * 2f;
                if (wellBehind && gate != CurrentGate)
                {
                    Destroy(gate.gameObject);
                    gates.RemoveAt(i);
                }
            }
        }
    }
}

