using System;
using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data.Upgrade
{
    // 한 판(여러 라운드에 걸친 플레이)의 지속 상태: 전 + 명성 + 업그레이드 구매 횟수.
    //
    // ScriptableObject라 씬을 전환해도(게임플레이 <-> 상점) 값이 유지된다.
    // 씬 싱글톤(GameManager)은 씬 전환 시 파괴되어 전이 리셋되므로 그걸 못 쓴다.
    //
    // 값들은 [NonSerialized]라 에디터에서 플레이를 멈추면(도메인 리로드) 리셋되고,
    // 에셋 파일에 눌러쓰이지 않는다. 즉 새 플레이 = 새 판이 자동으로 보장된다.
    [CreateAssetMenu(menuName = "Joomak/Upgrade/Run State", fileName = "RunState")]
    public sealed class RunState : ScriptableObject
    {
        private const string ResourcePath = "RunState";

        [Tooltip("판 시작 시 전. 보통 0에서 시작해 서빙으로 번다.")]
        [SerializeField, Min(0)] private int startingMoney;

        [Header("Reputation")]
        [SerializeField, Min(0)] private int startingReputation = 20;
        [SerializeField, Min(1)] private int maxReputation = 100;

        [NonSerialized] private bool sessionReady;
        [NonSerialized] private int money;
        [NonSerialized] private int reputation;
        [NonSerialized] private readonly Dictionary<UpgradeId, int> levels = new();

        public event Action<int> MoneyChanged;
        public event Action<int> ReputationChanged;
        public event Action<UpgradeId, int> Purchased;

        private static RunState instance;

        // 씬 참조 없이 어디서든 접근. Resources에서 한 번 읽어 캐시한다.
        public static RunState Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = Resources.Load<RunState>(ResourcePath);
                if (instance == null)
                {
                    Debug.LogError($"[RunState] Resources/{ResourcePath}.asset 을 찾을 수 없습니다. 생성기를 실행하세요.");
                    return null;
                }

                // 씬에서 아무도 참조하지 않는 순간 언로드되지 않도록 붙잡아 둔다.
                instance.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return instance;
            }
        }

        public int Money
        {
            get
            {
                EnsureSession();
                return money;
            }
        }

        public int Reputation
        {
            get
            {
                EnsureSession();
                return reputation;
            }
        }

        public int MaxReputation => maxReputation;

        // 판이 처음 시작될 때 한 번만 초기화. 씬 전환으로는 불리지 않는다.
        private void EnsureSession()
        {
            if (sessionReady)
            {
                return;
            }

            sessionReady = true;
            money = startingMoney;
            reputation = Mathf.Clamp(startingReputation, 0, maxReputation);
            levels.Clear();
        }

        // 새 판을 강제로 시작한다. (타이틀 -> 게임 시작 등)
        public void ResetRun()
        {
            sessionReady = false;
            EnsureSession();
            MoneyChanged?.Invoke(money);
            ReputationChanged?.Invoke(reputation);
        }

        public int AddMoney(int delta)
        {
            EnsureSession();
            money = Mathf.Max(0, money + delta);
            MoneyChanged?.Invoke(money);
            return money;
        }

        public bool CanAfford(int cost)
        {
            EnsureSession();
            return cost >= 0 && money >= cost;
        }

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            AddMoney(-cost);
            return true;
        }

        public int AddReputation(int delta)
        {
            EnsureSession();
            reputation = Mathf.Clamp(reputation + delta, 0, maxReputation);
            ReputationChanged?.Invoke(reputation);
            return reputation;
        }

        public int GetLevel(UpgradeId id)
        {
            EnsureSession();
            return levels.GetValueOrDefault(id, 0);
        }

        // 구매 기록만 올린다. 전 차감과 조건 검사는 ShopController가 담당한다.
        public int IncrementLevel(UpgradeId id)
        {
            EnsureSession();
            int next = levels.GetValueOrDefault(id, 0) + 1;
            levels[id] = next;
            Purchased?.Invoke(id, next);
            return next;
        }
    }
}
