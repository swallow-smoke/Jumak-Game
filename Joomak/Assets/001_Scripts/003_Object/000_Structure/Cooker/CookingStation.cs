using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._005_Data._000_Item;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Cooker
{
    public sealed class CookingStation : BaseStructure, IItemContainerOwner
    {
        [SerializeField] private CookingStationType stationType;
        [SerializeField] private List<RecipeData> recipes = new();
        [SerializeField] private InventoryModel ingredientInventory = new();
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
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            if (carrier.HeldItem is WorldItem heldItem)
            {
                if (heldItem.Item != null && ingredientInventory.TryAdd(heldItem.Item, 1))
                {
                    carrier.TryConsumeHeldItem(heldItem);
                }

                return;
            }

            if (TryGiveOutput(carrier) || IsCooking)
            {
                return;
            }

            foreach (RecipeData recipe in recipes)
            {
                if (TryStartCooking(recipe, ingredientInventory))
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

        private bool TryGiveOutput(ISingleItemCarrier carrier)
        {
            if (outputInventory.Stacks.Count == 0)
            {
                return false;
            }

            ItemStack stack = outputInventory.Stacks[0];
            if (!WorldItem.TryCreate(stack.Item, transform.position, out WorldItem worldItem))
            {
                return false;
            }

            if (carrier.TryCarry(worldItem))
            {
                outputInventory.TryRemove(stack.Item, 1);
                return true;
            }

            Destroy(worldItem.gameObject);
            return false;
        }

        private void OnValidate()
        {
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
