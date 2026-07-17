using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class WorldItem : CarryableItem
    {
        [SerializeField] private ItemBase item;

        public ItemBase Item => item;

        public void Initialize(ItemBase itemData)
        {
            item = itemData;
            gameObject.name = itemData != null ? itemData.DisplayName : "WorldItem";
        }

        public static bool TryCreate(ItemBase itemData, Vector3 position, out WorldItem worldItem)
        {
            worldItem = null;
            if (itemData == null || itemData.WorldPrefab == null)
            {
                return false;
            }

            GameObject instance = Instantiate(itemData.WorldPrefab, position, Quaternion.identity);
            if (!instance.TryGetComponent(out worldItem))
            {
                worldItem = instance.AddComponent<WorldItem>();
            }

            worldItem.Initialize(itemData);
            return true;
        }
    }
}
