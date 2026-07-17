using System;
using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data.Upgrade
{
    // 업그레이드의 단일 공개 API. 씬 전환과 무관하게 한 판의 전·명성·구매 상태를 유지한다.
    public static class UpgradeApi
    {
        private static readonly Dictionary<UpgradeId, UpgradeDefinition> Definitions = BuildDefinitions();
        private static readonly Dictionary<UpgradeId, int> Levels = new();

        private static bool initialized;
        private static int money;
        private static int reputation;
        private static int maxReputation = 100;

        public static event Action<int> MoneyChanged;
        public static event Action<int> ReputationChanged;
        public static event Action<UpgradeId, int> UpgradePurchased;

        public static int Money
        {
            get
            {
                EnsureInitialized();
                return money;
            }
        }

        public static int Reputation
        {
            get
            {
                EnsureInitialized();
                return reputation;
            }
        }

        public static int MaxReputation
        {
            get
            {
                EnsureInitialized();
                return maxReputation;
            }
        }

        public static bool DashUnlocked => IsPurchased(UpgradeId.Dash);
        public static float MoveSpeedMultiplier => 1f + 0.1f * SumLevels(UpgradeId.MoveSpeed1, UpgradeId.MoveSpeed2, UpgradeId.MoveSpeed3);
        public static int PatienceBonusSeconds => 10 * SumLevels(UpgradeId.Patience1, UpgradeId.Patience2, UpgradeId.Patience3);
        public static bool IronBroomUnlocked => IsPurchased(UpgradeId.IronBroom);
        public static int AddedTableCount => Mathf.Clamp(GetLevel(UpgradeId.TableAdd), 0, 4);
        public static float CookTimeMultiplier => Mathf.Max(0.7f, 1f - 0.1f * SumLevels(UpgradeId.CookTime1, UpgradeId.CookTime2, UpgradeId.CookTime3));
        public static float FailureDelayMultiplier => 1f + 0.1f * SumLevels(UpgradeId.FailureDelay1, UpgradeId.FailureDelay2, UpgradeId.FailureDelay3);
        public static int SaleReputationBonus => IsPurchased(UpgradeId.PremiumDish) ? 1 : 0;

        public static void EnsureInitialized(int startingMoney = 0, int startingReputation = 20, int reputationLimit = 100)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            money = Mathf.Max(0, startingMoney);
            maxReputation = Mathf.Max(1, reputationLimit);
            reputation = Mathf.Clamp(startingReputation, 0, maxReputation);
            Levels.Clear();
        }

        public static void ResetRun(int startingMoney = 0, int startingReputation = 20, int reputationLimit = 100)
        {
            initialized = false;
            EnsureInitialized(startingMoney, startingReputation, reputationLimit);
            MoneyChanged?.Invoke(money);
            ReputationChanged?.Invoke(reputation);
        }

        public static List<_001_Scripts._001_Manager.UpgradeLevelSave> CaptureUpgradeLevels()
        {
            EnsureInitialized();
            List<_001_Scripts._001_Manager.UpgradeLevelSave> result = new();
            foreach (KeyValuePair<UpgradeId, int> entry in Levels)
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                result.Add(new _001_Scripts._001_Manager.UpgradeLevelSave
                {
                    upgradeId = entry.Key.ToString(),
                    level = entry.Value
                });
            }

            return result;
        }

        public static void RestoreRun(
            int savedMoney,
            int savedReputation,
            int savedMaxReputation,
            IReadOnlyList<_001_Scripts._001_Manager.UpgradeLevelSave> savedLevels)
        {
            initialized = true;
            money = Mathf.Max(0, savedMoney);
            maxReputation = Mathf.Max(1, savedMaxReputation);
            reputation = Mathf.Clamp(savedReputation, 0, maxReputation);
            Levels.Clear();

            if (savedLevels != null)
            {
                foreach (_001_Scripts._001_Manager.UpgradeLevelSave entry in savedLevels)
                {
                    if (entry != null && Enum.TryParse(entry.upgradeId, out UpgradeId id) && Definitions.TryGetValue(id, out UpgradeDefinition definition))
                    {
                        Levels[id] = Mathf.Clamp(entry.level, 0, definition.MaxPurchases);
                    }
                }
            }

            MoneyChanged?.Invoke(money);
            ReputationChanged?.Invoke(reputation);
        }

        public static bool TryGetDefinition(UpgradeId id, out UpgradeDefinition definition)
        {
            return Definitions.TryGetValue(id, out definition);
        }

        public static IReadOnlyCollection<UpgradeDefinition> GetDefinitions() => Definitions.Values;

        public static int GetLevel(UpgradeId id)
        {
            EnsureInitialized();
            return Levels.GetValueOrDefault(id, 0);
        }

        public static bool IsPurchased(UpgradeId id) => GetLevel(id) > 0;

        public static int GetPrice(UpgradeId id)
        {
            if (!Definitions.TryGetValue(id, out UpgradeDefinition definition))
            {
                return -1;
            }

            int level = GetLevel(id);
            return level >= definition.MaxPurchases ? -1 : definition.PriceAt(level);
        }

        public static UpgradeSnapshot GetSnapshot(UpgradeId id)
        {
            if (!Definitions.TryGetValue(id, out UpgradeDefinition definition))
            {
                return default;
            }

            return new UpgradeSnapshot(definition, GetLevel(id), GetPrice(id), CanPurchase(id));
        }

        public static UpgradePurchaseResult CanPurchase(UpgradeId id)
        {
            EnsureInitialized();

            if (!Definitions.TryGetValue(id, out UpgradeDefinition definition))
            {
                return UpgradePurchaseResult.UnknownUpgrade;
            }

            int level = GetLevel(id);
            if (level >= definition.MaxPurchases)
            {
                return UpgradePurchaseResult.SoldOut;
            }

            if (definition.Prerequisite.HasValue && !IsPurchased(definition.Prerequisite.Value))
            {
                return UpgradePurchaseResult.LockedByPrerequisite;
            }

            if (id == UpgradeId.ReputationHeal && reputation >= maxReputation)
            {
                return UpgradePurchaseResult.ReputationFull;
            }

            return money >= definition.PriceAt(level)
                ? UpgradePurchaseResult.Success
                : UpgradePurchaseResult.NotEnoughMoney;
        }

        public static UpgradePurchaseResult TryPurchase(UpgradeId id)
        {
            UpgradePurchaseResult result = CanPurchase(id);
            if (result != UpgradePurchaseResult.Success)
            {
                return result;
            }

            UpgradeDefinition definition = Definitions[id];
            int price = definition.PriceAt(GetLevel(id));
            money -= price;
            MoneyChanged?.Invoke(money);

            int nextLevel = GetLevel(id) + 1;
            Levels[id] = nextLevel;

            if (id == UpgradeId.ReputationHeal)
            {
                AddReputation(10);
            }

            UpgradePurchased?.Invoke(id, nextLevel);
            Debug.Log($"[UpgradeApi] {definition.DisplayName} 구매 (Lv.{nextLevel}, -{price}전)");
            return UpgradePurchaseResult.Success;
        }

        public static int AddMoney(int delta)
        {
            EnsureInitialized();
            money = Mathf.Max(0, money + delta);
            MoneyChanged?.Invoke(money);
            return money;
        }

        public static bool TrySpendMoney(int cost)
        {
            EnsureInitialized();
            if (cost < 0 || money < cost)
            {
                return false;
            }

            AddMoney(-cost);
            return true;
        }

        public static int AddReputation(int delta)
        {
            EnsureInitialized();
            reputation = Mathf.Clamp(reputation + delta, 0, maxReputation);
            ReputationChanged?.Invoke(reputation);
            return reputation;
        }

        private static int SumLevels(params UpgradeId[] ids)
        {
            int sum = 0;
            foreach (UpgradeId id in ids)
            {
                sum += GetLevel(id);
            }

            return Mathf.Clamp(sum, 0, ids.Length);
        }

        private static Dictionary<UpgradeId, UpgradeDefinition> BuildDefinitions()
        {
            return new Dictionary<UpgradeId, UpgradeDefinition>
            {
                [UpgradeId.Dash] = new(UpgradeId.Dash, UpgradeCategory.Common, "대쉬", 150),
                [UpgradeId.MoveSpeed1] = new(UpgradeId.MoveSpeed1, UpgradeCategory.Common, "이동속도 1단계", 80),
                [UpgradeId.MoveSpeed2] = new(UpgradeId.MoveSpeed2, UpgradeCategory.Common, "이동속도 2단계", 120, prerequisite: UpgradeId.MoveSpeed1),
                [UpgradeId.MoveSpeed3] = new(UpgradeId.MoveSpeed3, UpgradeCategory.Common, "이동속도 3단계", 180, prerequisite: UpgradeId.MoveSpeed2),
                [UpgradeId.ReputationHeal] = new(UpgradeId.ReputationHeal, UpgradeCategory.Common, "명성 회복", 150, 99),

                [UpgradeId.Patience1] = new(UpgradeId.Patience1, UpgradeCategory.Hall, "손님 인내심 1단계", 100),
                [UpgradeId.Patience2] = new(UpgradeId.Patience2, UpgradeCategory.Hall, "손님 인내심 2단계", 150, prerequisite: UpgradeId.Patience1),
                [UpgradeId.Patience3] = new(UpgradeId.Patience3, UpgradeCategory.Hall, "손님 인내심 3단계", 225, prerequisite: UpgradeId.Patience2),
                [UpgradeId.IronBroom] = new(UpgradeId.IronBroom, UpgradeCategory.Hall, "철제 손잡이 빗자루", 200),
                [UpgradeId.TableAdd] = new(UpgradeId.TableAdd, UpgradeCategory.Hall, "테이블 추가", 180, 4, 1.5f),

                [UpgradeId.CookTime1] = new(UpgradeId.CookTime1, UpgradeCategory.Kitchen, "조리시간 감소 1단계", 120),
                [UpgradeId.CookTime2] = new(UpgradeId.CookTime2, UpgradeCategory.Kitchen, "조리시간 감소 2단계", 180, prerequisite: UpgradeId.CookTime1),
                [UpgradeId.CookTime3] = new(UpgradeId.CookTime3, UpgradeCategory.Kitchen, "조리시간 감소 3단계", 270, prerequisite: UpgradeId.CookTime2),
                [UpgradeId.FailureDelay1] = new(UpgradeId.FailureDelay1, UpgradeCategory.Kitchen, "실패시간 지연 1단계", 100),
                [UpgradeId.FailureDelay2] = new(UpgradeId.FailureDelay2, UpgradeCategory.Kitchen, "실패시간 지연 2단계", 150, prerequisite: UpgradeId.FailureDelay1),
                [UpgradeId.FailureDelay3] = new(UpgradeId.FailureDelay3, UpgradeCategory.Kitchen, "실패시간 지연 3단계", 225, prerequisite: UpgradeId.FailureDelay2),
                [UpgradeId.PremiumDish] = new(UpgradeId.PremiumDish, UpgradeCategory.Kitchen, "고급 요리", 300)
            };
        }
    }
}
