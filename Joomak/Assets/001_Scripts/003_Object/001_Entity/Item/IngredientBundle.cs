using _001_Scripts._005_Data._000_Item;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class IngredientBundle : CarryableItem
    {
        [UnityEngine.SerializeField] private ItemBundleData bundleData;

        public ItemBundleData BundleData => bundleData;
    }
}
