using System;

namespace _001_Scripts._005_Data.Hall
{
    // 외부(UI/상점/라운드)에 넘기는 읽기 전용 주문 정보.
    // 내부 객체를 그대로 넘기면 밖에서 홀 상태를 마음대로 바꿀 수 있으므로 값 복사본만 준다.
    public readonly struct OrderSnapshot
    {
        public readonly Guid OrderId;
        public readonly string DishId;
        public readonly string CustomerId;
        public readonly HallOrderStatus Status;

        public OrderSnapshot(Guid orderId, string dishId, string customerId, HallOrderStatus status)
        {
            OrderId = orderId;
            DishId = dishId;
            CustomerId = customerId;
            Status = status;
        }
    }
}
