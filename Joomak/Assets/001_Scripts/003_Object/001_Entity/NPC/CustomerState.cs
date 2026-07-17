namespace _001_Scripts._003_Object._001_Entity.NPC
{
    // 기획서 4-1 서빙 루프의 각 단계.
    public enum CustomerState
    {
        WaitingForSeat,
        Following,
        WalkingToSeat,
        Deciding,
        ReadyToOrder,
        WaitingForFood,
        Eating,
        Leaving
    }
}
