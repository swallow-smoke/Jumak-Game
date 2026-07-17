using System;
using _001_Scripts._005_Data.GameData;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 게임 전역 데이터(전·인구) 보관. 상점이 라운드 사이에 이 전을 쓴다.
    public class GameManager : SinManagerBase<GameManager>
    {
        [SerializeField] private GameDatas gameData = new();

        public event Action<int> MoneyChanged;

        public override void Initialize()
        {
            UpgradeApi.EnsureInitialized(gameData.Money);
            UpgradeApi.MoneyChanged += OnApiMoneyChanged;
            Debug.Log("GameManager Initialized");
        }

        protected override void OnDestroy()
        {
            UpgradeApi.MoneyChanged -= OnApiMoneyChanged;
            base.OnDestroy();
        }

        public int GetMoney() => UpgradeApi.Money;

        public bool CanAfford(int cost) => cost >= 0 && UpgradeApi.Money >= cost;

        public void UpdateMoney(int delta, string reason)
        {
            if (delta == 0)
            {
                return;
            }

            int current = UpgradeApi.AddMoney(delta);
            Debug.Log($"[Money] {(delta > 0 ? "+" : "")}{delta}전 ({reason}) -> {current}전");
        }

        // 상점용. 잔액이 모자라면 아무것도 차감하지 않고 false.
        public bool TrySpendMoney(int cost, string reason)
        {
            if (!UpgradeApi.TrySpendMoney(cost))
            {
                return false;
            }

            Debug.Log($"[Money] -{cost}전 ({reason}) -> {UpgradeApi.Money}전");
            return true;
        }

        private void OnApiMoneyChanged(int value) => MoneyChanged?.Invoke(value);
    }
}
