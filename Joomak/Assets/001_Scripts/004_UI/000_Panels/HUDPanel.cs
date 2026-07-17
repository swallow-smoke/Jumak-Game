using _001_Scripts._001_Manager;
using _001_Scripts._001_Manager.Interface;
using _001_Scripts._005_Data.Hall;
using TMPro;
using UnityEngine;

namespace _001_Scripts._004_UI._000_Panels
{
    // 영업 중 항상 떠 있는 HUD. 명성/전/주문 현황을 보여준다.
    // 홀 내부를 직접 뒤지지 않고 IHallService로만 읽는다.
    public sealed class HUDPanel : PanelBase
    {
        [SerializeField] private TMP_Text reputationText;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text orderText;

        private IHallService hall;

        protected override void Start()
        {
            base.Start();

            hall = HallManager.Instance;

            // 값이 바뀔 때만 갱신한다. 매 프레임 문자열을 만들면 GC가 계속 돈다.
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Changed += OnReputationChanged;
                OnReputationChanged(ReputationManager.Instance.Current);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MoneyChanged += OnMoneyChanged;
                OnMoneyChanged(GameManager.Instance.GetMoney());
            }

            RefreshOrders();
        }

        protected override void OnDestroy()
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Changed -= OnReputationChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MoneyChanged -= OnMoneyChanged;
            }

            base.OnDestroy();
        }

        // 주문은 바뀔 때 알려주는 이벤트가 없어서 아직은 폴링한다.
        // 잦은 갱신이 아니라 매 프레임은 낭비이므로 0.25초마다만 본다.
        private float refreshTimer;

        private void Update()
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer < 0.25f)
            {
                return;
            }

            refreshTimer = 0f;
            RefreshOrders();
        }

        private void RefreshOrders()
        {
            if (orderText == null)
            {
                return;
            }

            hall ??= HallManager.Instance;
            if (hall == null)
            {
                return;
            }

            int cooking = hall.GetOrderCount(HallOrderStatus.Placed);
            int ready = hall.GetOrderCount(HallOrderStatus.Ready);
            orderText.text = $"조리중 {cooking}  /  완성 {ready}";
        }

        private void OnReputationChanged(int value)
        {
            if (reputationText != null)
            {
                int max = ReputationManager.Instance != null ? ReputationManager.Instance.MaxValue : 100;
                reputationText.text = $"명성 {value} / {max}";
            }
        }

        private void OnMoneyChanged(int value)
        {
            if (moneyText != null)
            {
                moneyText.text = $"{value}전";
            }
        }
    }
}
