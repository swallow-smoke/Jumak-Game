using _001_Scripts._005_Data._000_Item;

namespace _001_Scripts._003_Object._001_Entity.Item.Interface
{
    public interface IIngredientBundleCarrier
    {
        ItemBundleData CarriedBundle { get; }
        bool TryCarry(ItemBundleData bundle);
        bool TryRelease(ItemBundleData expectedBundle);
    }
}
