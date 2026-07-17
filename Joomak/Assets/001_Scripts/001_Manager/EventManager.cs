using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._005_Data.Hall;
using _001_Scripts._004_UI.Components;
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

        [Tooltip("쓰레기가 생길 홀 바닥 범위 (월드 좌표). 홀은 x<0 (왼쪽), 주방은 x>0 (오른쪽).\n" +
                 "이 오브젝트를 선택하면 씬 뷰에 범위가 초록 사각형으로 그려진다. 타일맵에 맞춰 조정할 것.")]
        // 먼지는 홀 안(x -36.8 ~ -15.3의 왼쪽)에서만 생긴다. 오른쪽 주방으로 넘어가면 안 된다.
        // Rect(x, y, width, height): 오른쪽 끝(xMax)이 경계(-15.3)를 넘지 않게 -17에서 끊는다.
        [SerializeField] private Rect trashArea = new(-35f, -2f, 18f, 21f);

        [Header("재료 배달 (기획서 8번 + 5-1)")]
        [Tooltip("주방이 재료를 얻는 유일한 수단이다. 이게 안 오면 주방이 그대로 멈춘다.\n" +
                 "다른 이벤트와 달리 미해결 페널티가 없고, 늦게 받으면 그만큼 조리가 밀릴 뿐이다.")]
        [SerializeField, Min(10f)] private float deliveryInterval = 300f;
        [SerializeField, Min(1)] private int boxesPerDelivery = 3;

        [Tooltip("상자가 도착할 홀 안의 지점. 비어 있으면 배달이 아예 안 온다.")]
        [SerializeField] private Transform deliveryPoint;

        [Tooltip("배달될 수 있는 재료 묶음 프리팹들. 매 배달마다 여기서 서로 다른 종류로 뽑는다.")]
        [SerializeField] private List<IngredientBundle> bundlePrefabs = new();

        [SerializeField, Min(0.1f)] private float boxSpacing = 2f;

        [Tooltip("영업 시작 직후에도 한 번 배달한다. 끄면 첫 5분 동안 주방이 재료 없이 논다.")]
        [SerializeField] private bool deliverAtStart = true;

        private float eventTimer;
        private float deliveryTimer;

        public HallEventSettings Settings => settings;

        public override void Initialize()
        {
            eventTimer = 0f;
            deliveryTimer = 0f;
        }

        private void Start()
        {
            if (deliverAtStart)
            {
                TryDeliverIngredients();
            }
        }

        private void Update()
        {
            TickDelivery();

            eventTimer += Time.deltaTime;
            if (eventTimer < settings.EventInterval)
            {
                return;
            }

            eventTimer = 0f;
            TriggerRandomEvent();
        }

        // 재료 배달은 랜덤 이벤트 추첨과 별개다. 기획서상 5분 고정 간격이다.
        private void TickDelivery()
        {
            deliveryTimer += Time.deltaTime;
            if (deliveryTimer < deliveryInterval)
            {
                return;
            }

            deliveryTimer = 0f;
            TryDeliverIngredients();
        }

        // 기획서 8번: 5분 간격으로 재료 상자 3개 배송. 홀이 집어서 주방에 전달한다.
        public int TryDeliverIngredients()
        {
            if (deliveryPoint == null || bundlePrefabs.Count == 0)
            {
                Debug.LogWarning("[Event] 재료 배달: deliveryPoint나 bundlePrefabs가 비어 있어 배달을 건너뜁니다.", this);
                return 0;
            }

            // 같은 상자만 3개 오면 특정 재료가 영영 안 들어온다. 종류가 겹치지 않게 뽑되,
            // 묶음 종류가 배달 개수보다 적으면 어쩔 수 없이 다시 채워 쓴다.
            List<IngredientBundle> pool = new();
            int delivered = 0;

            for (int i = 0; i < boxesPerDelivery; i++)
            {
                if (pool.Count == 0)
                {
                    pool.AddRange(bundlePrefabs);
                }

                int index = Random.Range(0, pool.Count);
                IngredientBundle prefab = pool[index];
                pool.RemoveAt(index);

                if (prefab == null)
                {
                    continue;
                }

                // 상자끼리 겹쳐 쌓이면 집기 어려우니 옆으로 늘어놓는다.
                Vector3 offset = new((i - (boxesPerDelivery - 1) * 0.5f) * boxSpacing, 0f, 0f);
                Instantiate(prefab, deliveryPoint.position + offset, Quaternion.identity);
                delivered++;
            }

            Debug.Log($"[Event] 재료 배달: 상자 {delivered}개 도착 (다음 배달 {deliveryInterval}초 후)");
            if (delivered > 0)
            {
                NotificationModal.Show(
                    $"재료 배달이 도착했습니다.\n홀 입구의 상자 {delivered}개를 주방으로 옮겨주세요.",
                    NotificationKind.Warning,
                    5f);
                GameplayFeedback.Burst(deliveryPoint.position, new Color(1f, 0.68f, 0.2f), "배달 도착!", 16);
            }

            return delivered;
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
            NotificationModal.Important("홀에 쓰레기가 생겼습니다.\n빗자루로 치워주세요.", 4f);
            GameplayFeedback.Burst(position, new Color(0.55f, 0.42f, 0.28f), "청소 필요", 8);
            Debug.Log($"[Event] 청소: 쓰레기 발생 ({settings.TrashHits}회 연타, {settings.ResolveSeconds}초)");
            return true;
        }

        // 쓰레기 범위를 코드에 박아두면 레이아웃이 바뀔 때마다 조용히 틀어진다.
        // (실제로 홀이 왼쪽으로 옮겨졌을 때 쓰레기가 주방에 생기고 있었다)
        // 씬 뷰에서 직접 보고 맞출 수 있게 그려준다.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(trashArea.center, new Vector3(trashArea.width, trashArea.height, 0.1f));

            if (deliveryPoint == null)
            {
                return;
            }

            // 상자가 실제로 놓일 자리를 그려서, 벽이나 테이블에 겹치지 않는지 눈으로 확인할 수 있게 한다.
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            for (int i = 0; i < boxesPerDelivery; i++)
            {
                Vector3 offset = new((i - (boxesPerDelivery - 1) * 0.5f) * boxSpacing, 0f, 0f);
                Gizmos.DrawWireCube(deliveryPoint.position + offset, new Vector3(1.4f, 1.4f, 0.1f));
            }
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
            NotificationModal.Important("촛불 하나가 꺼졌습니다.\n시간 안에 다시 밝혀주세요.", 4f);
            Debug.Log($"[Event] 촛불 관리: {target.name}이(가) 꺼짐 ({settings.ResolveSeconds}초)");
            return true;
        }
    }
}
