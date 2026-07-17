using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class IngredientBundleCarrier : MonoBehaviour, IIngredientBundleCarrier
    {
        [SerializeField] private ItemBundleData carriedBundle;

        public ItemBundleData CarriedBundle => carriedBundle;

        public bool TryCarry(ItemBundleData bundle)
        {
            if (bundle == null || carriedBundle != null)
            {
                return false;
            }

            carriedBundle = bundle;
            return true;
        }

        public bool TryRelease(ItemBundleData expectedBundle)
        {
            if (carriedBundle == null || carriedBundle != expectedBundle)
            {
                return false;
            }

            carriedBundle = null;
            return true;
        }
    }
}
