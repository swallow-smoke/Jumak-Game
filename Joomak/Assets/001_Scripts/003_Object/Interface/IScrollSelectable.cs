namespace _001_Scripts._003_Object.Interface
{
    // 상호작용 대상을 바라본 채 스크롤 입력(위/아래 등)으로 후보를 순환 선택할 수 있는 대상.
    // 예: CookingStation의 레시피 선택 모드.
    public interface IScrollSelectable
    {
        void Scroll(int direction);
    }
}
