using System;
using System.Collections.Generic;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using _001_Scripts._001_Manager.Interface;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 홀 전체를 관장한다. 손님 스폰, 테이블 현황, 주방과의 주문 왕복.
    // 외부에는 IHallService(읽기 중심)만 노출하고, 홀 내부(Customer, ServingCounter)는 이 클래스를 직접 부른다.
    public sealed class HallManager : SinManagerBase<HallManager>, IHallService
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
        private readonly List<OrderRecord> orders = new();
        private readonly MessageSubscriptionBag subscriptions = new();
        private float spawnTimer;

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

        // ---------------- 홀 내부용 주문 CRUD ----------------

        public Guid CreateOrder(Customer customer, GameObject sender)
        {
            string dishId = customer.OrderedDish.ItemId;
            BaseMessage message = HallMessagePort.RequestOrder(customer.CustomerId, dishId, patience.FoodSeconds, sender);
            Guid orderId = message.GetData<OrderRequestedMsgData>().OrderId;
            orders.Add(new OrderRecord(orderId, dishId, customer));
            return orderId;
        }

        public bool UpdateOrderStatus(Guid orderId, HallOrderStatus status)
        {
            OrderRecord record = Find(orderId);
            if (record == null)
            {
                return false;
            }

            record.Status = status;

            // 손님에게 전달된 주문은 더 추적할 이유가 없다.
            if (status == HallOrderStatus.Served)
            {
                orders.Remove(record);
            }

            return true;
        }

        public bool DeleteOrder(Guid orderId, string reason, GameObject sender)
        {
            OrderRecord record = Find(orderId);
            if (record == null)
            {
                return false;
            }

            orders.Remove(record);
            HallMessagePort.CancelOrder(orderId, reason, sender);
            return true;
        }

        // 홀 플레이어가 서빙 카운터에서 요리를 집었다. 어느 주문 건인지는 주문 순서(FIFO)로 맞춘다.
        public bool TryCollectDish(string dishId, GameObject sender)
        {
            foreach (OrderRecord record in orders)
            {
                if (record.Status != HallOrderStatus.Ready || record.DishId != dishId)
                {
                    continue;
                }

                record.Status = HallOrderStatus.Collected;
                HallMessagePort.NotifyDishCollected(record.OrderId, dishId, sender);
                return true;
            }

            return false;
        }

        public void Unregister(Customer customer) => customers.Remove(customer);

        // ---------------- IHallService (외부 공개 API) ----------------

        public IReadOnlyList<OrderSnapshot> GetOrders()
        {
            List<OrderSnapshot> result = new(orders.Count);
            foreach (OrderRecord record in orders)
            {
                result.Add(ToSnapshot(record));
            }

            return result;
        }

        public bool TryGetOrder(Guid orderId, out OrderSnapshot order)
        {
            OrderRecord record = Find(orderId);
            if (record == null)
            {
                order = default;
                return false;
            }

            order = ToSnapshot(record);
            return true;
        }

        public int GetOrderCount(HallOrderStatus status)
        {
            int count = 0;
            foreach (OrderRecord record in orders)
            {
                if (record.Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        public IReadOnlyList<TableSnapshot> GetTables()
        {
            List<TableSnapshot> result = new(tables.Count);
            foreach (DiningTable table in tables)
            {
                if (table == null)
                {
                    continue;
                }

                int free = 0;
                int dirty = 0;
                foreach (Seat seat in table.Seats)
                {
                    if (seat == null)
                    {
                        continue;
                    }

                    if (seat.IsFree)
                    {
                        free++;
                    }

                    if (seat.HasDirtyPlate)
                    {
                        dirty++;
                    }
                }

                result.Add(new TableSnapshot(table.ObjectId, table.ObjectName, table.Seats.Count, free, dirty));
            }

            return result;
        }

        public int GetFreeSeatCount()
        {
            int count = 0;
            foreach (DiningTable table in tables)
            {
                if (table == null)
                {
                    continue;
                }

                foreach (Seat seat in table.Seats)
                {
                    if (seat != null && seat.IsFree)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public int GetReputation() => ReputationManager.Instance != null ? ReputationManager.Instance.Current : 0;

        public int GetMaxReputation() => ReputationManager.Instance != null ? ReputationManager.Instance.MaxValue : 0;

        public void UpdateReputation(int delta, string reason)
        {
            if (ReputationManager.Instance == null || delta == 0)
            {
                return;
            }

            if (delta < 0)
            {
                ReputationManager.Instance.Penalize(-delta, reason);
                return;
            }

            ReputationManager.Instance.Restore(delta);
        }

        // ---------------- 내부 ----------------

        private void OnDishReady(DishReadyMsgData data) => UpdateOrderStatus(data.OrderId, HallOrderStatus.Ready);

        private void OnOrderStatusChanged(OrderStatusChangedMsgData data)
        {
            if (data.Status is KitchenOrderStatus.Rejected or KitchenOrderStatus.Cancelled)
            {
                Debug.LogWarning($"[Hall] 주방이 주문을 거절했습니다. {data.OrderId} ({data.Reason})");
            }
        }

        private OrderRecord Find(Guid orderId)
        {
            foreach (OrderRecord record in orders)
            {
                if (record.OrderId == orderId)
                {
                    return record;
                }
            }

            return null;
        }

        private static OrderSnapshot ToSnapshot(OrderRecord record) => new(
            record.OrderId,
            record.DishId,
            record.Customer != null ? record.Customer.CustomerId : null,
            record.Status);

        private void TrySpawnCustomer()
        {
            if (customerPrefab == null || entrance == null || menu.Count == 0)
            {
                return;
            }

            if (!entrance.TryGetWaitingSpot(CountWaitingCustomers(), out Vector3 waitSpot))
            {
                return;
            }

            ItemBase dish = menu[UnityEngine.Random.Range(0, menu.Count)];

            // 기획서 9번: 손놈 발생 확률은 손님 단위로 굴린다.
            bool rowdy = EventManager.Instance != null && EventManager.Instance.Settings.RollRowdy();

            Customer customer = Instantiate(customerPrefab, entrance.SpawnPosition, Quaternion.identity);
            customer.Initialize(this, patience, dish, waitSpot, entrance.ExitPosition, rowdy);
            customers.Add(customer);
        }

        private int CountWaitingCustomers()
        {
            int count = 0;
            foreach (Customer customer in customers)
            {
                if (customer != null && customer.OccupiesWaitingSpot)
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

        private sealed class OrderRecord
        {
            public readonly Guid OrderId;
            public readonly string DishId;
            public readonly Customer Customer;
            public HallOrderStatus Status;

            public OrderRecord(Guid orderId, string dishId, Customer customer)
            {
                OrderId = orderId;
                DishId = dishId;
                Customer = customer;
                Status = HallOrderStatus.Placed;
            }
        }
    }
}
