using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity;
using _001_Scripts._005_Data.Hall;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 기획서 8번 랜덤 이벤트 스케줄러.
    // 타이머로 뽑는 건 청소/촛불뿐이다. 손놈과 먹튀는 손님 하나하나에 굴리는 확률이라
    // 여기가 아니라 Customer 쪽에서 처리한다. (기획서 9번의 '손놈 발생 확률'이 손님 단위)
    public class EventManager : SinManagerBase<EventManager>
    {
        [SerializeField] private HallEventSettings settings = new();
        [SerializeField] private Trash trashPrefab;
        [SerializeField] private List<Candle> candles = new();

        [Tooltip("쓰레기가 생길 홀 바닥 범위 (월드 좌표)")]
        [SerializeField] private Rect trashArea = new(1f, -4.4f, 8f, 8.8f);

        private float eventTimer;

        public HallEventSettings Settings => settings;

        public override void Initialize()
        {
            eventTimer = 0f;
        }

        private void Update()
        {
            eventTimer += Time.deltaTime;
            if (eventTimer < settings.EventInterval)
            {
                return;
            }

            eventTimer = 0f;
            TriggerRandomEvent();
        }

        public void TriggerRandomEvent()
        {
            // 둘 중 하나를 뽑되, 뽑힌 쪽이 불가능하면(촛불이 다 켜져있다든지) 다른 쪽으로 넘어간다.
            bool trashFirst = Random.value < 0.5f;

            if (trashFirst ? TrySpawnTrash() : TryExtinguishCandle())
            {
                return;
            }

            if (trashFirst)
            {
                TryExtinguishCandle();
                return;
            }

            TrySpawnTrash();
        }

        public bool TrySpawnTrash()
        {
            if (trashPrefab == null)
            {
                return false;
            }

            Vector3 position = new(
                Random.Range(trashArea.xMin, trashArea.xMax),
                Random.Range(trashArea.yMin, trashArea.yMax),
                0f);

            Trash trash = Instantiate(trashPrefab, position, Quaternion.identity);
            trash.Initialize(settings.TrashHits, settings.ResolveSeconds);
            Debug.Log($"[Event] 청소: 쓰레기 발생 ({settings.TrashHits}회 연타, {settings.ResolveSeconds}초)");
            return true;
        }

        public bool TryExtinguishCandle()
        {
            List<Candle> lit = new();
            foreach (Candle candle in candles)
            {
                if (candle != null && candle.IsLit)
                {
                    lit.Add(candle);
                }
            }

            if (lit.Count == 0)
            {
                return false;
            }

            Candle target = lit[Random.Range(0, lit.Count)];
            target.Extinguish(settings.ResolveSeconds);
            Debug.Log($"[Event] 촛불 관리: {target.name}이(가) 꺼짐 ({settings.ResolveSeconds}초)");
            return true;
        }
    }
}
