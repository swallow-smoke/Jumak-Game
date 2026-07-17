using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 상점 구매 규칙을 한 곳에 모은다. 카탈로그(정의)와 RunState(구매 기록)를 잇는다.
    // UI는 이 클래스에만 의존하고 RunState/카탈로그를 직접 뒤지지 않는다.
    public enum PurchaseResult
    {
        Success,
        Unknown,          // 카탈로그에 없는 id
        SoldOut,          // 최대 구매 횟수 도달
        LockedByPrereq,   // 선행 업그레이드 미구매
        ReputationFull,   // 명성이 이미 최대치
        NotEnoughMoney
    }

    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private UpgradeCatalog catalog;

        public UpgradeCatalog Catalog => catalog;
        private RunState State => RunState.Instance;

        // 다음 구매 가격. 매진이면 -1.
        public int GetPrice(UpgradeId id)
        {
            if (catalog == null || !catalog.TryGet(id, out UpgradeDefinition def))
            {
                return -1;
            }

            int level = State != null ? State.GetLevel(id) : 0;
            return level >= def.MaxPurchases ? -1 : def.PriceAt(level);
        }

        public int GetLevel(UpgradeId id) => State != null ? State.GetLevel(id) : 0;

        public bool IsSoldOut(UpgradeId id)
        {
            if (catalog != null && catalog.TryGet(id, out UpgradeDefinition def) && State != null)
            {
                return State.GetLevel(id) >= def.MaxPurchases;
            }

            return true;
        }

        // 실제로 사지 않고 살 수 있는지만 본다. UI가 버튼 활성화 판단에 쓴다.
        public PurchaseResult CanPurchase(UpgradeId id)
        {
            if (catalog == null || State == null || !catalog.TryGet(id, out UpgradeDefinition def))
            {
                return PurchaseResult.Unknown;
            }

            if (State.GetLevel(id) >= def.MaxPurchases)
            {
                return PurchaseResult.SoldOut;
            }

            // 단계형은 이전 단계를 사야 열린다. (이동속도 2단계는 1단계 구매 후)
            if (def.HasPrerequisite && State.GetLevel(def.Prerequisite) <= 0)
            {
                return PurchaseResult.LockedByPrereq;
            }

            if (id == UpgradeId.ReputationHeal && State.Reputation >= State.MaxReputation)
            {
                return PurchaseResult.ReputationFull;
            }

            if (!State.CanAfford(def.PriceAt(State.GetLevel(id))))
            {
                return PurchaseResult.NotEnoughMoney;
            }

            return PurchaseResult.Success;
        }

        // 조건을 모두 만족하면 전을 차감하고 구매를 기록한다.
        // 전 차감과 기록 사이에 실패 지점이 없어야 하므로 CanPurchase로 먼저 다 검사한다.
        public PurchaseResult TryPurchase(UpgradeId id)
        {
            PurchaseResult result = CanPurchase(id);
            if (result != PurchaseResult.Success)
            {
                return result;
            }

            catalog.TryGet(id, out UpgradeDefinition def);
            int price = def.PriceAt(State.GetLevel(id));

            if (!State.TrySpend(price))
            {
                // CanPurchase를 통과했으면 여기 올 일이 없지만 방어적으로 둔다.
                return PurchaseResult.NotEnoughMoney;
            }

            int level = State.IncrementLevel(id);

            if (id == UpgradeId.ReputationHeal)
            {
                State.AddReputation(10);
            }

            Debug.Log($"[Shop] {def.DisplayName} 구매 (Lv.{level}, -{price}전, 잔액 {State.Money}전)");
            return PurchaseResult.Success;
        }
    }
}
