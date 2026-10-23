using UnityEngine;
using TypeDrive.Core;

namespace TypeDrive.Gameplay
{
    /// <summary>Reads characters one by one and advances the active word gate.</summary>
    public class TypingController : MonoBehaviour
    {
        public GateSpawner gateSpawner;
        public string FullWord { get; private set; } = "";
        public int TypedCount { get; private set; }

        public System.Action<TypingController> OnProgressChanged;
        public System.Action OnWordCompleted;
        public System.Action OnTypo;
        public System.Action<bool> OnCharacterAttempt;

        bool subscribed;

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Bind(GateSpawner source)
        {
            Unsubscribe();
            gateSpawner = source;
            Subscribe();
            if (gateSpawner != null && gateSpawner.CurrentGate != null)
                HandleNewActiveGate(gateSpawner.CurrentGate);
        }

        void Subscribe()
        {
            if (!isActiveAndEnabled || subscribed || gateSpawner == null) return;
            gateSpawner.OnNewActiveGate += HandleNewActiveGate;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (subscribed && gateSpawner != null)
                gateSpawner.OnNewActiveGate -= HandleNewActiveGate;
            subscribed = false;
        }

        void HandleNewActiveGate(WordGate gate)
        {
            FullWord = gate != null ? gate.Word : "";
            TypedCount = 0;
            OnProgressChanged?.Invoke(this);
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.State.Playing) return;
            if (string.IsNullOrEmpty(FullWord) || TypedCount >= FullWord.Length) return;

            foreach (char input in Input.inputString)
            {
                if (input == '\b')
                {
                    if (TypedCount > 0)
                    {
                        TypedCount--;
                        OnProgressChanged?.Invoke(this);
                    }
                    continue;
                }

                if (char.IsControl(input) || TypedCount >= FullWord.Length) continue;

                char expected = char.ToLowerInvariant(FullWord[TypedCount]);
                char typed = char.ToLowerInvariant(input);
                if (typed == expected)
                {
                    OnCharacterAttempt?.Invoke(true);
                    TypedCount++;
                    OnProgressChanged?.Invoke(this);
                    if (TypedCount >= FullWord.Length) CompleteWord();
                }
                else
                {
                    OnCharacterAttempt?.Invoke(false);
                    OnTypo?.Invoke();
                }
            }
        }

        void CompleteWord()
        {
            if (gateSpawner == null || gateSpawner.CurrentGate == null || gateSpawner.CurrentGate.Completed) return;
            gateSpawner.CurrentGate.MarkCompleted();
            if (gateSpawner.CurrentGate.IsBoss)
                GameManager.Instance.RegisterBossSuccess(FullWord.Length);
            else
                GameManager.Instance.RegisterWordSuccess(FullWord.Length);
            OnWordCompleted?.Invoke();
        }

        public void ResetForBossRetry()
        {
            TypedCount = 0;
            OnProgressChanged?.Invoke(this);
        }
    }
}

