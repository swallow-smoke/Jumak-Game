using System;
using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    [Serializable]
    public struct ItemAmount
    {
        [SerializeField] private ItemBase item;
        [SerializeField, Min(1)] private int amount;

        public ItemBase Item => item;
        public int Amount => amount;

        public ItemAmount(ItemBase item, int amount)
        {
            this.item = item;
            this.amount = Mathf.Max(1, amount);
        }
    }
}
