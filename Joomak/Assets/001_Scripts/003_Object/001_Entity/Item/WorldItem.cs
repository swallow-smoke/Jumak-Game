using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class WorldItem : BaseEntity, IPickable
    {
        [SerializeField] private ItemBase item;
        [SerializeField, Min(1)] private int amount = 1;

        public ItemBase Item => item;
        public int Amount => amount;

        public void Interact(GameObject interactor)
        {
            Pick(interactor);
        }

        public void Pick(GameObject picker)
        {
            if (item == null || !picker.TryGetComponent(out IItemContainerOwner carrier))
            {
                return;
            }

            if (carrier.Inventory.TryAdd(item, amount))
            {
                Destroy(gameObject);
            }
        }
    }
}
