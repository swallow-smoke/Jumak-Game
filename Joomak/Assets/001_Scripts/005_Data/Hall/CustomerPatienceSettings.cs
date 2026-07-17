using System;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._005_Data.Hall
{
    // 손님 만족도/타이밍 값. 라운드 난이도 곡선이 붙으면 라운드마다 이 값을 갈아끼운다.
    [Serializable]
    public sealed class CustomerPatienceSettings
    {
        [Header("주방 조리 제한시간 - 주문 시 주방으로 넘어가는 값 (기획서 7번)")]
        [SerializeField, Min(1f)] private float foodSeconds = 60f;

        [Header("만족도 - 입장 시 이 값에서 시작해 초당 랜덤하게(min~max) 깎인다")]
        [SerializeField, Min(0f)] private float startingSatisfaction = 100f;
        [SerializeField, Min(0f)] private float minSatisfactionDecayPerSecond = 0.5f;
        [SerializeField, Min(0f)] private float maxSatisfactionDecayPerSecond = 2f;

        [Header("착석 후 주문 결정까지 랜덤 대기시간")]
        [SerializeField, Min(0f)] private float minDecideSeconds = 5f;
        [SerializeField, Min(0f)] private float maxDecideSeconds = 10f;

        [Tooltip("착석한 손님이 실제로 주문할 확률. 나머지는 주문 없이 조용히 나간다.")]
        [SerializeField, Range(0f, 1f)] private float orderChance = 0.75f;

        [Header("기획서 4-1 - 식사 시간")]
        [SerializeField, Min(1f)] private float minEatSeconds = 10f;
        [SerializeField, Min(1f)] private float maxEatSeconds = 60f;

        public float FoodSeconds => foodSeconds + UpgradeBonusSeconds;
        public float StartingSatisfaction => startingSatisfaction;
        public float OrderChance => orderChance;

        public float RandomSatisfactionDecayPerSecond =>
            UnityEngine.Random.Range(minSatisfactionDecayPerSecond, Mathf.Max(minSatisfactionDecayPerSecond, maxSatisfactionDecayPerSecond));

        public float RandomDecideSeconds =>
            UnityEngine.Random.Range(minDecideSeconds, Mathf.Max(minDecideSeconds, maxDecideSeconds));

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

        public float RandomEatSeconds => UnityEngine.Random.Range(minEatSeconds, Mathf.Max(minEatSeconds, maxEatSeconds));
    }
}
