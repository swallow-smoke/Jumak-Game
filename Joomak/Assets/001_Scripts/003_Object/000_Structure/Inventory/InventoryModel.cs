using System;
using System.Collections.Generic;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Inventory
{
    [Serializable]
    public sealed class InventoryModel
    {
        [SerializeField, Min(1)] private int slotCapacity = 8;
        [SerializeField] private List<ItemStack> stacks = new();

        public IReadOnlyList<ItemStack> Stacks => stacks;

        public bool Contains(ItemBase item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            int total = 0;
            foreach (ItemStack stack in stacks)
            {
                if (stack.Item == item)
                {
                    total += stack.Amount;
                }
            }

            return total >= amount;
        }

        public bool CanAdd(ItemBase item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return false;
            }

            int remaining = amount;
            foreach (ItemStack stack in stacks)
            {
                if (stack.Item == item)
                {
                    remaining -= item.MaxStack - stack.Amount;
                }
            }

            int freeSlots = slotCapacity - stacks.Count;
            return remaining <= freeSlots * item.MaxStack;
        }

        public bool TryAdd(ItemBase item, int amount)
        {
            if (!CanAdd(item, amount))
            {
                return false;
            }

            int remaining = amount;
            foreach (ItemStack stack in stacks)
            {
                if (stack.Item != item || stack.Amount >= item.MaxStack)
                {
                    continue;
                }

                int added = Mathf.Min(remaining, item.MaxStack - stack.Amount);
                stack.Add(added);
                remaining -= added;
                if (remaining == 0)
                {
                    return true;
                }
            }

            while (remaining > 0)
            {
                int added = Mathf.Min(remaining, item.MaxStack);
                stacks.Add(new ItemStack(item, added));
                remaining -= added;
            }

            return true;
        }

        public bool TryAddRange(IEnumerable<ItemAmount> items)
        {
            List<ItemStack> snapshot = CloneStacks();
            foreach (ItemAmount entry in items)
            {
                if (!TryAdd(entry.Item, entry.Amount))
                {
                    stacks = snapshot;
                    return false;
                }
            }

            return true;
        }

        public bool TryRemove(ItemBase item, int amount)
        {
            if (!Contains(item, amount))
            {
                return false;
            }

            int remaining = amount;
            for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack stack = stacks[i];
                if (stack.Item != item)
                {
                    continue;
                }

                int removed = stack.Remove(remaining);
                remaining -= removed;
                if (stack.Amount == 0)
                {
                    stacks.RemoveAt(i);
                }
            }

            return true;
        }

        public bool TryTransferFirstTo(InventoryModel destination)
        {
            if (destination == null || stacks.Count == 0)
            {
                return false;
            }

            ItemStack first = stacks[0];
            int transferAmount = first.Amount;
            if (!destination.TryAdd(first.Item, transferAmount))
            {
                return false;
            }

            return TryRemove(first.Item, transferAmount);
        }

        private List<ItemStack> CloneStacks()
        {
            List<ItemStack> clone = new(stacks.Count);
            foreach (ItemStack stack in stacks)
            {
                clone.Add(new ItemStack(stack.Item, stack.Amount));
            }

            return clone;
        }
    }
}
