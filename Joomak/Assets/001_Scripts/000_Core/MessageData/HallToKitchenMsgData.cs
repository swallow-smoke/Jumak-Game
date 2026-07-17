using System;

namespace _001_Scripts._000_Core.MessageData
{
    public sealed class OrderRequestedMsgData : BaseMsgData
    {
        public Guid OrderId { get; }
        public string CustomerId { get; }
        public string DishId { get; }
        public float TimeLimitSeconds { get; }

        public OrderRequestedMsgData(Guid orderId, string customerId, string dishId, float timeLimitSeconds)
        {
            OrderId = orderId == Guid.Empty ? Guid.NewGuid() : orderId;
            CustomerId = customerId;
            DishId = dishId;
            TimeLimitSeconds = Math.Max(0f, timeLimitSeconds);
        }
    }

    public sealed class OrderCancelledMsgData : BaseMsgData
    {
        public Guid OrderId { get; }
        public string Reason { get; }

        public OrderCancelledMsgData(Guid orderId, string reason = null)
        {
            OrderId = orderId;
            Reason = reason;
        }
    }

    public sealed class DishCollectedMsgData : BaseMsgData
    {
        public Guid OrderId { get; }
        public string DishId { get; }

        public DishCollectedMsgData(Guid orderId, string dishId)
        {
            OrderId = orderId;
            DishId = dishId;
        }
    }

    public sealed class IngredientBundleDeliveredMsgData : BaseMsgData
    {
        public Guid DeliveryId { get; }
        public string BundleId { get; }
        public string DropOffStructureId { get; }

        public IngredientBundleDeliveredMsgData(Guid deliveryId, string bundleId, string dropOffStructureId)
        {
            DeliveryId = deliveryId == Guid.Empty ? Guid.NewGuid() : deliveryId;
            BundleId = bundleId;
            DropOffStructureId = dropOffStructureId;
        }
    }
}
