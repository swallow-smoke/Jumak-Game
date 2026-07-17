using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    public sealed class BundleUnpackingStation : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier) ||
                carrier.HeldItem is not IngredientBundle bundle)
            {
                return;
            }

            if (bundle.BundleData == null || !inventory.TryAddRange(bundle.BundleData.GetUnpackedItems()))
            {
                return;
            }

            carrier.TryConsumeHeldItem(bundle);
        }
    }
}
