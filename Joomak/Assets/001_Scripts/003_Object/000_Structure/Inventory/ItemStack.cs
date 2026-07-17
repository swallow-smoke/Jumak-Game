using System;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    [Serializable]
    public sealed class ItemStack
    {
        [SerializeField] private ItemBase item;
        [SerializeField] private int amount;

        public ItemBase Item => item;
        public int Amount => amount;

        public ItemStack(ItemBase item, int amount)
        {
            this.item = item;
            this.amount = Mathf.Max(0, amount);
        }

        public void Add(int value)
        {
            amount = Mathf.Min(item.MaxStack, amount + Mathf.Max(0, value));
        }

        public int Remove(int value)
        {
            int removed = Mathf.Min(amount, Mathf.Max(0, value));
            amount -= removed;
            return removed;
        }
    }
}
