using System;
using _001_Scripts._005_Data.Upgrade;
using _001_Scripts._005_Data.Config;
using UnityEngine;

namespace _001_Scripts._005_Data.Hall
{
    // 기획서 8번 랜덤 이벤트 + 9번 난이도 곡선의 이벤트 수치.
    // 라운드가 붙으면 라운드마다 이 객체를 갈아끼운다.
    [Serializable]
    public sealed class HallEventSettings
    {
        private static HallEventBalance Balance
        {
            get
            {
                GameBalance.EnsureLoaded();
                return GameBalance.Current.hallEvents;
            }
        }

        public float EventInterval => Mathf.Max(1f, Balance.eventIntervalSeconds);
        public float ResolveSeconds => Mathf.Max(1f, Balance.resolveSeconds);
        public float DineAndDashTelegraphSeconds => Mathf.Max(0f, Balance.dineAndDashTelegraphSeconds);
        public int RowdyHits => Mathf.Max(1, HasIronBroom ? Balance.ironBroomRowdyHits : Balance.rowdyHits);
        public int TrashHits => Mathf.Max(1, HasIronBroom ? Balance.ironBroomTrashHits : Balance.trashHits);
        public int DineAndDashHits => Mathf.Max(1, Balance.dineAndDashHits);

        private static bool HasIronBroom
        {
            get
            {
                RunState state = RunState.Instance;
                return state != null && state.GetLevel(UpgradeId.IronBroom) > 0;
            }
        }

        public bool RollRowdy() => UnityEngine.Random.value < Mathf.Clamp01(Balance.rowdyChance);
        public bool RollDineAndDash() => UnityEngine.Random.value < Mathf.Clamp01(Balance.dineAndDashChance);
    }
}
