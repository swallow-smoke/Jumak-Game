using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    public sealed class BundleUnpackingStation : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out IIngredientBundleCarrier carrier))
            {
                return;
            }

            ItemBundleData bundle = carrier.CarriedBundle;
            if (bundle == null || !inventory.TryAddRange(bundle.GetUnpackedItems()))
            {
                return;
            }

            carrier.TryRelease(bundle);
        }
    }
}
