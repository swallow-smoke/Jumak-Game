using System;
using _001_Scripts._000_Core.MessageData;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public static class KitchenMessagePort
    {
        public static IDisposable OnOrderRequested(Action<BaseMessage> handler)
        {
            return MessagePipe.Subscribe<OrderRequestedMsgData>(handler, MessageEndpoint.Kitchen);
        }

        public static IDisposable OnOrderCancelled(Action<OrderCancelledMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Kitchen);
        }

        public static IDisposable OnDishCollected(Action<DishCollectedMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Kitchen);
        }

        public static IDisposable OnBundleDelivered(Action<IngredientBundleDeliveredMsgData> handler)
        {
            return MessagePipe.SubscribeData(handler, MessageEndpoint.Kitchen);
        }

        public static BaseMessage ReplyOrderStatus(
            in BaseMessage orderRequest,
            KitchenOrderStatus status,
            string reason = null,
            GameObject sender = null)
        {
            OrderRequestedMsgData order = orderRequest.GetData<OrderRequestedMsgData>();
            if (order == null)
            {
                throw new ArgumentException("The message is not an order request.", nameof(orderRequest));
            }

            OrderStatusChangedMsgData data = new(order.OrderId, status, reason);
            return MessagePipe.Reply(
                orderRequest,
                data,
                MessageEndpoint.Kitchen,
                MessageEndpoint.Hall,
                sender);
        }

        public static BaseMessage NotifyDishReady(
            in BaseMessage orderRequest,
            string pickupStructureId,
            GameObject sender = null)
        {
            OrderRequestedMsgData order = orderRequest.GetData<OrderRequestedMsgData>();
            if (order == null)
            {
                throw new ArgumentException("The message is not an order request.", nameof(orderRequest));
            }

            DishReadyMsgData data = new(order.OrderId, order.DishId, pickupStructureId);
            return MessagePipe.Reply(
                orderRequest,
                data,
                MessageEndpoint.Kitchen,
                MessageEndpoint.Hall,
                sender);
        }

        public static BaseMessage RequestIngredientSupply(
            string ingredientId,
            int amount,
            GameObject sender = null)
        {
            IngredientSupplyRequestedMsgData data = new(ingredientId, amount);
            return MessagePipe.Publish(data, MessageEndpoint.Kitchen, MessageEndpoint.Hall, sender);
        }
    }
}
