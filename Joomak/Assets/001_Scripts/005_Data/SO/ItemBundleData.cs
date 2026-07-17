using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    [CreateAssetMenu(menuName = "Joomak/Items/Ingredient Bundle", fileName = "IngredientBundle")]
    public sealed class ItemBundleData : ScriptableObject
    {
        [SerializeField] private string bundleId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int unpackMultiplier = 3;
        [SerializeField] private List<ItemAmount> contents = new();

        public string BundleId => bundleId;
        public string DisplayName => displayName;
        public int UnpackMultiplier => unpackMultiplier;
        public IReadOnlyList<ItemAmount> Contents => contents;

        public IEnumerable<ItemAmount> GetUnpackedItems()
        {
            foreach (ItemAmount content in contents)
            {
                if (content.Item != null && content.Amount > 0)
                {
                    yield return new ItemAmount(content.Item, content.Amount * unpackMultiplier);
                }
            }
        }

        private void OnValidate()
        {
            bundleId = bundleId?.Trim();
            displayName = displayName?.Trim();
            unpackMultiplier = Mathf.Max(1, unpackMultiplier);
        }
    }
}
