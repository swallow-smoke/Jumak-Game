using System;
using System.Collections.Generic;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 홀 전체를 관장한다. 손님 스폰, 테이블 현황, 주방과의 주문 왕복을 담당.
    public sealed class HallManager : SinManagerBase<HallManager>
    {
        [Header("Spawn")]
        [SerializeField] private Customer customerPrefab;
        [SerializeField] private CustomerEntrance entrance;
        [SerializeField, Min(1f)] private float spawnInterval = 20f;

        [Header("Hall")]
        [SerializeField] private List<DiningTable> tables = new();
        [SerializeField] private List<ItemBase> menu = new();
        [SerializeField] private CustomerPatienceSettings patience = new();

        private readonly List<Customer> customers = new();
        private readonly Dictionary<Guid, Customer> pendingOrders = new();
        private readonly List<ReadyOrder> readyOrders = new();
        private readonly MessageSubscriptionBag subscriptions = new();
        private float spawnTimer;

        public IReadOnlyList<DiningTable> Tables => tables;

        public override void Initialize()
        {
            subscriptions.Add(HallMessagePort.OnDishReady(OnDishReady));
            subscriptions.Add(HallMessagePort.OnOrderStatusChanged(OnOrderStatusChanged));
        }

        protected override void OnDestroy()
        {
            subscriptions.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer < spawnInterval)
            {
                return;
            }

            spawnTimer = 0f;
            TrySpawnCustomer();
        }

        public Guid SubmitOrder(Customer customer, GameObject sender)
        {
            BaseMessage message = HallMessagePort.RequestOrder(
                customer.CustomerId,
                customer.OrderedDish.ItemId,
                patience.FoodSeconds,
                sender);

            Guid orderId = message.GetData<OrderRequestedMsgData>().OrderId;
            pendingOrders[orderId] = customer;
            return orderId;
        }

        public void CancelOrder(Guid orderId, string reason, GameObject sender)
        {
            if (orderId == Guid.Empty || !pendingOrders.Remove(orderId))
            {
                return;
            }

            readyOrders.RemoveAll(order => order.OrderId == orderId);
            HallMessagePort.CancelOrder(orderId, reason, sender);
        }

        public void CompleteOrder(Guid orderId) => pendingOrders.Remove(orderId);

        public void Unregister(Customer customer) => customers.Remove(customer);

        // 홀 플레이어가 서빙 카운터에서 요리를 집었다. 어느 주문 건인지는 완성 순서(FIFO)로 맞춘다.
        public void NotifyDishCollected(string dishId, GameObject sender)
        {
            for (int i = 0; i < readyOrders.Count; i++)
            {
                if (readyOrders[i].DishId != dishId)
                {
                    continue;
                }

                HallMessagePort.NotifyDishCollected(readyOrders[i].OrderId, dishId, sender);
                readyOrders.RemoveAt(i);
                return;
            }
        }

        private void OnDishReady(DishReadyMsgData data)
        {
            if (pendingOrders.ContainsKey(data.OrderId))
            {
                readyOrders.Add(new ReadyOrder(data.OrderId, data.DishId));
            }
        }

        private void OnOrderStatusChanged(OrderStatusChangedMsgData data)
        {
            if (data.Status is KitchenOrderStatus.Rejected or KitchenOrderStatus.Cancelled)
            {
                Debug.LogWarning($"[Hall] 주방이 주문을 거절했습니다. {data.OrderId} ({data.Reason})");
            }
        }

        private void TrySpawnCustomer()
        {
            if (customerPrefab == null || entrance == null || menu.Count == 0)
            {
                return;
            }

            int waitingCount = CountWaitingCustomers();
            if (!entrance.TryGetWaitingSpot(waitingCount, out Vector3 waitSpot))
            {
                return;
            }

            ItemBase dish = menu[UnityEngine.Random.Range(0, menu.Count)];
            Customer customer = Instantiate(customerPrefab, entrance.SpawnPosition, Quaternion.identity);
            customer.Initialize(this, patience, dish, waitSpot, entrance.ExitPosition);
            customers.Add(customer);
        }

        private int CountWaitingCustomers()
        {
            int count = 0;
            foreach (Customer customer in customers)
            {
                if (customer != null && customer.IsWaitingForSeat)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnValidate()
        {
            foreach (ItemBase dish in menu)
            {
                if (dish != null && dish.Category != ItemCategory.Dish)
                {
                    Debug.LogWarning($"{name}: 메뉴 항목 {dish.name}은 Dish 카테고리가 아닙니다.", this);
                }
            }
        }

        private readonly struct ReadyOrder
        {
            public readonly Guid OrderId;
            public readonly string DishId;

            public ReadyOrder(Guid orderId, string dishId)
            {
                OrderId = orderId;
                DishId = dishId;
            }
        }
    }
}
