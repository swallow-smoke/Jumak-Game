using UnityEngine;

namespace _001_Scripts._005_Data.Upgrade
{
    public readonly struct UpgradeDefinition
    {
        public UpgradeDefinition(
            UpgradeId id,
            UpgradeCategory category,
            string displayName,
            int basePrice,
            int maxPurchases = 1,
            float priceMultiplier = 1f,
            UpgradeId? prerequisite = null)
        {
            Id = id;
            Category = category;
            DisplayName = displayName;
            BasePrice = Mathf.Max(0, basePrice);
            MaxPurchases = Mathf.Max(1, maxPurchases);
            PriceMultiplier = Mathf.Max(1f, priceMultiplier);
            Prerequisite = prerequisite;
        }

        public UpgradeId Id { get; }
        public UpgradeCategory Category { get; }
        public string DisplayName { get; }
        public int BasePrice { get; }
        public int MaxPurchases { get; }
        public float PriceMultiplier { get; }
        public UpgradeId? Prerequisite { get; }

        public int PriceAt(int purchasedCount)
        {
            return Mathf.RoundToInt(BasePrice * Mathf.Pow(PriceMultiplier, Mathf.Max(0, purchasedCount)));
        }
    }

    public enum UpgradePurchaseResult
    {
        Success,
        UnknownUpgrade,
        SoldOut,
        LockedByPrerequisite,
        ReputationFull,
        NotEnoughMoney
    }

    public readonly struct UpgradeSnapshot
    {
        public UpgradeSnapshot(UpgradeDefinition definition, int level, int nextPrice, UpgradePurchaseResult purchaseResult)
        {
            Definition = definition;
            Level = level;
            NextPrice = nextPrice;
            PurchaseResult = purchaseResult;
        }

        public UpgradeDefinition Definition { get; }
        public int Level { get; }
        public int NextPrice { get; }
        public UpgradePurchaseResult PurchaseResult { get; }
        public bool IsPurchased => Level > 0;
        public bool IsSoldOut => PurchaseResult == UpgradePurchaseResult.SoldOut;
    }
}
