using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    public sealed class StorageStructure : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            if (carrier.HeldItem is WorldItem heldItem)
            {
                if (heldItem.Item != null && inventory.TryAdd(heldItem.Item, 1))
                {
                    carrier.TryConsumeHeldItem(heldItem);
                }

                return;
            }

            TryGiveFirstItem(carrier);
        }

        private void TryGiveFirstItem(ISingleItemCarrier carrier)
        {
            if (inventory.Stacks.Count == 0)
            {
                return;
            }

            ItemStack stack = inventory.Stacks[0];
            if (!WorldItem.TryCreate(stack.Item, transform.position, out WorldItem worldItem))
            {
                return;
            }

            if (carrier.TryCarry(worldItem))
            {
                inventory.TryRemove(stack.Item, 1);
                return;
            }

            Destroy(worldItem.gameObject);
        }
    }
}
