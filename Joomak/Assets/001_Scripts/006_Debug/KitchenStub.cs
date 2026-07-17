using System.Collections.Generic;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._006_Debug
{
    // 홀만 단독으로 테스트하기 위한 가짜 주방.
    // 주문을 받으면 cookSeconds 뒤에 서빙 카운터로 요리를 올려준다.
    // 진짜 주방이 붙으면 이 오브젝트를 씬에서 지우면 된다.
    public sealed class KitchenStub : MonoBehaviour
    {
        [SerializeField] private ItemDB itemDatabase;
        [SerializeField] private ServingCounter servingCounter;
        [SerializeField, Min(0.1f)] private float cookSeconds = 5f;

        private readonly MessageSubscriptionBag subscriptions = new();
        private readonly List<PendingCook> pending = new();

        private void Awake()
        {
            subscriptions.Add(KitchenMessagePort.OnOrderRequested(OnOrderRequested));
            subscriptions.Add(KitchenMessagePort.OnOrderCancelled(OnOrderCancelled));
        }

        private void OnDestroy()
        {
            subscriptions.Dispose();
        }

        private void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].ReadyAt)
                {
                    continue;
                }

                CompleteCooking(pending[i]);
                pending.RemoveAt(i);
            }
        }

        private void OnOrderRequested(BaseMessage message)
        {
            OrderRequestedMsgData order = message.GetData<OrderRequestedMsgData>();
            if (itemDatabase == null || !itemDatabase.TryGet(order.DishId, out ItemBase _))
            {
                KitchenMessagePort.ReplyOrderStatus(message, KitchenOrderStatus.Rejected, "메뉴에 없는 요리", gameObject);
                return;
            }

            KitchenMessagePort.ReplyOrderStatus(message, KitchenOrderStatus.Cooking, null, gameObject);
            pending.Add(new PendingCook(message, Time.time + cookSeconds));
            Debug.Log($"[KitchenStub] 주문 접수: {order.DishId} ({cookSeconds}초 후 완성)");
        }

        private void OnOrderCancelled(OrderCancelledMsgData data)
        {
            pending.RemoveAll(cook => cook.OrderId == data.OrderId);
        }

        private void CompleteCooking(PendingCook cook)
        {
            OrderRequestedMsgData order = cook.Message.GetData<OrderRequestedMsgData>();
            if (servingCounter == null || !itemDatabase.TryGet(order.DishId, out ItemBase dish))
            {
                return;
            }

            if (!servingCounter.Inventory.TryAdd(dish, 1))
            {
                Debug.LogWarning($"[KitchenStub] 서빙 카운터가 가득 찼습니다: {order.DishId}");
                return;
            }

            KitchenMessagePort.NotifyDishReady(cook.Message, servingCounter.StructureId, gameObject);
            Debug.Log($"[KitchenStub] 완성: {dish.DisplayName} -> 서빙 카운터");
        }

        private readonly struct PendingCook
        {
            public readonly BaseMessage Message;
            public readonly float ReadyAt;

            public System.Guid OrderId => Message.GetData<OrderRequestedMsgData>().OrderId;

            public PendingCook(BaseMessage message, float readyAt)
            {
                Message = message;
                ReadyAt = readyAt;
            }
        }
    }
}
