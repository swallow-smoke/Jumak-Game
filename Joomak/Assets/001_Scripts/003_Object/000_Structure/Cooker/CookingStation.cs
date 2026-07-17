using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Cooker
{
    public sealed class CookingStation : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private CookingStationType stationType;
        [SerializeField] private List<RecipeData> recipes = new();
        [SerializeField] private InventoryModel outputInventory = new();

        private RecipeData activeRecipe;
        private float remainingCookTime;

        public InventoryModel Inventory => outputInventory;
        public bool IsCooking => activeRecipe != null;
        public float RemainingCookTime => remainingCookTime;

        private void Update()
        {
            if (activeRecipe == null)
            {
                return;
            }

            remainingCookTime -= Time.deltaTime;
            if (remainingCookTime <= 0f)
            {
                CompleteCooking();
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out IItemContainerOwner carrier))
            {
                return;
            }

            if (outputInventory.TryTransferFirstTo(carrier.Inventory) || IsCooking)
            {
                return;
            }

            foreach (RecipeData recipe in recipes)
            {
                if (TryStartCooking(recipe, carrier.Inventory))
                {
                    return;
                }
            }
        }

        public bool TryStartCooking(RecipeData recipe, InventoryModel ingredients)
        {
            if (IsCooking || recipe == null || recipe.StationType != stationType || !recipes.Contains(recipe))
            {
                return false;
            }

            ItemAmount result = recipe.Result;
            if (!outputInventory.CanAdd(result.Item, result.Amount) || !recipe.TryConsumeIngredients(ingredients))
            {
                return false;
            }

            activeRecipe = recipe;
            remainingCookTime = recipe.CookTime;
            return true;
        }

        private void CompleteCooking()
        {
            ItemAmount result = activeRecipe.Result;
            outputInventory.TryAdd(result.Item, result.Amount);
            activeRecipe = null;
            remainingCookTime = 0f;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            foreach (RecipeData recipe in recipes)
            {
                if (recipe != null && recipe.StationType != stationType)
                {
                    Debug.LogWarning($"Recipe {recipe.name} belongs to {recipe.StationType}.", this);
                }
            }
        }
    }
}
