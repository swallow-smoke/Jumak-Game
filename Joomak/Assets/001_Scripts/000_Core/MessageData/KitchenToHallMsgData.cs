using System;

namespace _001_Scripts._000_Core.MessageData
{
    public enum KitchenOrderStatus
    {
        Accepted,
        Cooking,
        Rejected,
        Cancelled
    }

    public sealed class OrderStatusChangedMsgData : BaseMsgData
    {
        public Guid OrderId { get; }
        public KitchenOrderStatus Status { get; }
        public string Reason { get; }

        public OrderStatusChangedMsgData(Guid orderId, KitchenOrderStatus status, string reason = null)
        {
            OrderId = orderId;
            Status = status;
            Reason = reason;
        }
    }

    public sealed class DishReadyMsgData : BaseMsgData
    {
        public Guid OrderId { get; }
        public string DishId { get; }
        public string PickupStructureId { get; }

        public DishReadyMsgData(Guid orderId, string dishId, string pickupStructureId)
        {
            OrderId = orderId;
            DishId = dishId;
            PickupStructureId = pickupStructureId;
        }
    }

    public sealed class IngredientSupplyRequestedMsgData : BaseMsgData
    {
        public string IngredientId { get; }
        public int RequestedAmount { get; }

        public IngredientSupplyRequestedMsgData(string ingredientId, int requestedAmount)
        {
            IngredientId = ingredientId;
            RequestedAmount = Math.Max(1, requestedAmount);
        }
    }
}
