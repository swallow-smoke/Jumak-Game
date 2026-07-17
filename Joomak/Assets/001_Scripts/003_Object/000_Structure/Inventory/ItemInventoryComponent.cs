using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    public sealed class ItemInventoryComponent : MonoBehaviour, IItemContainerOwner
    {
        [SerializeField] private InventoryModel inventory = new();

        public InventoryModel Inventory => inventory;
    }
}
