using UnityEngine;

namespace TypeDrive.Core
{
    /// <summary>Saves and loads the best score / distance between sessions.</summary>
    public class HighScoreManager : MonoBehaviour
    {
        public static HighScoreManager Instance { get; private set; }

        const string KeyBestScore = "TypeDrive_BestScore";
        const string KeyBestDistance = "TypeDrive_BestDistance";

        public int BestScore { get; private set; }
        public float BestDistance { get; private set; }
        public bool LastRunWasNewBest { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BestScore = PlayerPrefs.GetInt(KeyBestScore, 0);
            BestDistance = PlayerPrefs.GetFloat(KeyBestDistance, 0f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ReportRun(int score, float distance)
        {
            LastRunWasNewBest = score > BestScore;
            if (LastRunWasNewBest)
            {
                BestScore = score;
                PlayerPrefs.SetInt(KeyBestScore, BestScore);
            }
            if (distance > BestDistance)
            {
                BestDistance = distance;
                PlayerPrefs.SetFloat(KeyBestDistance, BestDistance);
            }
            PlayerPrefs.Save();
        }
    }
}
