using _001_Scripts._003_Object._000_Structure.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    public sealed class StorageStructure : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out IItemContainerOwner carrier))
            {
                return;
            }

            if (!carrier.Inventory.TryTransferFirstTo(inventory))
            {
                inventory.TryTransferFirstTo(carrier.Inventory);
            }
        }
    }
}
