using System;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._005_Data.Hall
{
    // 기획서 4-1 / 7 / 9번의 제한시간 값. 라운드 난이도 곡선이 붙으면 라운드마다 이 값을 갈아끼운다.
    [Serializable]
    public sealed class CustomerPatienceSettings
    {
        [Header("기획서 7번 - 명성 감소 제한시간")]
        [SerializeField, Min(1f)] private float seatSeconds = 80f;
        [SerializeField, Min(1f)] private float orderSeconds = 20f;
        [SerializeField, Min(1f)] private float foodSeconds = 60f;

        [Header("기획서 4-1 - 손님 행동")]
        [SerializeField, Min(0f)] private float decideSeconds = 10f;
        [SerializeField, Min(1f)] private float minEatSeconds = 10f;
        [SerializeField, Min(1f)] private float maxEatSeconds = 60f;

        public float SeatSeconds => seatSeconds + UpgradeApi.PatienceBonusSeconds;
        public float OrderSeconds => orderSeconds + UpgradeApi.PatienceBonusSeconds;
        public float FoodSeconds => foodSeconds + UpgradeApi.PatienceBonusSeconds;
        public float DecideSeconds => decideSeconds;

        public float RandomEatSeconds => UnityEngine.Random.Range(minEatSeconds, Mathf.Max(minEatSeconds, maxEatSeconds));
    }
}
