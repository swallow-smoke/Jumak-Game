namespace _001_Scripts._003_Object.Interface
{
    // 빗자루를 손에 들어야만 해결되는 상호작용. (기획서 8번: 손놈 척결 / 청소 / 먹튀 척결)
    // 촛불 관리·재료 배달은 맨손이므로 이 인터페이스를 구현하지 않는다.
    //
    // RequiresBroom이 프로퍼티인 이유: 빗자루 필요 여부가 고정이 아니다.
    // 손님은 평소엔 맨손으로 주문받고 서빙하지만, 먹튀 상태가 되면 빗자루로만 잡을 수 있다.
    public interface IBroomTarget : IInteractable
    {
        bool RequiresBroom { get; }
    }
}
