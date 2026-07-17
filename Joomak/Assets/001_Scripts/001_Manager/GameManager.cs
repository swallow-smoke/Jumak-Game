using System;
using _001_Scripts._005_Data.GameData;
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
            Debug.Log("GameManager Initialized");
        }

        public int GetMoney() => gameData.Money;

        public bool CanAfford(int cost) => gameData.CanAfford(cost);

        public void UpdateMoney(int delta, string reason)
        {
            if (delta == 0)
            {
                return;
            }

            int current = gameData.AddMoney(delta);
            Debug.Log($"[Money] {(delta > 0 ? "+" : "")}{delta}전 ({reason}) -> {current}전");
            MoneyChanged?.Invoke(current);
        }

        // 상점용. 잔액이 모자라면 아무것도 차감하지 않고 false.
        public bool TrySpendMoney(int cost, string reason)
        {
            if (!gameData.CanAfford(cost))
            {
                return false;
            }

            UpdateMoney(-cost, reason);
            return true;
        }
    }
}
