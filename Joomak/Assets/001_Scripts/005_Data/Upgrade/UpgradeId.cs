namespace _001_Scripts._005_Data.Upgrade
{
    // 기획서 6-1/6-2/6-3의 모든 업그레이드.
    // 단계형(이동속도·조리시간·인내심)은 각 단계를 별도 항목으로 두고 이전 단계를 선행 조건으로 건다.
    public enum UpgradeId
    {
        // 공용 (6-1)
        Dash,
        MoveSpeed1,
        MoveSpeed2,
        MoveSpeed3,
        ReputationHeal,

        // 주방 (6-2)
        CookTime1,
        CookTime2,
        CookTime3,
        CookingSlot,

        // 홀 (6-3)
        Patience1,
        Patience2,
        Patience3,
        IronBroom,
        TableAdd,

        // 주방 추가 강화. 기존 enum 값 보존을 위해 뒤에 배치한다.
        FailureDelay1,
        FailureDelay2,
        FailureDelay3,
        PremiumDish
    }
}
