using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // Upgrade 씬의 첫 화면만 담당한다.
    // 상단 상태 표시와 세 카테고리 진입 버튼까지만 제공하며,
    // 세부 목록/구매/라운드 전환은 이 컴포넌트의 책임이 아니다.
    [ExecuteAlways]
    public sealed class UpgradeShopEntryView : MonoBehaviour
    {
        [Header("Top Status")]
        [SerializeField] private Text moneyText;
        [SerializeField] private Text reputationText;

        [Header("Typography")]
        [SerializeField] private Text[] sceneTexts;

        [Header("Edit Mode Preview")]
        [SerializeField, Min(0)] private int previewMoney;
        [SerializeField, Min(0)] private int previewReputation = 20;
        [SerializeField, Min(1)] private int previewMaxReputation = 100;

        private static Font koreanFont;
        private RunState runState;

        private void OnEnable()
        {
            ApplyKoreanFont();

            if (!Application.isPlaying)
            {
                RefreshPreview();
                return;
            }

            runState = RunState.Instance;

            if (runState != null)
            {
                runState.MoneyChanged += OnMoneyChanged;
                runState.ReputationChanged += OnReputationChanged;
            }

            OnMoneyChanged(runState != null ? runState.Money : previewMoney);
            OnReputationChanged(runState != null ? runState.Reputation : previewReputation);
        }

        private void OnDisable()
        {
            if (runState != null)
            {
                runState.MoneyChanged -= OnMoneyChanged;
                runState.ReputationChanged -= OnReputationChanged;
            }

            runState = null;
        }

        private void OnValidate()
        {
            ApplyKoreanFont();
            RefreshPreview();
        }

        public void EnterCommonCategory() => LogCategory("공용");
        public void EnterHallCategory() => LogCategory("홀");
        public void EnterKitchenCategory() => LogCategory("주방");

        private void LogCategory(string category)
        {
            Debug.Log($"[UpgradeShop] {category} 카테고리 진입 선택. 세부 화면은 아직 구현하지 않았습니다.", this);
        }

        private void RefreshPreview()
        {
            OnMoneyChanged(previewMoney);
            OnReputationChanged(previewReputation);
        }

        private void OnMoneyChanged(int value)
        {
            if (moneyText != null)
            {
                moneyText.text = $"전  {value}";
            }
        }

        private void OnReputationChanged(int value)
        {
            if (reputationText == null)
            {
                return;
            }

            int max = runState != null ? runState.MaxReputation : previewMaxReputation;
            reputationText.text = $"명성  {value} / {max}";
        }

        private void ApplyKoreanFont()
        {
            koreanFont ??= Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial Unicode MS" },
                48);

            if (koreanFont == null)
            {
                koreanFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (sceneTexts == null)
            {
                return;
            }

            foreach (Text text in sceneTexts)
            {
                if (text != null)
                {
                    text.font = koreanFont;
                }
            }
        }
    }
}
