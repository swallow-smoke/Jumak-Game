using System;
using UnityEngine;

namespace _001_Scripts._005_Data.Upgrade
{
    // 업그레이드 한 종류의 정의. UpgradeCatalog가 리스트로 들고 있어 인스펙터에서 값을 조정할 수 있다.
    //
    // 가격 모델 하나로 세 가지를 다 표현한다:
    //   - 1회성/단계형 : maxPurchases=1, priceMultiplier=1  (대쉬, 이동속도N단계, 철제빗자루)
    //   - 반복 고정가   : maxPurchases 큰 값, priceMultiplier=1  (명성 회복 150전 고정)
    //   - 반복 상승가   : maxPurchases=N, priceMultiplier=1.5   (조리대 추가, 테이블 추가)
    // 다음 구매가 = basePrice * priceMultiplier^(현재 구매 횟수)
    [Serializable]
    public sealed class UpgradeDefinition
    {
        [SerializeField] private UpgradeId id;
        [SerializeField] private UpgradeCategory category;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int basePrice;
        [SerializeField, Min(1f)] private float priceMultiplier = 1f;
        [SerializeField, Min(1)] private int maxPurchases = 1;

        [Tooltip("이 업그레이드를 사려면 먼저 사야 하는 선행 업그레이드. 없으면 자기 자신으로 둔다.")]
        [SerializeField] private UpgradeId prerequisite;
        [SerializeField] private bool hasPrerequisite;

        public UpgradeId Id => id;
        public UpgradeCategory Category => category;
        public string DisplayName => displayName;
        public string Description => description;
        public int BasePrice => basePrice;
        public float PriceMultiplier => priceMultiplier;
        public int MaxPurchases => maxPurchases;
        public bool HasPrerequisite => hasPrerequisite;
        public UpgradeId Prerequisite => prerequisite;

        public UpgradeDefinition()
        {
        }

        public UpgradeDefinition(
            UpgradeId id,
            UpgradeCategory category,
            string displayName,
            string description,
            int basePrice,
            float priceMultiplier,
            int maxPurchases,
            UpgradeId? prerequisite)
        {
            this.id = id;
            this.category = category;
            this.displayName = displayName;
            this.description = description;
            this.basePrice = basePrice;
            this.priceMultiplier = priceMultiplier;
            this.maxPurchases = maxPurchases;
            hasPrerequisite = prerequisite.HasValue;
            this.prerequisite = prerequisite ?? id;
        }

        // 이미 purchasedCount번 산 상태에서 다음 구매에 드는 값.
        public int PriceAt(int purchasedCount)
        {
            float price = basePrice * Mathf.Pow(priceMultiplier, purchasedCount);
            return Mathf.RoundToInt(price);
        }
    }
}
