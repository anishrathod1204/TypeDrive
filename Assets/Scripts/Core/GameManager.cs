using System;
using UnityEngine;
using TypeDrive.Gameplay;

namespace TypeDrive.Core
{
    /// <summary>Owns run mode, campaign progress, score, lives, speed, and distance.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum State { MainMenu, Playing, Paused, GameOver, LevelComplete }
        public enum RunMode { Campaign, Endless }

        public State CurrentState { get; private set; } = State.MainMenu;
        public RunMode CurrentMode { get; private set; } = RunMode.Campaign;
        public int SelectedLevelIndex { get; private set; }
        public bool IsEndless => CurrentMode == RunMode.Endless;
        public float RunStartingSpeed => (IsEndless ? baseSpeed : SelectedLevel.StartingSpeed) + CarController.SelectedPreset.speedBonus;
        public GameLevelDefinition SelectedLevel => GameLevelCatalog.Get(SelectedLevelIndex);
        public int WordsCompleted { get; private set; }
        public int WordGoal => IsEndless ? 0 : SelectedLevel.WordGoal;
        public float ScoreMultiplier { get; private set; } = 1f;
        public float GameplayTimeScale { get; private set; } = 1f;

        [Header("Endless speed")]
        [Tooltip("Starting forward speed in metres per second for endless drive.")]
        public float baseSpeed = 8f;
        public float currentSpeed;
        public float maxSpeed = 28f;
        public float speedPerWord = 0.6f;
        public float crashSpeedPenalty = 4f;

        [Header("Score and lives")]
        public int score;
        public int combo;
        public int lives = 3;
        public float distanceTravelled;

        [Header("Endless difficulty thresholds")]
        public int mediumWordsScore = 150;
        public int hardWordsScore = 400;

        public event Action<State> OnStateChanged;
        public event Action OnRunStarted;
        public event Action OnScoreChanged;
        public event Action OnLivesChanged;
        public event Action OnCrash;
        public event Action OnShieldAbsorbedCrash;
        public event Func<bool> OnCrashProtectionRequested;
        public event Action OnBossCompleted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        public void StartRun()
        {
            if (IsEndless) StartEndless();
            else StartLevel(SelectedLevelIndex);
        }

        /// <summary>Restarts the current mode and level without returning to the menu.</summary>
        public void RestartRun() => BeginRun();

        public void StartLevel(int index)
        {
            CurrentMode = RunMode.Campaign;
            SelectedLevelIndex = Mathf.Clamp(index, 0, GameLevelCatalog.Count - 1);
            BeginRun();
        }

        public void StartEndless()
        {
            CurrentMode = RunMode.Endless;
            BeginRun();
        }

        void BeginRun()
        {
            Time.timeScale = 1f;
            score = 0;
            combo = 0;
            lives = 3;
            WordsCompleted = 0;
            distanceTravelled = 0f;
            ScoreMultiplier = 1f;
            GameplayTimeScale = 1f;
            currentSpeed = RunStartingSpeed;

            // Return to a live state before reset listeners rebuild the car,
            // road, gates, and traffic for this run.
            SetState(State.Playing);
            OnRunStarted?.Invoke();
            OnScoreChanged?.Invoke();
            OnLivesChanged?.Invoke();
        }

        public void SetState(State next)
        {
            State previous = CurrentState;
            if (next == State.GameOver || next == State.LevelComplete ||
                (next == State.MainMenu && (previous == State.Playing || previous == State.Paused)))
                SaveCurrentRun();

            CurrentState = next;
            Time.timeScale = next == State.Playing ? GameplayTimeScale :
                (next == State.Paused || next == State.GameOver || next == State.LevelComplete ? 0f : 1f);
            OnStateChanged?.Invoke(next);
        }

        public void SetScoreMultiplier(float value)
        {
            ScoreMultiplier = Mathf.Max(1f, value);
        }

        public void SetGameplayTimeScale(float value)
        {
            GameplayTimeScale = Mathf.Clamp(value, 0.15f, 1f);
            if (CurrentState == State.Playing) Time.timeScale = GameplayTimeScale;
        }

        void SaveCurrentRun()
        {
            if (HighScoreManager.Instance != null)
                HighScoreManager.Instance.ReportRun(score, distanceTravelled);
        }

        public void RegisterWordSuccess(int wordLength)
        {
            if (CurrentState != State.Playing) return;

            WordsCompleted++;
            combo++;
            int comboBonus = Mathf.Clamp(combo / 3, 0, 5);
            score += Mathf.RoundToInt(((10 * wordLength) + comboBonus * 5) * ScoreMultiplier);
            currentSpeed = Mathf.Min(maxSpeed, currentSpeed + speedPerWord * (1f + combo * 0.05f));
            OnScoreChanged?.Invoke();

        }

        public void RegisterBossSuccess(int sentenceLength)
        {
            if (CurrentState != State.Playing || IsEndless) return;
            combo++;
            score += Mathf.RoundToInt((sentenceLength * 15 + 100) * ScoreMultiplier);
            OnScoreChanged?.Invoke();
            OnBossCompleted?.Invoke();
            SetState(State.LevelComplete);
        }

        public bool RegisterCrash()
        {
            if (CurrentState != State.Playing) return false;

            if (OnCrashProtectionRequested != null && OnCrashProtectionRequested.Invoke())
            {
                OnShieldAbsorbedCrash?.Invoke();
                return false;
            }

            combo = 0;
            if (!IsEndless)
                lives = Mathf.Max(0, lives - 1);
            currentSpeed = Mathf.Max(RunStartingSpeed * 0.6f,
                currentSpeed - crashSpeedPenalty);
            OnCrash?.Invoke();
            OnLivesChanged?.Invoke();
            OnScoreChanged?.Invoke();

            if (!IsEndless && lives <= 0) SetState(State.GameOver);
            return true;
        }

        public WordBank.Difficulty CurrentDifficulty()
        {
            if (!IsEndless) return SelectedLevel.Difficulty;
            if (score >= hardWordsScore) return WordBank.Difficulty.Hard;
            if (score >= mediumWordsScore) return WordBank.Difficulty.Medium;
            return WordBank.Difficulty.Easy;
        }

        void Update()
        {
            if (CurrentState == State.Playing)
                distanceTravelled += currentSpeed * Time.deltaTime;
        }
    }
}
