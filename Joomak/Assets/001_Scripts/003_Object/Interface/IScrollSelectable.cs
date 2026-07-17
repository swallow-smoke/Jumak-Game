namespace _001_Scripts._003_Object.Interface
{
    // 상호작용 대상을 바라본 채 스크롤 입력(위/아래 등)으로 후보를 순환 선택할 수 있는 대상.
    // 예: CookingStation의 레시피 선택 모드.
    public interface IScrollSelectable
    {
        void Scroll(int direction);
    }

    // 레시피처럼 플레이어 이동 키를 메뉴 조작에 잠시 빌려 쓰는 상호작용.
    public interface ISelectionInputCapture : IScrollSelectable
    {
        bool IsSelectionActive { get; }
        bool CanControlSelection(UnityEngine.GameObject interactor);
        void ConfirmSelection(UnityEngine.GameObject interactor);
        void CancelSelection(UnityEngine.GameObject interactor);
    }
}
