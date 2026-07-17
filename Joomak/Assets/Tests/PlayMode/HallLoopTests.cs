#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Hall;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace _001_Scripts.Tests
{
    public sealed class HallLoopTests
    {
        // 손님이 퇴장 중에 도착해서 사라져버리면 검사를 못 하므로 출구를 멀리 둔다.
        private static readonly Vector3 FarExit = new(1000f, 0f, 0f);

        private readonly List<Object> spawned = new();
        private readonly List<System.IDisposable> subscriptions = new();

        [TearDown]
        public void TearDown()
        {
            foreach (System.IDisposable subscription in subscriptions)
            {
                subscription.Dispose();
            }

            subscriptions.Clear();

            // 매니저가 static Instance를 잡고 있어서 즉시 파괴하지 않으면 다음 테스트가 오염된다.
            // 씬을 로드하는 테스트가 남긴 오브젝트까지 쓸어야 하므로 씬 루트 전체를 지운다.
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            foreach (Object target in spawned)
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            spawned.Clear();
        }

        [UnityTest]
        public IEnumerator 손님이_안내부터_식사까지_서빙루프를_완주한다()
        {
            ReputationManager reputation = NewManager<ReputationManager>();
            HallManager hall = NewManager<HallManager>();
            ItemData dish = NewDish("rice", "쌀밥");
            ItemData plate = NewPlate();
            CustomerPatienceSettings patience = NewFastPatience();

            // 주방 대역: 주문이 실제로 MessagePipe를 타고 넘어오는지 확인한다.
            BaseMessage? orderSeenByKitchen = null;
            subscriptions.Add(KitchenMessagePort.OnOrderRequested(message => orderSeenByKitchen = message));

            (DiningTable table, Seat seat) = NewTable(plate);
            GameObject player = NewPlayer(out SingleItemCarrier carrier);
            Customer customer = NewCustomer(hall, patience, dish);

            Assert.AreEqual(CustomerState.WaitingForSeat, customer.State, "생성 직후엔 입구에서 대기해야 한다");

            // 1) 안내
            customer.Interact(player);
            Assert.AreEqual(CustomerState.Following, customer.State, "안내하면 따라와야 한다");

            // 2) 착석
            table.Interact(player);
            Assert.AreEqual(CustomerState.WalkingToSeat, customer.State, "테이블을 잡으면 자리로 향해야 한다");
            Assert.AreSame(customer, seat.Occupant, "자리가 선점되어야 한다");

            yield return null;
            Assert.AreEqual(CustomerState.Deciding, customer.State, "자리에 도착하면 메뉴를 고민해야 한다");

            // 3) 주문
            yield return new WaitForSeconds(0.25f);
            Assert.AreEqual(CustomerState.ReadyToOrder, customer.State, "고민이 끝나면 주문 준비가 되어야 한다");

            customer.Interact(player);
            Assert.AreEqual(CustomerState.WaitingForFood, customer.State, "주문을 받으면 음식을 기다려야 한다");
            Assert.IsTrue(orderSeenByKitchen.HasValue, "주방이 주문 메시지를 못 받았다");
            Assert.AreEqual("rice", orderSeenByKitchen.Value.GetData<OrderRequestedMsgData>().DishId);

            // 4) 서빙
            carrier.TryCarry(NewWorldItem(dish));
            customer.Interact(player);
            Assert.AreEqual(CustomerState.Eating, customer.State, "주문한 요리를 받으면 먹어야 한다");
            Assert.IsNull(carrier.HeldItem, "건네준 요리는 손에서 사라져야 한다");

            // 5) 식사 종료 → 퇴장 + 빈 그릇
            yield return new WaitForSeconds(0.35f);
            Assert.AreEqual(CustomerState.Leaving, customer.State, "식사가 끝나면 퇴장해야 한다");
            Assert.IsTrue(seat.HasDirtyPlate, "자리에 빈 그릇이 남아야 한다");
            Assert.IsFalse(seat.IsFree, "빈 그릇이 있는 자리는 재사용되면 안 된다");
            Assert.AreEqual(20, reputation.Current, "정상 접객인데 명성이 깎였다");

            // 6) 그릇을 치우면 자리가 풀린다
            table.Interact(player);
            Assert.IsTrue(carrier.HeldItem != null, "테이블에서 빈 그릇을 집어야 한다");
            Assert.IsTrue(seat.IsFree, "그릇을 치우면 자리가 다시 비어야 한다");
        }

        [UnityTest]
        public IEnumerator 손님을_방치하면_명성이_깎이고_나간다()
        {
            ReputationManager reputation = NewManager<ReputationManager>();
            HallManager hall = NewManager<HallManager>();
            ItemData dish = NewDish("gukbap", "국밥");

            CustomerPatienceSettings patience = NewFastPatience();
            SetField(patience, "seatSeconds", 0.2f);

            Customer customer = NewCustomer(hall, patience, dish);
            Assert.AreEqual(20, reputation.Current, "명성 시작값은 20이어야 한다");

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(CustomerState.Leaving, customer.State, "인내심이 끝나면 나가야 한다");
            Assert.AreEqual(15, reputation.Current, "방치하면 명성이 5 깎여야 한다");
        }

        // 위 두 테스트는 오브젝트를 코드로 만들어 로직만 본다.
        // 이건 실제로 생성된 씬을 띄워서 배선(프리팹/입구/테이블/메뉴 참조)이 맞는지 확인한다.
        [UnityTest]
        public IEnumerator 생성된_HallTest_씬에서_손님이_스폰된다()
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                "Assets/000_Scenes/HallTest.unity",
                new LoadSceneParameters(LoadSceneMode.Single));

            yield return null;

            Assert.IsNotNull(Object.FindAnyObjectByType<HallManager>(), "씬에 HallManager가 없다");
            Assert.IsNotNull(Object.FindAnyObjectByType<ServingCounter>(), "씬에 ServingCounter가 없다");
            Assert.AreEqual(3, Object.FindObjectsByType<DiningTable>(FindObjectsSortMode.None).Length, "테이블은 3개여야 한다");
            Assert.AreEqual(6, Object.FindObjectsByType<Seat>(FindObjectsSortMode.None).Length, "좌석은 6석이어야 한다");

            // spawnInterval이 8초라 여유를 두고 기다린다.
            float deadline = Time.time + 12f;
            while (Object.FindAnyObjectByType<Customer>() == null && Time.time < deadline)
            {
                yield return null;
            }

            Customer customer = Object.FindAnyObjectByType<Customer>();
            Assert.IsNotNull(customer, "HallManager가 손님을 스폰하지 못했다 (프리팹/입구/메뉴 참조 확인)");
            Assert.IsNotNull(customer.OrderedDish, "손님에게 주문할 요리가 배정되지 않았다");
            Assert.AreEqual(CustomerState.WaitingForSeat, customer.State, "스폰된 손님은 입구에서 대기해야 한다");
        }

        private T NewManager<T>() where T : SinManagerBase<T>
        {
            GameObject host = new(typeof(T).Name);
            spawned.Add(host);
            return host.AddComponent<T>();
        }

        private Customer NewCustomer(HallManager hall, CustomerPatienceSettings patience, ItemData dish)
        {
            GameObject host = new("Customer");
            spawned.Add(host);
            Customer customer = host.AddComponent<Customer>();
            customer.Initialize(hall, patience, dish, Vector3.zero, FarExit);
            return customer;
        }

        private GameObject NewPlayer(out SingleItemCarrier carrier)
        {
            GameObject host = new("Player");
            spawned.Add(host);
            carrier = host.AddComponent<SingleItemCarrier>();
            host.AddComponent<CustomerEscort>();
            return host;
        }

        private (DiningTable, Seat) NewTable(ItemData plate)
        {
            // Awake가 seats를 읽으므로 비활성 상태로 만들어 필드를 채운 뒤 켠다.
            GameObject host = new("DiningTable");
            host.SetActive(false);
            spawned.Add(host);

            DiningTable table = host.AddComponent<DiningTable>();

            GameObject seatHost = new("Seat_0");
            seatHost.transform.SetParent(host.transform);
            Seat seat = seatHost.AddComponent<Seat>();

            SetField(table, "seats", new List<Seat> { seat });
            SetField(table, "plateItem", plate);
            host.SetActive(true);
            return (table, seat);
        }

        private WorldItem NewWorldItem(ItemData dish)
        {
            GameObject host = new("WorldItem");
            spawned.Add(host);
            WorldItem item = host.AddComponent<WorldItem>();
            item.Initialize(dish);
            return item;
        }

        private ItemData NewDish(string id, string displayName) => NewItem(id, displayName, ItemCategory.Dish);

        private ItemData NewPlate()
        {
            ItemData plate = NewItem("plate", "접시", ItemCategory.Plate);
            // DiningTable이 빈 그릇을 월드에 만들 때 WorldPrefab이 필요하다.
            // 비활성으로 두면 Instantiate 사본도 비활성이라 Awake가 안 돌고 CarryableItem이 터진다.
            GameObject prefab = new("PlatePrefab");
            spawned.Add(prefab);
            prefab.AddComponent<WorldItem>();
            SetField(plate, "worldPrefab", prefab);
            return plate;
        }

        private ItemData NewItem(string id, string displayName, ItemCategory category)
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            spawned.Add(item);
            SetField(item, "itemId", id);
            SetField(item, "displayName", displayName);
            SetField(item, "category", category);
            return item;
        }

        private static CustomerPatienceSettings NewFastPatience()
        {
            CustomerPatienceSettings patience = new();
            SetField(patience, "seatSeconds", 30f);
            SetField(patience, "orderSeconds", 30f);
            SetField(patience, "foodSeconds", 30f);
            SetField(patience, "decideSeconds", 0.1f);
            SetField(patience, "minEatSeconds", 0.2f);
            SetField(patience, "maxEatSeconds", 0.2f);
            return patience;
        }

        private static void SetField(object target, string name, object value)
        {
            // private 필드는 상속되지 않으므로 선언 타입을 찾을 때까지 거슬러 올라간다. (itemId는 ItemBase 소유)
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null)
                {
                    continue;
                }

                field.SetValue(target, value);
                return;
            }

            Assert.Fail($"필드를 찾을 수 없다: {target.GetType().Name}.{name}");
        }
    }
}
#endif
