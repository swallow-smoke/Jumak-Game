using System;
using UnityEngine;

namespace _001_Scripts._005_Data.Hall
{
    // 기획서 8번 랜덤 이벤트 + 9번 난이도 곡선의 이벤트 수치.
    // 라운드가 붙으면 라운드마다 이 객체를 갈아끼운다.
    [Serializable]
    public sealed class HallEventSettings
    {
        [Header("기획서 9번 - 라운드마다 갱신")]
        [SerializeField, Min(1f)] private float eventInterval = 45f;
        [SerializeField, Min(1f)] private float resolveSeconds = 30f;
        [SerializeField, Range(0f, 1f)] private float rowdyChance = 0.15f;

        [Header("기획서 8번 - 먹튀 (기획서에 확률 명시가 없어 임시값)")]
        [SerializeField, Range(0f, 1f)] private float dineAndDashChance = 0.15f;

        [Tooltip("먹튀가 튀기 전에 빨갛게 변한 채로 멈춰 있는 시간. 플레이어에게 반응할 틈을 준다.")]
        [SerializeField, Min(0f)] private float dineAndDashTelegraphSeconds = 3f;

        [Header("기획서 6-3 - 철제 손잡이 빗자루를 사면 낮아진다")]
        [SerializeField, Min(1)] private int rowdyHits = 5;
        [SerializeField, Min(1)] private int trashHits = 3;
        [SerializeField, Min(1)] private int dineAndDashHits = 1;

        public float EventInterval => eventInterval;
        public float ResolveSeconds => resolveSeconds;
        public float DineAndDashTelegraphSeconds => dineAndDashTelegraphSeconds;
        public int RowdyHits => rowdyHits;
        public int TrashHits => trashHits;
        public int DineAndDashHits => dineAndDashHits;

        public bool RollRowdy() => UnityEngine.Random.value < rowdyChance;
        public bool RollDineAndDash() => UnityEngine.Random.value < dineAndDashChance;
    }
}
