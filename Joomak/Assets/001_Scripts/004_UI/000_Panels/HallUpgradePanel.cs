using _001_Scripts._001_Manager;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // 홀 업그레이드 화면만 담당한다. 공용/주방 카테고리 로직은 포함하지 않는다.
    public sealed class HallUpgradePanel : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject categoryEntryPage;
        [SerializeField] private GameObject hallPage;

        [Header("Purchase Service")]
        [SerializeField] private ShopController shop;

        [Header("Patience")]
        [SerializeField] private Button patienceButton;
        [SerializeField] private Text patienceStateText;
        [SerializeField] private Text patiencePriceText;

        [Header("Iron Broom")]
        [SerializeField] private Button broomButton;
        [SerializeField] private Text broomStateText;
        [SerializeField] private Text broomPriceText;

        [Header("Table")]
        [SerializeField] private Button tableButton;
        [SerializeField] private Text tableStateText;
        [SerializeField] private Text tablePriceText;

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
            hallPage.SetActive(true);
            SetFeedback("적색 홀 강화는 접객과 홀 운영에 적용됩니다.");
            Refresh();
        }

        public void Back()
        {
            hallPage.SetActive(false);
            categoryEntryPage.SetActive(true);
        }

        public void BuyPatience()
        {
            UpgradeId? next = GetNextPatienceUpgrade();
            if (next.HasValue)
            {
                Purchase(next.Value, "손님 인내심이 10초 증가했습니다.");
            }
        }

        public void BuyIronBroom() => Purchase(UpgradeId.IronBroom, "철제 손잡이 빗자루를 장착했습니다.");
        public void BuyTable() => Purchase(UpgradeId.TableAdd, "다음 라운드에 테이블 1개가 추가됩니다.");

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

            RefreshPatience();
            RefreshBroom();
            RefreshTable();
        }

        private void RefreshPatience()
        {
            int level = PatienceLevel();
            patienceStateText.text = $"현재 {level} / 3단계 · 인내심 +{level * 10}초";

            UpgradeId? next = GetNextPatienceUpgrade();
            if (!next.HasValue)
            {
                patiencePriceText.text = "최대 단계";
                patienceButton.interactable = false;
                return;
            }

            patiencePriceText.text = $"{level + 1}단계 · {PriceLabel(next.Value)}";
            patienceButton.interactable = shop.CanPurchase(next.Value) == PurchaseResult.Success;
        }

        private void RefreshBroom()
        {
            bool owned = shop.GetLevel(UpgradeId.IronBroom) > 0;
            broomStateText.text = owned ? "장착 완료 · 손놈 3회 / 청소 2회" : "손놈 5→3회 · 청소 3→2회";
            broomPriceText.text = owned ? "구매 완료" : PriceLabel(UpgradeId.IronBroom);
            broomButton.interactable = !owned && shop.CanPurchase(UpgradeId.IronBroom) == PurchaseResult.Success;
        }

        private void RefreshTable()
        {
            int purchases = shop.GetLevel(UpgradeId.TableAdd);
            tableStateText.text = $"현재 테이블 {2 + purchases} / 6개 · 구매 {purchases} / 4회";

            if (purchases >= 4)
            {
                tablePriceText.text = "최대 개수";
                tableButton.interactable = false;
                return;
            }

            tablePriceText.text = $"테이블 +1 · {PriceLabel(UpgradeId.TableAdd)}";
            tableButton.interactable = shop.CanPurchase(UpgradeId.TableAdd) == PurchaseResult.Success;
        }

        private string PriceLabel(UpgradeId id)
        {
            int price = shop.GetPrice(id);
            return price < 0 ? "구매 완료" : $"{price}전";
        }

        private int PatienceLevel()
        {
            return shop.GetLevel(UpgradeId.Patience1)
                   + shop.GetLevel(UpgradeId.Patience2)
                   + shop.GetLevel(UpgradeId.Patience3);
        }

        private UpgradeId? GetNextPatienceUpgrade()
        {
            if (shop.GetLevel(UpgradeId.Patience1) <= 0) return UpgradeId.Patience1;
            if (shop.GetLevel(UpgradeId.Patience2) <= 0) return UpgradeId.Patience2;
            if (shop.GetLevel(UpgradeId.Patience3) <= 0) return UpgradeId.Patience3;
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
