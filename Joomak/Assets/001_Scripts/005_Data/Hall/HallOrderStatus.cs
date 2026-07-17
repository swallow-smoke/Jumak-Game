namespace _001_Scripts._005_Data.Hall
{
    // 홀이 보는 주문의 일생. 주방이 쓰는 KitchenOrderStatus와는 관점이 다르다.
    // (주방은 "조리 상태", 홀은 "손님에게 가기까지의 상태")
    public enum HallOrderStatus
    {
        Placed,     // 주문을 받아 주방에 넘김
        Ready,      // 주방이 완성해서 서빙 카운터에 올림
        Collected,  // 홀 플레이어가 카운터에서 집어감
        Served      // 손님에게 전달 완료 (전달 즉시 목록에서 빠진다)
    }
}
