using System.Collections.Generic;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public sealed class IngredientBundle : CarryableItem
    {
        [SerializeField] private ItemBundleData bundleData;
        [SerializeField, Min(1)] private int deliveredStockAmount;

        public ItemBundleData BundleData => bundleData;
        public int DeliveredStockAmount => deliveredStockAmount > 0
            ? deliveredStockAmount
            : bundleData != null ? bundleData.UnpackMultiplier : 1;

        public void ConfigureDeliveryAmount(int amount)
        {
            deliveredStockAmount = Mathf.Max(1, amount);
        }

        public IEnumerable<ItemAmount> GetUnpackedItems()
        {
            if (bundleData == null)
            {
                yield break;
            }

            foreach (ItemAmount content in bundleData.Contents)
            {
                if (content.Item != null && content.Amount > 0)
                {
                    yield return new ItemAmount(content.Item, content.Amount * DeliveredStockAmount);
                }
            }
        }
    }
}
