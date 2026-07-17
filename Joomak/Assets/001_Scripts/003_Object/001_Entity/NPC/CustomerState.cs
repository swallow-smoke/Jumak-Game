namespace _001_Scripts._003_Object._001_Entity.NPC
{
    // 기획서 4-1 서빙 루프의 각 단계 + 8번 이벤트 상태.
    public enum CustomerState
    {
        WaitingForSeat,
        Following,
        WalkingToSeat,
        Deciding,
        ReadyToOrder,
        WaitingForFood,
        Eating,
        Leaving,

        Rowdy,       // 손놈: 난동 중. 빗자루로 연타해 제압해야 한다.
        DineAndDash  // 먹튀: 계산하지 않고 입구로 도주 중. 빗자루로 잡으면 계산시킨다.
    }
}
