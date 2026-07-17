using _001_Scripts._001_Manager;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // 주방 업그레이드 화면과 구매 상태만 담당한다.
    public sealed class KitchenUpgradePanel : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject categoryEntryPage;
        [SerializeField] private GameObject kitchenPage;

        [Header("Purchase Service")]
        [SerializeField] private ShopController shop;

        [Header("Cook Time")]
        [SerializeField] private Button cookTimeButton;
        [SerializeField] private Text cookTimeStateText;
        [SerializeField] private Text cookTimePriceText;

        [Header("Failure Delay")]
        [SerializeField] private Button failureDelayButton;
        [SerializeField] private Text failureDelayStateText;
        [SerializeField] private Text failureDelayPriceText;

        [Header("Premium Dish")]
        [SerializeField] private Button premiumDishButton;
        [SerializeField] private Text premiumDishStateText;
        [SerializeField] private Text premiumDishPriceText;

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
                state.Purchased += OnPurchased;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (state != null)
            {
                state.MoneyChanged -= OnMoneyChanged;
                state.Purchased -= OnPurchased;
            }

            state = null;
        }

        public void Open()
        {
            categoryEntryPage.SetActive(false);
            kitchenPage.SetActive(true);
            SetFeedback("청색 주방 강화는 조리와 요리 판매에 적용됩니다.");
            Refresh();
        }

        public void Back()
        {
            kitchenPage.SetActive(false);
            categoryEntryPage.SetActive(true);
        }

        public void BuyCookTime()
        {
            UpgradeId? next = GetNextCookTimeUpgrade();
            if (next.HasValue)
            {
                Purchase(next.Value, "모든 조리시간이 10% 감소했습니다.");
            }
        }

        public void BuyFailureDelay()
        {
            UpgradeId? next = GetNextFailureDelayUpgrade();
            if (next.HasValue)
            {
                Purchase(next.Value, "요리가 실패작이 되기까지의 시간이 10% 늘었습니다.");
            }
        }

        public void BuyPremiumDish() => Purchase(UpgradeId.PremiumDish, "고급 요리가 활성화되었습니다.");

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

            RefreshCookTime();
            RefreshFailureDelay();
            RefreshPremiumDish();
        }

        private void RefreshCookTime()
        {
            int level = CookTimeLevel();
            cookTimeStateText.text = $"현재 {level} / 3단계 · 조리시간 -{level * 10}%";

            UpgradeId? next = GetNextCookTimeUpgrade();
            if (!next.HasValue)
            {
                cookTimePriceText.text = "최대 단계";
                cookTimeButton.interactable = false;
                return;
            }

            cookTimePriceText.text = $"{level + 1}단계 · {PriceLabel(next.Value)}";
            cookTimeButton.interactable = shop.CanPurchase(next.Value) == PurchaseResult.Success;
        }

        private void RefreshFailureDelay()
        {
            int level = FailureDelayLevel();
            failureDelayStateText.text = $"현재 {level} / 3단계 · 실패시간 +{level * 10}%";

            UpgradeId? next = GetNextFailureDelayUpgrade();
            if (!next.HasValue)
            {
                failureDelayPriceText.text = "최대 단계";
                failureDelayButton.interactable = false;
                return;
            }

            failureDelayPriceText.text = $"{level + 1}단계 · {PriceLabel(next.Value)}";
            failureDelayButton.interactable = shop.CanPurchase(next.Value) == PurchaseResult.Success;
        }

        private void RefreshPremiumDish()
        {
            bool owned = shop.GetLevel(UpgradeId.PremiumDish) > 0;
            premiumDishStateText.text = owned ? "적용 중 · 판매 시 명성 +1" : "요리 판매 시 명성 +1 추가";
            premiumDishPriceText.text = owned ? "구매 완료" : PriceLabel(UpgradeId.PremiumDish);
            premiumDishButton.interactable = !owned && shop.CanPurchase(UpgradeId.PremiumDish) == PurchaseResult.Success;
        }

        private string PriceLabel(UpgradeId id)
        {
            int price = shop.GetPrice(id);
            return price < 0 ? "구매 완료" : $"{price}전";
        }

        private int CookTimeLevel()
        {
            return shop.GetLevel(UpgradeId.CookTime1)
                   + shop.GetLevel(UpgradeId.CookTime2)
                   + shop.GetLevel(UpgradeId.CookTime3);
        }

        private int FailureDelayLevel()
        {
            return shop.GetLevel(UpgradeId.FailureDelay1)
                   + shop.GetLevel(UpgradeId.FailureDelay2)
                   + shop.GetLevel(UpgradeId.FailureDelay3);
        }

        private UpgradeId? GetNextCookTimeUpgrade()
        {
            if (shop.GetLevel(UpgradeId.CookTime1) <= 0) return UpgradeId.CookTime1;
            if (shop.GetLevel(UpgradeId.CookTime2) <= 0) return UpgradeId.CookTime2;
            if (shop.GetLevel(UpgradeId.CookTime3) <= 0) return UpgradeId.CookTime3;
            return null;
        }

        private UpgradeId? GetNextFailureDelayUpgrade()
        {
            if (shop.GetLevel(UpgradeId.FailureDelay1) <= 0) return UpgradeId.FailureDelay1;
            if (shop.GetLevel(UpgradeId.FailureDelay2) <= 0) return UpgradeId.FailureDelay2;
            if (shop.GetLevel(UpgradeId.FailureDelay3) <= 0) return UpgradeId.FailureDelay3;
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
                PurchaseResult.SoldOut => "이미 모두 구매했습니다.",
                _ => "구매할 수 없습니다."
            };
        }

        private void OnMoneyChanged(int _) => Refresh();
        private void OnPurchased(UpgradeId _, int __) => Refresh();
    }
}
