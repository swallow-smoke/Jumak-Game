using System;
using System.Collections.Generic;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using _001_Scripts._001_Manager.Interface;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 홀 전체를 관장한다. 손님 스폰, 테이블 현황, 주방과의 주문 왕복.
    // 외부에는 IHallService(읽기 중심)만 노출하고, 홀 내부(Customer, ServingCounter)는 이 클래스를 직접 부른다.
    public sealed class HallManager : SinManagerBase<HallManager>, IHallService
    {
        [Header("Spawn")]
        [Tooltip("손님 프리팹 여러 종류(생김새 다른 손님 등)를 넣으면 스폰할 때마다 순서대로 번갈아 쓴다.")]
        [SerializeField] private List<Customer> customerPrefabs = new();
        [SerializeField] private CustomerEntrance entrance;
        [SerializeField, Min(1f)] private float spawnInterval = 20f;

        [Header("Hall")]
        [Tooltip("씬에 놓인 테이블 전부를 '잠금 해제 순서대로' 넣는다.\n" +
                 "앞에서부터 startingTableCount개만 켜진 채로 시작하고, 나머지는 업그레이드로 하나씩 열린다.")]
        [SerializeField] private List<DiningTable> tables = new();

        [SerializeField, Min(1)] private int startingTableCount = 2;
        [SerializeField, Min(1)] private int maxTableCount = 6;
        [SerializeField] private List<ItemBase> menu = new();
        [SerializeField] private CustomerPatienceSettings patience = new();

        [Tooltip("dishId로 레시피(이미지/재료 목록)를 찾아야 하는 UI(OrderPanel 등)가 쓴다.")]
        [SerializeField] private RecipeDB recipeDatabase;

        private readonly List<Customer> customers = new();
        private readonly List<OrderRecord> orders = new();
        private readonly MessageSubscriptionBag subscriptions = new();
        private float spawnTimer;
        private int unlockedTableCount;
        private int nextCustomerPrefabIndex;

        // 주문서 UI(OrderPanel)가 새 주문이 들어올 때마다 이걸로 알아챈다.
        public event Action<OrderSnapshot> OrderCreated;

        public override void Initialize()
        {
            PopulateMenuFromRecipeDatabase();

            subscriptions.Add(HallMessagePort.OnDishReady(OnDishReady));
            subscriptions.Add(HallMessagePort.OnOrderStatusChanged(OnOrderStatusChanged));

            UpgradeApi.UpgradePurchased += OnUpgradePurchased;

            SetUnlockedTableCount(startingTableCount + PurchasedTableCount());

            // 영업 시작 직후 첫 손님을 보여주고, 이후부터 설정된 간격으로 받는다.
            // 첫 스폰까지 20초 동안 아무 일도 없으면 설정이 고장 난 것처럼 보이기 쉽다.
            spawnTimer = spawnInterval;
        }

        private void PopulateMenuFromRecipeDatabase()
        {
            if (menu.Count > 0 || recipeDatabase == null)
            {
                return;
            }

            foreach (RecipeData recipe in recipeDatabase.Recipes)
            {
                ItemBase dish = recipe != null ? recipe.Result.Item : null;
                if (dish != null && dish.Category == ItemCategory.Dish && !menu.Contains(dish))
                {
                    menu.Add(dish);
                }
            }

            Debug.Log($"[Hall] 비어 있던 메뉴를 RecipeDB에서 자동 구성했습니다: {menu.Count}종", this);
        }

        // 테이블은 씬에 미리 다 놓여 있고, 잠긴 것은 꺼둔다.
        // 꺼두면 렌더링도 상호작용 레이캐스트도 자동으로 걸러진다.
        private void SetUnlockedTableCount(int count)
        {
            unlockedTableCount = Mathf.Clamp(count, 0, UnlockableTableCount);

            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null)
                {
                    tables[i].gameObject.SetActive(i < unlockedTableCount);
                }
            }
        }

        // 씬에 놓인 개수와 최대치 중 작은 쪽. 6개로 설정해도 4개만 놓았으면 4개가 상한이다.
        private int UnlockableTableCount => Mathf.Min(maxTableCount, tables.Count);

        protected override void OnDestroy()
        {
            UpgradeApi.UpgradePurchased -= OnUpgradePurchased;
            subscriptions.Dispose();
            base.OnDestroy();
        }

        private static int PurchasedTableCount() => UpgradeApi.AddedTableCount;

        private void OnUpgradePurchased(UpgradeId id, int _)
        {
            if (id == UpgradeId.TableAdd)
            {
                SetUnlockedTableCount(startingTableCount + PurchasedTableCount());
            }
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
            OrderRecord record = new(orderId, dishId, customer);
            orders.Add(record);
            OrderCreated?.Invoke(ToSnapshot(record));
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

        public int GetTableCount() => unlockedTableCount;

        public int GetMaxTableCount() => UnlockableTableCount;

        // 상점의 '테이블 추가' 업그레이드가 부른다. 상한에 닿았으면 false.
        public bool TryUnlockTable()
        {
            if (unlockedTableCount >= UnlockableTableCount)
            {
                return false;
            }

            SetUnlockedTableCount(unlockedTableCount + 1);
            Debug.Log($"[Hall] 테이블 추가: {unlockedTableCount} / {UnlockableTableCount}");
            return true;
        }

        // 잠긴 테이블은 없는 셈 친다. 아래 조회들이 전부 열린 것만 본다.
        public IReadOnlyList<TableSnapshot> GetTables()
        {
            List<TableSnapshot> result = new(unlockedTableCount);
            foreach (DiningTable table in UnlockedTables())
            {
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
            foreach (DiningTable table in UnlockedTables())
            {
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

        private IEnumerable<DiningTable> UnlockedTables()
        {
            for (int i = 0; i < unlockedTableCount && i < tables.Count; i++)
            {
                if (tables[i] != null)
                {
                    yield return tables[i];
                }
            }
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
            if (customerPrefabs.Count == 0)
            {
                Debug.LogWarning("[Hall] Customer Prefabs가 비어 있어 손님을 생성할 수 없습니다.", this);
                return;
            }

            if (entrance == null)
            {
                Debug.LogWarning("[Hall] Customer Entrance가 연결되지 않아 손님을 생성할 수 없습니다.", this);
                return;
            }

            if (menu.Count == 0)
            {
                Debug.LogWarning("[Hall] Menu가 비어 있어 손님을 생성할 수 없습니다.", this);
                return;
            }

            if (!entrance.TryGetWaitingSpot(CountWaitingCustomers(), out Vector3 waitSpot))
            {
                return;
            }

            // 기획서 9번: 손놈 발생 확률은 손님 단위로 굴린다.
            bool rowdy = EventManager.Instance != null && EventManager.Instance.Settings.RollRowdy();

            Customer customer = Instantiate(GetNextCustomerPrefab(), entrance.SpawnPosition, Quaternion.identity);
            customer.Initialize(this, patience, waitSpot, entrance.ExitPosition, rowdy);
            customers.Add(customer);
            Debug.Log($"[Hall] 손님 등장: {customer.name} ({customers.Count}/{entrance.WaitingCapacity})", customer);
        }

        // 손님 프리팹이 여러 종류면 스폰할 때마다 순서대로 번갈아 쓴다 (매번 같은 손님만 나오지 않게).
        private Customer GetNextCustomerPrefab()
        {
            Customer prefab = customerPrefabs[nextCustomerPrefabIndex];
            nextCustomerPrefabIndex = (nextCustomerPrefabIndex + 1) % customerPrefabs.Count;
            return prefab;
        }

        // 손님은 착석 후 주문을 결정하는 시점에 이걸 불러 메뉴(=해금된 레시피 목록) 중 하나를 뽑는다.
        public bool TryGetRandomMenuItem(out ItemBase dish)
        {
            if (menu.Count == 0)
            {
                dish = null;
                return false;
            }

            dish = menu[UnityEngine.Random.Range(0, menu.Count)];
            return true;
        }

        // OrderPanel처럼 dishId만 들고 있는 UI가 주문서에 띄울 이미지/재료 목록을 찾을 때 쓴다.
        public bool TryGetRecipe(string dishId, out RecipeData recipe)
        {
            if (recipeDatabase == null)
            {
                recipe = null;
                return false;
            }

            return recipeDatabase.TryGetByDishId(dishId, out recipe);
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
            if (customerPrefabs.Count == 0)
            {
                Debug.LogWarning($"{name}: Customer Prefabs가 비어있어 손님이 절대 스폰되지 않습니다.", this);
            }

            maxTableCount = Mathf.Max(1, maxTableCount);
            startingTableCount = Mathf.Clamp(startingTableCount, 1, maxTableCount);

            if (tables.Count > maxTableCount)
            {
                Debug.LogWarning($"{name}: 테이블을 {tables.Count}개 넣었지만 최대치가 {maxTableCount}개라 뒤쪽은 영영 안 열립니다.", this);
            }
            else if (tables.Count < startingTableCount)
            {
                Debug.LogWarning($"{name}: 시작 테이블이 {startingTableCount}개인데 씬에 {tables.Count}개만 연결됐습니다.", this);
            }

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
