using _001_Scripts._001_Manager;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // 공용 업그레이드 화면만 담당한다. 다른 카테고리 화면은 이 클래스에서 만들지 않는다.
    public sealed class CommonUpgradePanel : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject categoryEntryPage;
        [SerializeField] private GameObject commonPage;

        [Header("Purchase Service")]
        [SerializeField] private ShopController shop;

        [Header("Dash")]
        [SerializeField] private Button dashButton;
        [SerializeField] private Text dashStateText;
        [SerializeField] private Text dashPriceText;

        [Header("Move Speed")]
        [SerializeField] private Button moveSpeedButton;
        [SerializeField] private Text moveSpeedStateText;
        [SerializeField] private Text moveSpeedPriceText;

        [Header("Reputation")]
        [SerializeField] private Button reputationButton;
        [SerializeField] private Text reputationStateText;
        [SerializeField] private Text reputationPriceText;

        [Header("Feedback")]
        [SerializeField] private Text feedbackText;

        private RunState state;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            state = RunState.Instance;
            if (state != null)
            {
                state.MoneyChanged += OnMoneyChanged;
                state.ReputationChanged += OnReputationChanged;
                state.Purchased += OnPurchased;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (state != null)
            {
                state.MoneyChanged -= OnMoneyChanged;
                state.ReputationChanged -= OnReputationChanged;
                state.Purchased -= OnPurchased;
            }

            state = null;
        }

        public void Open()
        {
            categoryEntryPage.SetActive(false);
            commonPage.SetActive(true);
            SetFeedback("황색 공용 강화는 두 플레이어에게 함께 적용됩니다.");
            Refresh();
        }

        public void Back()
        {
            commonPage.SetActive(false);
            categoryEntryPage.SetActive(true);
        }

        public void BuyDash() => Purchase(UpgradeId.Dash, "대쉬를 습득했습니다.");

        public void BuyMoveSpeed()
        {
            UpgradeId? next = GetNextMoveSpeedUpgrade();
            if (next.HasValue)
            {
                Purchase(next.Value, "이동속도가 10% 증가했습니다.");
            }
        }

        public void BuyReputation() => Purchase(UpgradeId.ReputationHeal, "명성을 10 회복했습니다.");

        private void Purchase(UpgradeId id, string successMessage)
        {
            if (shop == null)
            {
                SetFeedback("상점 데이터를 불러오지 못했습니다.");
                return;
            }

            PurchaseResult result = shop.TryPurchase(id);
            SetFeedback(result == PurchaseResult.Success ? successMessage : ResultMessage(result));
            Refresh();
        }

        private void Refresh()
        {
            if (!Application.isPlaying || state == null || shop == null)
            {
                return;
            }

            RefreshDash();
            RefreshMoveSpeed();
            RefreshReputation();
        }

        private void RefreshDash()
        {
            bool owned = shop.GetLevel(UpgradeId.Dash) > 0;
            dashStateText.text = owned ? "습득 완료 · 쿨타임 5초" : "P1 왼쪽 Shift · P2 오른쪽 Shift";
            dashPriceText.text = owned ? "구매 완료" : PriceLabel(UpgradeId.Dash);
            dashButton.interactable = !owned && shop.CanPurchase(UpgradeId.Dash) == PurchaseResult.Success;
        }

        private void RefreshMoveSpeed()
        {
            int level = MoveSpeedLevel();
            moveSpeedStateText.text = $"현재 {level} / 3단계 · 이동속도 +{level * 10}%";

            UpgradeId? next = GetNextMoveSpeedUpgrade();
            if (!next.HasValue)
            {
                moveSpeedPriceText.text = "최대 단계";
                moveSpeedButton.interactable = false;
                return;
            }

            moveSpeedPriceText.text = $"{level + 1}단계 · {PriceLabel(next.Value)}";
            moveSpeedButton.interactable = shop.CanPurchase(next.Value) == PurchaseResult.Success;
        }

        private void RefreshReputation()
        {
            reputationStateText.text = $"현재 명성 {state.Reputation} / {state.MaxReputation}";
            PurchaseResult result = shop.CanPurchase(UpgradeId.ReputationHeal);
            reputationPriceText.text = result == PurchaseResult.ReputationFull ? "명성 최대" : PriceLabel(UpgradeId.ReputationHeal);
            reputationButton.interactable = result == PurchaseResult.Success;
        }

        private string PriceLabel(UpgradeId id)
        {
            int price = shop.GetPrice(id);
            return price < 0 ? "구매 완료" : $"{price}전";
        }

        private int MoveSpeedLevel()
        {
            return shop.GetLevel(UpgradeId.MoveSpeed1)
                   + shop.GetLevel(UpgradeId.MoveSpeed2)
                   + shop.GetLevel(UpgradeId.MoveSpeed3);
        }

        private UpgradeId? GetNextMoveSpeedUpgrade()
        {
            if (shop.GetLevel(UpgradeId.MoveSpeed1) <= 0) return UpgradeId.MoveSpeed1;
            if (shop.GetLevel(UpgradeId.MoveSpeed2) <= 0) return UpgradeId.MoveSpeed2;
            if (shop.GetLevel(UpgradeId.MoveSpeed3) <= 0) return UpgradeId.MoveSpeed3;
            return null;
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        private static string ResultMessage(PurchaseResult result)
        {
            return result switch
            {
                PurchaseResult.NotEnoughMoney => "전이 부족합니다.",
                PurchaseResult.LockedByPrereq => "이전 단계를 먼저 구매해야 합니다.",
                PurchaseResult.ReputationFull => "명성이 이미 최대입니다.",
                PurchaseResult.SoldOut => "이미 모두 구매했습니다.",
                _ => "구매할 수 없습니다."
            };
        }

        private void OnMoneyChanged(int _) => Refresh();
        private void OnReputationChanged(int _) => Refresh();
        private void OnPurchased(UpgradeId _, int __) => Refresh();
    }
}
