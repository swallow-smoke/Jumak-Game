using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    // 기획서 8번 촛불 관리: 꺼진 촛불에 다가가 상호작용하면 다시 켜진다.
    // IBroomTarget이 아니므로 맨손 전용이다. (빗자루를 들고 있으면 InteractionRules가 막는다)
    public sealed class Candle : BaseStructure
    {
        private const int TimeoutPenalty = 5;

        [SerializeField] private SpriteRenderer flame;
        [SerializeField] private Color litColor = new(1f, 0.85f, 0.35f);
        [SerializeField] private Color unlitColor = new(0.28f, 0.26f, 0.30f);

        private float remainingSeconds;

        public bool IsLit { get; private set; } = true;

        protected override void Awake()
        {
            base.Awake();
            ApplyColor();
        }

        private void Update()
        {
            if (IsLit)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds > 0f)
            {
                return;
            }

            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Penalize(TimeoutPenalty, $"{name} 촛불이 꺼진 채 방치됨");
            }

            // 페널티를 물린 뒤 스스로 다시 켠다. 꺼진 채로 두면 같은 촛불로 계속 깎인다.
            Relight();
        }

        public override void Interact(GameObject interactor)
        {
            if (!IsLit)
            {
                Relight();
            }
        }

        public void Extinguish(float timeLimitSeconds)
        {
            if (!IsLit)
            {
                return;
            }

            IsLit = false;
            remainingSeconds = timeLimitSeconds;
            ApplyColor();
        }

        private void Relight()
        {
            IsLit = true;
            remainingSeconds = 0f;
            ApplyColor();
        }

        private void ApplyColor()
        {
            if (flame != null)
            {
                flame.color = IsLit ? litColor : unlitColor;
            }
        }
    }
}
