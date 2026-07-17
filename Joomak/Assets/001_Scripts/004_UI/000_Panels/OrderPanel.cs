using System;
using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._004_UI.Components;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using UnityEngine;

namespace _001_Scripts._004_UI._000_Panels
{
    // 새 주문이 들어올 때마다 Poster 프리팹을 orderListRoot 밑에 하나씩 소환해 그리드로 쌓는다.
    // GridLayoutGroup에 맡기지 않는 이유: Poster의 스폰 연출이 anchoredPosition을 직접 트윈하는데,
    // 자동 레이아웃이 붙어 있으면 매 레이아웃 패스마다 위치를 강제로 되돌려서 트윈과 싸운다.
    public sealed class OrderPanel : PanelBase
    {
        [SerializeField] private Poster posterPrefab;
        [SerializeField] private RectTransform orderListRoot;

        [Header("Grid")]
        [SerializeField, Min(1)] private int columns = 3;
        [SerializeField] private Vector2 cellSize = new(220f, 260f);
        [SerializeField] private Vector2 spacing = new(16f, 16f);

        [Tooltip("첫 번째(0번) 칸의 anchoredPosition. 보통 카드 앵커/피벗을 좌상단(0,1)으로 맞춰서 쓴다.")]
        [SerializeField] private Vector2 startAnchoredPosition = Vector2.zero;

        private readonly List<Poster> activePosters = new();
        private readonly Dictionary<Guid, Poster> postersByOrderId = new();

        protected override void Start()
        {
            base.Start();

            if (HallManager.Instance != null)
            {
                HallManager.Instance.OrderCreated += OnOrderCreated;
                HallManager.Instance.OrderRemoved += OnOrderRemoved;
            }
        }

        protected override void OnDestroy()
        {
            if (HallManager.Instance != null)
            {
                HallManager.Instance.OrderCreated -= OnOrderCreated;
                HallManager.Instance.OrderRemoved -= OnOrderRemoved;
            }

            base.OnDestroy();
        }

        private void OnOrderCreated(OrderSnapshot order)
        {
            if (posterPrefab == null || orderListRoot == null)
            {
                return;
            }

            Poster poster = Instantiate(posterPrefab, orderListRoot);

            // 자리를 잡기 전엔 비활성화해둔다. Poster.OnEnable()이 "현재 anchoredPosition"을
            // 쉴 자리로 캡처해서 스폰 연출을 재생하므로, 그리드 위치를 먼저 넣고 나서 켜야 한다.
            poster.gameObject.SetActive(false);
            ((RectTransform)poster.transform).anchoredPosition = GetGridPosition(activePosters.Count);
            activePosters.Add(poster);
            postersByOrderId[order.OrderId] = poster;
            poster.gameObject.SetActive(true);

            RecipeData recipe = null;
            HallManager.Instance?.TryGetRecipe(order.DishId, out recipe);
            poster.Show(recipe);
        }

        private void OnOrderRemoved(Guid orderId)
        {
            if (!postersByOrderId.Remove(orderId, out Poster poster))
            {
                return;
            }

            RemovePoster(poster);
        }

        // 주문 완료/취소 시 카드를 치우고 나머지를 앞으로 당겨 빈 칸을 남기지 않는다.
        public void RemovePoster(Poster poster)
        {
            int index = activePosters.IndexOf(poster);
            if (index < 0)
            {
                return;
            }

            activePosters.RemoveAt(index);
            RemovePosterLookup(poster);
            Destroy(poster.gameObject);

            for (int i = index; i < activePosters.Count; i++)
            {
                ((RectTransform)activePosters[i].transform).anchoredPosition = GetGridPosition(i);
            }
        }

        private void RemovePosterLookup(Poster poster)
        {
            Guid matchingId = Guid.Empty;
            foreach (KeyValuePair<Guid, Poster> pair in postersByOrderId)
            {
                if (pair.Value == poster)
                {
                    matchingId = pair.Key;
                    break;
                }
            }

            if (matchingId != Guid.Empty)
            {
                postersByOrderId.Remove(matchingId);
            }
        }

        private Vector2 GetGridPosition(int index)
        {
            int col = index % columns;
            int row = index / columns;

            return new Vector2(
                startAnchoredPosition.x + col * (cellSize.x + spacing.x),
                startAnchoredPosition.y - row * (cellSize.y + spacing.y));
        }
    }
}
