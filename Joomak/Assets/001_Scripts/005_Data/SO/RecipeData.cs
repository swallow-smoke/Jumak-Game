using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Inventory;
using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    [CreateAssetMenu(menuName = "Joomak/Items/Recipe", fileName = "Recipe")]
    public sealed class RecipeData : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private CookingStationType stationType;
        [SerializeField] private List<ItemAmount> ingredients = new();
        [SerializeField] private ItemAmount result;
        [SerializeField, Min(0.1f)] private float cookTime = 3f;

        public string RecipeId => recipeId;
        public CookingStationType StationType => stationType;
        public IReadOnlyList<ItemAmount> Ingredients => ingredients;
        public ItemAmount Result => result;
        public float CookTime => cookTime;

        public bool CanCraft(InventoryModel inventory)
        {
            if (inventory == null || result.Item == null || result.Item.ProcessingLevel > 2)
            {
                return false;
            }

            foreach (KeyValuePair<ItemBase, int> requirement in BuildRequirements())
            {
                if (!inventory.Contains(requirement.Key, requirement.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryConsumeIngredients(InventoryModel inventory)
        {
            if (!CanCraft(inventory))
            {
                return false;
            }

            foreach (KeyValuePair<ItemBase, int> requirement in BuildRequirements())
            {
                inventory.TryRemove(requirement.Key, requirement.Value);
            }

            return true;
        }

        private Dictionary<ItemBase, int> BuildRequirements()
        {
            Dictionary<ItemBase, int> requirements = new();
            foreach (ItemAmount ingredient in ingredients)
            {
                if (ingredient.Item == null || ingredient.Amount <= 0)
                {
                    continue;
                }

                requirements.TryGetValue(ingredient.Item, out int currentAmount);
                requirements[ingredient.Item] = currentAmount + ingredient.Amount;
            }

            return requirements;
        }

        private void OnValidate()
        {
            recipeId = recipeId?.Trim();
            cookTime = Mathf.Max(0.1f, cookTime);

            if (result.Item != null && result.Item.ProcessingLevel > 2)
            {
                Debug.LogError($"Recipe {name} exceeds the two-step processing rule.", this);
            }
        }
    }
}
