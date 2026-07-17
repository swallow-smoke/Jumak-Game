namespace _001_Scripts._003_Object._001_Entity.Item.Interface
{
    public interface ISingleItemCarrier
    {
        CarryableItem HeldItem { get; }
        bool TryCarry(CarryableItem item);
        bool TryConsumeHeldItem(CarryableItem expectedItem);
        bool TryDropHeldItem(UnityEngine.Vector3 worldPosition);
        void SetFacingDirection(UnityEngine.Vector2 direction);
    }
}
