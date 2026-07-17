using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Upgrade;
using TMPro;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Cooker
{
    // 조리 흐름: 빈손 상호작용으로 레시피 선택 모드 진입 -> 스크롤로 후보 순환 -> 빈손 상호작용으로 확정.
    // 확정된 레시피의 재료를 손에 들고 상호작용하면 주입되고, 다 모이면 자동으로 조리가 시작된다.
    // 조리 중 상호작용하면 레시피에 정해진 만큼 시간이 깎인다.
    // 완성 후 failTimeoutSeconds 동안 아무도 안 가져가면 결과물이 실패작으로 바뀐다.
    public sealed class CookingStation : BaseStructure, IItemContainerOwner, IScrollSelectable
    {
        private enum State
        {
            Idle,
            Selecting,
            Cooking,
            Ready
        }

        [SerializeField] private CookingStationType stationType;
        [SerializeField] private List<RecipeData> recipes = new();
        [SerializeField] private InventoryModel ingredientInventory = new();
        [SerializeField] private InventoryModel outputInventory = new();
        [SerializeField] private ItemBase failedDishItem;
        [SerializeField, Min(0f)] private float failTimeoutSeconds = 5f;
        [SerializeField] private TextMeshProUGUI recipeLabel;

        private State state = State.Idle;
        private RecipeData selectedRecipe;
        private int selectingIndex;
        private float remainingCookTime;
        private float readyElapsedSeconds;
        private bool outputIsFailed;

        public InventoryModel Inventory => outputInventory;
        public bool IsCooking => state == State.Cooking;
        public float RemainingCookTime => remainingCookTime;

        protected override void Awake()
        {
            base.Awake();
            UpdateLabel();
        }

        private void Update()
        {
            switch (state)
            {
                case State.Cooking:
                    remainingCookTime -= Time.deltaTime;
                    if (remainingCookTime <= 0f)
                    {
                        CompleteCooking();
                    }

                    break;

                case State.Ready when !outputIsFailed:
                    readyElapsedSeconds += Time.deltaTime;
                    if (readyElapsedSeconds >= failTimeoutSeconds * FailureDelayMultiplier)
                    {
                        TurnOutputIntoFailedDish();
                    }

                    break;
            }

            UpdateLabel();
        }

        public override void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out ISingleItemCarrier carrier))
            {
                return;
            }

            if (carrier.HeldItem is WorldItem heldItem)
            {
                TryInjectIngredient(carrier, heldItem);
                return;
            }

            switch (state)
            {
                case State.Ready:
                    TryGiveOutput(carrier);
                    break;

                case State.Cooking:
                    ReduceCookTime();
                    break;

                case State.Selecting:
                    ConfirmSelection();
                    break;

                case State.Idle:
                    EnterSelectionMode();
                    break;
            }
        }

        // 레시피 선택 모드에서만 의미가 있다. PlayerController가 포커스 중일 때 스크롤 입력을 그대로 전달해준다.
        public void Scroll(int direction)
        {
            if (state != State.Selecting || recipes.Count == 0)
            {
                return;
            }

            selectingIndex = (selectingIndex + direction + recipes.Count) % recipes.Count;
        }

        private void EnterSelectionMode()
        {
            if (recipes.Count == 0)
            {
                return;
            }

            selectingIndex = Mathf.Max(0, selectedRecipe != null ? recipes.IndexOf(selectedRecipe) : 0);
            state = State.Selecting;
        }

        private void ConfirmSelection()
        {
            selectedRecipe = recipes[selectingIndex];
            state = State.Idle;
        }

        private void TryInjectIngredient(ISingleItemCarrier carrier, WorldItem heldItem)
        {
            if (selectedRecipe == null || state != State.Idle || heldItem.Item == null || !NeedsMoreOf(heldItem.Item))
            {
                return;
            }

            if (!ingredientInventory.TryAdd(heldItem.Item, 1))
            {
                return;
            }

            carrier.TryConsumeHeldItem(heldItem);

            if (selectedRecipe.TryConsumeIngredients(ingredientInventory))
            {
                remainingCookTime = selectedRecipe.CookTime * CookTimeMultiplier;
                state = State.Cooking;
            }
        }

        private bool NeedsMoreOf(ItemBase item)
        {
            foreach (ItemAmount ingredient in selectedRecipe.Ingredients)
            {
                if (ingredient.Item == item && ingredientInventory.CountOf(item) < ingredient.Amount)
                {
                    return true;
                }
            }

            return false;
        }

        private void ReduceCookTime()
        {
            remainingCookTime -= selectedRecipe.InteractReduceSeconds;
        }

        private static float CookTimeMultiplier
        {
            get
            {
                RunState runState = RunState.Instance;
                if (runState == null)
                {
                    return 1f;
                }

                int level = runState.GetLevel(UpgradeId.CookTime1)
                            + runState.GetLevel(UpgradeId.CookTime2)
                            + runState.GetLevel(UpgradeId.CookTime3);
                return Mathf.Max(0.7f, 1f - Mathf.Clamp(level, 0, 3) * 0.1f);
            }
        }

        private static float FailureDelayMultiplier
        {
            get
            {
                RunState runState = RunState.Instance;
                if (runState == null)
                {
                    return 1f;
                }

                int level = runState.GetLevel(UpgradeId.FailureDelay1)
                            + runState.GetLevel(UpgradeId.FailureDelay2)
                            + runState.GetLevel(UpgradeId.FailureDelay3);
                return 1f + Mathf.Clamp(level, 0, 3) * 0.1f;
            }
        }

        private void CompleteCooking()
        {
            ItemAmount result = selectedRecipe.Result;
            outputInventory.TryAdd(result.Item, result.Amount);
            remainingCookTime = 0f;
            readyElapsedSeconds = 0f;
            outputIsFailed = false;
            state = State.Ready;
        }

        private void TurnOutputIntoFailedDish()
        {
            if (failedDishItem == null || outputInventory.Stacks.Count == 0)
            {
                return;
            }

            ItemStack stack = outputInventory.Stacks[0];
            outputInventory.TryRemove(stack.Item, stack.Amount);
            outputInventory.TryAdd(failedDishItem, 1);
            outputIsFailed = true;
        }

        private void TryGiveOutput(ISingleItemCarrier carrier)
        {
            if (outputInventory.Stacks.Count == 0)
            {
                return;
            }

            ItemStack stack = outputInventory.Stacks[0];
            if (!WorldItem.TryCreate(stack.Item, transform.position, out WorldItem worldItem))
            {
                return;
            }

            if (!carrier.TryCarry(worldItem))
            {
                Destroy(worldItem.gameObject);
                return;
            }

            outputInventory.TryRemove(stack.Item, 1);
            outputIsFailed = false;
            state = State.Idle;
        }

        private void UpdateLabel()
        {
            if (recipeLabel == null)
            {
                return;
            }

            recipeLabel.text = state switch
            {
                State.Selecting => recipes.Count > 0 ? $"> {recipes[selectingIndex].RecipeId}" : "레시피 없음",
                State.Cooking => $"{selectedRecipe.RecipeId} ({Mathf.CeilToInt(remainingCookTime)}s)",
                State.Ready => outputIsFailed ? "실패작" : "완성!",
                _ => selectedRecipe != null ? selectedRecipe.RecipeId : "레시피 선택 필요"
            };
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
