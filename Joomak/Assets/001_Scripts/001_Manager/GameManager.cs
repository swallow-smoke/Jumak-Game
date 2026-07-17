using System;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 게임플레이 씬에서 전을 다루는 창구.
    // 실제 값은 RunState(ScriptableObject)에 있어 씬을 전환해도 유지된다.
    // GameManager는 그 값을 게임플레이 코드와 HUD에 이어주는 얇은 프록시다.
    public class GameManager : SinManagerBase<GameManager>
    {
        public event Action<int> MoneyChanged;

        private RunState State => RunState.Instance;

        public override void Initialize()
        {
            if (State != null)
            {
                // HUD가 GameManager.MoneyChanged만 구독하면 되도록 RunState의 변경을 다시 쏴준다.
                State.MoneyChanged += OnStateMoneyChanged;
            }
        }

        protected override void OnDestroy()
        {
            if (State != null)
            {
                State.MoneyChanged -= OnStateMoneyChanged;
            }

            base.OnDestroy();
        }

        public int GetMoney() => State != null ? State.Money : 0;

        public bool CanAfford(int cost) => State != null && State.CanAfford(cost);

        public void UpdateMoney(int delta, string reason)
        {
            if (delta == 0 || State == null)
            {
                return;
            }

            int current = State.AddMoney(delta);
            Debug.Log($"[Money] {(delta > 0 ? "+" : "")}{delta}전 ({reason}) -> {current}전");
        }

        // 잔액이 모자라면 아무것도 차감하지 않고 false.
        public bool TrySpendMoney(int cost, string reason)
        {
            if (State == null || !State.CanAfford(cost))
            {
                return false;
            }

            UpdateMoney(-cost, reason);
            return true;
        }

        private void OnStateMoneyChanged(int current) => MoneyChanged?.Invoke(current);
    }
}
