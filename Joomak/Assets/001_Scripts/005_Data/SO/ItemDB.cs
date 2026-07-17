using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    [CreateAssetMenu(menuName = "Joomak/Items/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDB : ScriptableObject
    {
        [SerializeField] private List<ItemBase> items = new();

        private Dictionary<string, ItemBase> itemById;

        public IReadOnlyList<ItemBase> Items => items;

        public bool TryGet(string itemId, out ItemBase item)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            BuildIndexIfNeeded();
            return itemById.TryGetValue(itemId, out item);
        }

        private void OnEnable()
        {
            itemById = null;
        }

        private void OnValidate()
        {
            itemById = null;
        }

        private void BuildIndexIfNeeded()
        {
            if (itemById != null)
            {
                return;
            }

            itemById = new Dictionary<string, ItemBase>();
            foreach (ItemBase item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (!itemById.TryAdd(item.ItemId, item))
                {
                    Debug.LogWarning($"Duplicate item id: {item.ItemId}", this);
                }
            }
        }
    }
}
