using System;
using _001_Scripts._005_Data.Upgrade;
using _001_Scripts._005_Data.Config;
using UnityEngine;

namespace _001_Scripts._005_Data.Hall
{
    // 손님 만족도/타이밍 값. 라운드 난이도 곡선이 붙으면 라운드마다 이 값을 갈아끼운다.
    [Serializable]
    public sealed class CustomerPatienceSettings
    {
        private static CustomerBalance Balance
        {
            get
            {
                GameBalance.EnsureLoaded();
                return GameBalance.Current.customer;
            }
        }

        public float FoodSeconds => Mathf.Max(1f, Balance.foodSeconds) + UpgradeBonusSeconds;
        public float StartingSatisfaction => Mathf.Max(0f, Balance.startingSatisfaction);
        public float OrderChance => Mathf.Clamp01(Balance.orderChance);

        public float RandomSatisfactionDecayPerSecond =>
            UnityEngine.Random.Range(Mathf.Max(0f, Balance.minSatisfactionDecayPerSecond),
                Mathf.Max(Balance.minSatisfactionDecayPerSecond, Balance.maxSatisfactionDecayPerSecond));

        public float RandomDecideSeconds =>
            UnityEngine.Random.Range(Mathf.Max(0f, Balance.minDecideSeconds),
                Mathf.Max(Balance.minDecideSeconds, Balance.maxDecideSeconds));

        private static float UpgradeBonusSeconds
        {
            get
            {
                RunState state = RunState.Instance;
                if (state == null)
                {
                    return 0f;
                }

                int levels = state.GetLevel(UpgradeId.Patience1)
                             + state.GetLevel(UpgradeId.Patience2)
                             + state.GetLevel(UpgradeId.Patience3);
                return Mathf.Clamp(levels, 0, 3) * 10f;
            }
        }

        public float RandomEatSeconds => UnityEngine.Random.Range(Mathf.Max(1f, Balance.minEatSeconds),
            Mathf.Max(Balance.minEatSeconds, Balance.maxEatSeconds));
    }
}
