using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    public abstract class ItemBase : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private ItemCategory category;
        [SerializeField, Range(0, 2)] private int processingLevel;
        [SerializeField, Min(1)] private int maxStack = 99;
        [SerializeField] private GameObject worldPrefab;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemCategory Category => category;
        public int ProcessingLevel => processingLevel;
        public int MaxStack => maxStack;
        public GameObject WorldPrefab => worldPrefab;

        protected virtual void OnValidate()
        {
            itemId = itemId?.Trim();
            displayName = displayName?.Trim();
            processingLevel = Mathf.Clamp(processingLevel, 0, 2);
            maxStack = Mathf.Max(1, maxStack);
        }
    }
}
