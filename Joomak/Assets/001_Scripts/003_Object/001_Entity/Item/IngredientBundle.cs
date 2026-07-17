using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class IngredientBundle : BaseEntity, IPickable
    {
        [SerializeField] private ItemBundleData bundleData;

        public void Interact(GameObject interactor)
        {
            Pick(interactor);
        }

        public void Pick(GameObject picker)
        {
            if (bundleData == null || !picker.TryGetComponent(out IIngredientBundleCarrier carrier))
            {
                return;
            }

            if (carrier.TryCarry(bundleData))
            {
                Destroy(gameObject);
            }
        }
    }
}
