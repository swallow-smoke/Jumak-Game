using System;
using _001_Scripts._001_Manager;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // Upgrade.unity의 기존 그래픽을 UpgradeApi에 연결한다.
    public sealed class UpgradeShopApiController : MonoBehaviour
    {
        private Transform categoryPage;
        private Transform commonPage;
        private Transform hallPage;
        private Transform kitchenPage;
        private Text moneyText;
        private Text reputationText;

        private void Awake()
        {
            UpgradeApi.EnsureInitialized();
            CacheUi();
            BindButtons();
            ShowEntry();
        }

        private void OnEnable()
        {
            UpgradeApi.MoneyChanged += OnMoneyChanged;
            UpgradeApi.ReputationChanged += OnReputationChanged;
            UpgradeApi.UpgradePurchased += OnUpgradePurchased;
            Refresh();
        }

        private void OnDisable()
        {
            UpgradeApi.MoneyChanged -= OnMoneyChanged;
            UpgradeApi.ReputationChanged -= OnReputationChanged;
            UpgradeApi.UpgradePurchased -= OnUpgradePurchased;
        }

        private void CacheUi()
        {
            categoryPage = transform.Find("Category Entry Page");
            commonPage = transform.Find("Common Upgrade Page");
            hallPage = transform.Find("Hall Upgrade Page");
            kitchenPage = transform.Find("Kitchen Upgrade Page");
            moneyText = FindComponent<Text>(transform, "Money Status/Value");
            reputationText = FindComponent<Text>(transform, "Reputation Status/Value");

            if (categoryPage == null || commonPage == null || hallPage == null || kitchenPage == null)
            {
                Debug.LogError("[UpgradeShopApiController] Upgrade 씬의 페이지 구조를 찾지 못했습니다.", this);
            }
        }

        private void BindButtons()
        {
            Bind(categoryPage, "Common Category", () => ShowPage(commonPage));
            Bind(categoryPage, "Hall Category", () => ShowPage(hallPage));
            Bind(categoryPage, "Kitchen Category", () => ShowPage(kitchenPage));

            Bind(commonPage, "Back Button", ShowEntry);
            Bind(commonPage, "Dash Upgrade/Purchase Button", () => Purchase(UpgradeId.Dash, commonPage));
            Bind(commonPage, "Move Speed Upgrade/Purchase Button", () => Purchase(NextMoveSpeed(), commonPage));
            Bind(commonPage, "Reputation Upgrade/Purchase Button", () => Purchase(UpgradeId.ReputationHeal, commonPage));

            Bind(hallPage, "Back Button", ShowEntry);
            Bind(hallPage, "Patience Upgrade/Purchase Button", () => Purchase(NextPatience(), hallPage));
            Bind(hallPage, "Iron Broom Upgrade/Purchase Button", () => Purchase(UpgradeId.IronBroom, hallPage));
            Bind(hallPage, "Table Upgrade/Purchase Button", () => Purchase(UpgradeId.TableAdd, hallPage));

            Bind(kitchenPage, "Back Button", ShowEntry);
            Bind(kitchenPage, "Cook Time Upgrade/Purchase Button", () => Purchase(NextCookTime(), kitchenPage));
            Bind(kitchenPage, "Failure Delay Upgrade/Purchase Button", () => Purchase(NextFailureDelay(), kitchenPage));
            Bind(kitchenPage, "Premium Dish Upgrade/Purchase Button", () => Purchase(UpgradeId.PremiumDish, kitchenPage));
        }

        private void ShowEntry()
        {
            SetActive(categoryPage, true);
            SetActive(commonPage, false);
            SetActive(hallPage, false);
            SetActive(kitchenPage, false);
            Refresh();
        }

        private void ShowPage(Transform target)
        {
            SetActive(categoryPage, false);
            SetActive(commonPage, target == commonPage);
            SetActive(hallPage, target == hallPage);
            SetActive(kitchenPage, target == kitchenPage);
            Refresh();
        }

        private void Purchase(UpgradeId? id, Transform page)
        {
            if (!id.HasValue)
            {
                SetFeedback(page, "이미 최대 단계입니다.");
                return;
            }

            UpgradePurchaseResult result = UpgradeApi.TryPurchase(id.Value);
            SetFeedback(page, result == UpgradePurchaseResult.Success
                ? $"{UpgradeApi.GetSnapshot(id.Value).Definition.DisplayName} 구매 완료"
                : ResultMessage(result));
            Refresh();
        }

        private void Refresh()
        {
            if (moneyText != null) moneyText.text = $"전  {UpgradeApi.Money}";
            if (reputationText != null) reputationText.text = $"명성  {UpgradeApi.Reputation} / {UpgradeApi.MaxReputation}";

            RefreshSingle(commonPage, "Dash Upgrade", UpgradeId.Dash,
                UpgradeApi.DashUnlocked ? "대쉬 습득 완료" : "미습득 · 쿨타임 5초");

            int moveLevel = SequentialLevel(UpgradeId.MoveSpeed1, UpgradeId.MoveSpeed2, UpgradeId.MoveSpeed3);
            RefreshSequential(commonPage, "Move Speed Upgrade", NextMoveSpeed(), moveLevel,
                $"현재 {moveLevel} / 3단계 · 이동속도 +{moveLevel * 10}%");

            RefreshSingle(commonPage, "Reputation Upgrade", UpgradeId.ReputationHeal,
                $"현재 명성 {UpgradeApi.Reputation} / {UpgradeApi.MaxReputation}");

            int patienceLevel = SequentialLevel(UpgradeId.Patience1, UpgradeId.Patience2, UpgradeId.Patience3);
            RefreshSequential(hallPage, "Patience Upgrade", NextPatience(), patienceLevel,
                $"현재 {patienceLevel} / 3단계 · 인내심 +{UpgradeApi.PatienceBonusSeconds}초");

            RefreshSingle(hallPage, "Iron Broom Upgrade", UpgradeId.IronBroom,
                UpgradeApi.IronBroomUnlocked ? "장착 완료 · 손놈 3회 / 청소 2회" : "손놈 5→3회 · 청소 3→2회");

            int tableLevel = UpgradeApi.GetLevel(UpgradeId.TableAdd);
            RefreshSingle(hallPage, "Table Upgrade", UpgradeId.TableAdd,
                $"현재 테이블 {2 + tableLevel} / 6개 · 구매 {tableLevel} / 4회");

            int cookLevel = SequentialLevel(UpgradeId.CookTime1, UpgradeId.CookTime2, UpgradeId.CookTime3);
            RefreshSequential(kitchenPage, "Cook Time Upgrade", NextCookTime(), cookLevel,
                $"현재 {cookLevel} / 3단계 · 조리시간 -{cookLevel * 10}%");

            int delayLevel = SequentialLevel(UpgradeId.FailureDelay1, UpgradeId.FailureDelay2, UpgradeId.FailureDelay3);
            RefreshSequential(kitchenPage, "Failure Delay Upgrade", NextFailureDelay(), delayLevel,
                $"현재 {delayLevel} / 3단계 · 실패시간 +{delayLevel * 10}%");

            RefreshSingle(kitchenPage, "Premium Dish Upgrade", UpgradeId.PremiumDish,
                UpgradeApi.SaleReputationBonus > 0 ? "적용 중 · 판매 시 명성 +1" : "요리 판매 시 명성 +1 추가");
        }

        private static void RefreshSingle(Transform page, string cardPath, UpgradeId id, string stateLabel)
        {
            if (page == null) return;

            Text state = FindComponent<Text>(page, $"{cardPath}/State");
            Text price = FindComponent<Text>(page, $"{cardPath}/Purchase Button/Price");
            Button button = FindComponent<Button>(page, $"{cardPath}/Purchase Button");
            UpgradePurchaseResult result = UpgradeApi.CanPurchase(id);

            if (state != null) state.text = stateLabel;
            if (price != null) price.text = PriceLabel(id, result);
            if (button != null) button.interactable = result == UpgradePurchaseResult.Success;
        }

        private static void RefreshSequential(
            Transform page,
            string cardPath,
            UpgradeId? next,
            int level,
            string stateLabel)
        {
            if (page == null) return;

            Text state = FindComponent<Text>(page, $"{cardPath}/State");
            Text price = FindComponent<Text>(page, $"{cardPath}/Purchase Button/Price");
            Button button = FindComponent<Button>(page, $"{cardPath}/Purchase Button");

            if (state != null) state.text = stateLabel;
            if (!next.HasValue)
            {
                if (price != null) price.text = "최대 단계";
                if (button != null) button.interactable = false;
                return;
            }

            UpgradePurchaseResult result = UpgradeApi.CanPurchase(next.Value);
            if (price != null) price.text = $"{level + 1}단계 · {PriceLabel(next.Value, result)}";
            if (button != null) button.interactable = result == UpgradePurchaseResult.Success;
        }

        private static string PriceLabel(UpgradeId id, UpgradePurchaseResult result)
        {
            return result switch
            {
                UpgradePurchaseResult.SoldOut => "구매 완료",
                UpgradePurchaseResult.ReputationFull => "명성 최대",
                _ => $"{UpgradeApi.GetPrice(id)}전"
            };
        }

        private static string ResultMessage(UpgradePurchaseResult result)
        {
            return result switch
            {
                UpgradePurchaseResult.NotEnoughMoney => "전이 부족합니다.",
                UpgradePurchaseResult.LockedByPrerequisite => "이전 단계를 먼저 구매해야 합니다.",
                UpgradePurchaseResult.ReputationFull => "명성이 이미 최대입니다.",
                UpgradePurchaseResult.SoldOut => "이미 모두 구매했습니다.",
                _ => "구매할 수 없습니다."
            };
        }

        private static int SequentialLevel(params UpgradeId[] ids)
        {
            int total = 0;
            foreach (UpgradeId id in ids) total += UpgradeApi.GetLevel(id);
            return total;
        }

        private static UpgradeId? NextMoveSpeed()
        {
            if (!UpgradeApi.IsPurchased(UpgradeId.MoveSpeed1)) return UpgradeId.MoveSpeed1;
            if (!UpgradeApi.IsPurchased(UpgradeId.MoveSpeed2)) return UpgradeId.MoveSpeed2;
            if (!UpgradeApi.IsPurchased(UpgradeId.MoveSpeed3)) return UpgradeId.MoveSpeed3;
            return null;
        }

        private static UpgradeId? NextPatience()
        {
            if (!UpgradeApi.IsPurchased(UpgradeId.Patience1)) return UpgradeId.Patience1;
            if (!UpgradeApi.IsPurchased(UpgradeId.Patience2)) return UpgradeId.Patience2;
            if (!UpgradeApi.IsPurchased(UpgradeId.Patience3)) return UpgradeId.Patience3;
            return null;
        }

        private static UpgradeId? NextCookTime()
        {
            if (!UpgradeApi.IsPurchased(UpgradeId.CookTime1)) return UpgradeId.CookTime1;
            if (!UpgradeApi.IsPurchased(UpgradeId.CookTime2)) return UpgradeId.CookTime2;
            if (!UpgradeApi.IsPurchased(UpgradeId.CookTime3)) return UpgradeId.CookTime3;
            return null;
        }

        private static UpgradeId? NextFailureDelay()
        {
            if (!UpgradeApi.IsPurchased(UpgradeId.FailureDelay1)) return UpgradeId.FailureDelay1;
            if (!UpgradeApi.IsPurchased(UpgradeId.FailureDelay2)) return UpgradeId.FailureDelay2;
            if (!UpgradeApi.IsPurchased(UpgradeId.FailureDelay3)) return UpgradeId.FailureDelay3;
            return null;
        }

        private static void Bind(Transform root, string path, UnityEngine.Events.UnityAction action)
        {
            Button button = FindComponent<Button>(root, path);
            if (button == null)
            {
                Debug.LogWarning($"[UpgradeShopApiController] 버튼을 찾지 못했습니다: {path}");
                return;
            }

            button.onClick.AddListener(action);
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            return root != null && root.Find(path) != null ? root.Find(path).GetComponent<T>() : null;
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null) target.gameObject.SetActive(active);
        }

        private static void SetFeedback(Transform page, string message)
        {
            Text feedback = FindComponent<Text>(page, "Feedback");
            if (feedback != null) feedback.text = message;
        }

        private void OnMoneyChanged(int _) => Refresh();
        private void OnReputationChanged(int _) => Refresh();
        private void OnUpgradePurchased(UpgradeId _, int __)
        {
            Refresh();
            DayCycleManager.SaveCurrentProgress("Upgrade");
        }
    }
}
