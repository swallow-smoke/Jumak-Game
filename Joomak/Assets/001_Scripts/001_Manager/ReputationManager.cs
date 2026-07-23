using System;
using _001_Scripts._004_UI.Components;
using _001_Scripts._005_Data.Upgrade;
using _001_Scripts._005_Data.Config;
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
            GameBalance.EnsureLoaded();
            ReputationBalance balance = GameBalance.Current.reputation;
            startValue = Mathf.Max(1, balance.startingValue);
            maxValue = Mathf.Max(startValue, balance.maximumValue);
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
            if (TutorialOverlay.IsRunning)
            {
                Debug.Log($"[Reputation] 튜토리얼 보호로 명성 감소 무시 ({reason})");
                return;
            }

            int penalty = Mathf.Abs(amount);
            Debug.Log($"[Reputation] -{penalty} ({reason})");
            string detail = string.IsNullOrWhiteSpace(reason) ? "명성이 감소했습니다." : reason;
            NotificationModal.Show($"명성 -{penalty}\n{detail}", NotificationKind.Warning, 4f);
            Add(-penalty);
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

            int endingThreshold = GameBalance.Current.reputation.deathEndingThreshold;
            if (Current > endingThreshold)
            {
                return;
            }

            IsGameOver = true;
            Debug.Log("[Reputation] 명성이 0이 되어 게임오버");
            NotificationModal.Show("명성이 모두 떨어졌습니다.\n영업을 더 이상 계속할 수 없습니다.", NotificationKind.Error, 6f);
            GameOver?.Invoke();
            ReputationDeathEnding.Show();
        }
    }
}
