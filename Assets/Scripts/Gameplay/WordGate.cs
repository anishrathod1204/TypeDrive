using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>A neon arch that marks the deadline for one typing challenge.</summary>
    public class WordGate : MonoBehaviour
    {
        public TextMesh wordLabel;
        public Transform triggerPoint;

        public string Word { get; private set; }
        public bool Completed { get; private set; }
        public bool IsBoss { get; private set; }
        public float BossTimeLimit { get; private set; }
        public float BossTimeRemaining { get; private set; }

        Transform car;
        bool passedTrigger;
        GateUnlockAnimation unlockAnimation;

        void Awake() => unlockAnimation = GetComponent<GateUnlockAnimation>();

        public void Setup(string word, Transform carTransform, bool boss = false, float bossTimeLimit = 0f)
        {
            Word = word;
            car = carTransform;
            IsBoss = boss;
            BossTimeLimit = bossTimeLimit;
            BossTimeRemaining = bossTimeLimit;
            if (wordLabel != null)
            {
                wordLabel.text = word.ToUpperInvariant();
                wordLabel.color = new Color(0.9f, 0.97f, 1f);
            }
            Completed = false;
            passedTrigger = false;
            if (IsBoss) ApplyBossStyle();
        }

        public void MarkCompleted()
        {
            Completed = true;
            if (wordLabel != null) wordLabel.color = new Color(0.25f, 1f, 0.64f);
            if (unlockAnimation != null) unlockAnimation.Play();
        }

        void Update()
        {
            if (passedTrigger || car == null || GameManager.Instance == null ||
                GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            if (IsBoss && GameManager.Instance.CurrentState == GameManager.State.Playing &&
                GetComponentInParent<GateSpawner>()?.CurrentGate == this)
            {
                BossTimeRemaining -= Time.deltaTime;
                if (BossTimeRemaining <= 0f)
                {
                    FailBossAttempt();
                    return;
                }
            }

            float gateZ = triggerPoint != null ? triggerPoint.position.z : transform.position.z;
            if (car.position.z >= gateZ)
            {
                passedTrigger = true;
                HandleArrival();
            }
        }

        void HandleArrival()
        {
            if (Completed) return;

            if (IsBoss)
            {
                FailBossAttempt();
                return;
            }

            MarkFailed();
            bool crashed = GameManager.Instance.RegisterCrash();
            CarController controller = car.GetComponent<CarController>();
            if (crashed && controller != null) controller.TriggerCrashReaction();
        }

        public void ResetBossTimer()
        {
            BossTimeRemaining = BossTimeLimit;
            passedTrigger = false;
        }

        void FailBossAttempt()
        {
            if (Completed || GameManager.Instance == null) return;
            bool crashed = GameManager.Instance.RegisterCrash();
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            CarController carController = car.GetComponent<CarController>();
            if (crashed && carController != null) carController.TriggerCrashReaction();
            TypingController typing = FindObjectOfType<TypingController>();
            if (typing != null) typing.ResetForBossRetry();
            ResetBossTimer();
            float speed = Mathf.Max(1f, GameManager.Instance.currentSpeed);
            transform.position = new Vector3(transform.position.x, transform.position.y,
                car.position.z + speed * (BossTimeLimit + 5f));
        }

        void ApplyBossStyle()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.material;
                Color color = new Color(1f, 0.24f, 0.52f);
                material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 2f);
                }
            }
        }

        void MarkFailed()
        {
            Completed = true;
            if (wordLabel != null) wordLabel.color = new Color(1f, 0.28f, 0.35f);
        }
    }
}
