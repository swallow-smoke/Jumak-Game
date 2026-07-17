using System;
using _001_Scripts._000_Core.MessageData;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public static class HallMessagePort
    {
        public static BaseMessage RequestOrder(
            string customerId,
            string dishId,
            float timeLimitSeconds,
            GameObject sender = null,
            Guid orderId = default)
        {
            OrderRequestedMsgData data = new(orderId, customerId, dishId, timeLimitSeconds);
            return MessagePipe.Publish(data, MessageEndpoint.Hall, MessageEndpoint.Kitchen, sender);
        }

        public static BaseMessage CancelOrder(Guid orderId, string reason = null, GameObject sender = null)
        {
            OrderCancelledMsgData data = new(orderId, reason);
            return MessagePipe.Publish(data, MessageEndpoint.Hall, MessageEndpoint.Kitchen, sender);
        }

        public static BaseMessage NotifyDishCollected(Guid orderId, string dishId, GameObject sender = null)
        {
            DishCollectedMsgData data = new(orderId, dishId);
            return MessagePipe.Publish(data, MessageEndpoint.Hall, MessageEndpoint.Kitchen, sender);
        }

        public static BaseMessage NotifyBundleDelivered(
            string bundleId,
            string dropOffStructureId,
            GameObject sender = null,
            Guid deliveryId = default)
        {
            IngredientBundleDeliveredMsgData data = new(deliveryId, bundleId, dropOffStructureId);
            return MessagePipe.Publish(data, MessageEndpoint.Hall, MessageEndpoint.Kitchen, sender);
        }

        public static IDisposable OnOrderStatusChanged(Action<OrderStatusChangedMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Hall);
        }

        public static IDisposable OnDishReady(Action<DishReadyMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Hall);
        }

        public static IDisposable OnIngredientSupplyRequested(Action<IngredientSupplyRequestedMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Hall);
        }
    }
}
