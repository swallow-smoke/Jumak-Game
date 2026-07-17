using System;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 기획서 7번 명성(목숨) 시스템. 시작 20, 최대 100, 0 이하면 게임오버.
    public sealed class ReputationManager : SinManagerBase<ReputationManager>
    {
        [SerializeField, Min(1)] private int startValue = 20;
        [SerializeField, Min(1)] private int maxValue = 100;

        public int Current { get; private set; }
        public int MaxValue => UpgradeApi.MaxReputation;
        public bool IsGameOver { get; private set; }

        public event Action<int> Changed;
        public event Action GameOver;

        public override void Initialize()
        {
            IsGameOver = false;
            UpgradeApi.EnsureInitialized(0, startValue, maxValue);
            UpgradeApi.ReputationChanged += OnApiReputationChanged;
            OnApiReputationChanged(UpgradeApi.Reputation);
        }

        protected override void OnDestroy()
        {
            UpgradeApi.ReputationChanged -= OnApiReputationChanged;
            base.OnDestroy();
        }

        public void Penalize(int amount, string reason)
        {
            Debug.Log($"[Reputation] -{Mathf.Abs(amount)} ({reason})");
            Add(-Mathf.Abs(amount));
        }

        public void Restore(int amount) => Add(Mathf.Abs(amount));

        private void Add(int delta)
        {
            if (IsGameOver)
            {
                return;
            }

            UpgradeApi.AddReputation(delta);
        }

        private void OnApiReputationChanged(int value)
        {
            Current = value;
            Changed?.Invoke(Current);

            if (Current > 0)
            {
                return;
            }

            IsGameOver = true;
            Debug.Log("[Reputation] 명성이 0이 되어 게임오버");
            GameOver?.Invoke();
        }
    }
}
