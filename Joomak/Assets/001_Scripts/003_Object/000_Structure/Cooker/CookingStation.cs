using System.Collections.Generic;
using System.Text;
using _001_Scripts._002_Controller;
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
    public sealed class CookingStation : BaseStructure, IItemContainerOwner, ISelectionInputCapture
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

        [Header("Recipe Bubble")]
        [Tooltip("플레이어가 이 반경 안에 들어오면 말풍선을 띄운다. 나타날 때 Scale 연출은 프리팹의 ScaleRevealAnimator가 담당한다.")]
        [SerializeField, Min(0f)] private float bubbleShowRadius = 2.5f;
        [SerializeField] private GameObject recipeBubbleRoot;
        [SerializeField] private TMP_Text recipeBubbleText;
        private UnityEngine.UI.Text recipeCanvasText;
        [SerializeField] private Vector3 bubbleWorldOffset = new(0f, 1.45f, 0f);
        [SerializeField, Min(0.1f)] private float bubbleWorldScale = 1.25f;

        private static readonly Collider2D[] BubbleProximityHits = new Collider2D[4];

        private static readonly ContactFilter2D ProximityFilter = new()
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false
        };

        [Header("Cooking Particle")]
        [Tooltip("조리 중일 때(State.Cooking) 재생되는 파티클. 힘쓰는 느낌용.")]
        [SerializeField] private ParticleSystem cookingParticle;

        private State state = State.Idle;
        private RecipeData selectedRecipe;
        private int selectingIndex;
        private float remainingCookTime;
        private float readyElapsedSeconds;
        private bool outputIsFailed;
        private bool bubbleVisible;
        private bool particlePlaying;
        private PlayerController bubblePlayer;
        private GameObject selectingInteractor;

        public InventoryModel Inventory => outputInventory;
        public bool IsCooking => state == State.Cooking;
        public bool IsSelectionActive => state == State.Selecting;
        public float RemainingCookTime => remainingCookTime;

        protected override void Awake()
        {
            base.Awake();

            if (recipeBubbleRoot != null)
            {
                recipeBubbleRoot.transform.position = transform.position + bubbleWorldOffset;
                recipeBubbleRoot.transform.localScale = Vector3.one * bubbleWorldScale;

                TMP_Text polishedText = recipeBubbleRoot.GetComponentInChildren<TMP_Text>(true);
                if (polishedText != null)
                {
                    recipeBubbleText = polishedText;
                }

                recipeCanvasText = recipeBubbleRoot.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (recipeCanvasText != null)
                {
                    recipeBubbleText = null;
                }

                ConfigureBubbleSorting();
                recipeBubbleRoot.SetActive(false);
            }

            // 구형 조리대 프리팹에 있던 Screen Space 텍스트는 말풍선과 위치가 따로 놀기 때문에 숨긴다.
            if (recipeLabel != null)
            {
                recipeLabel.gameObject.SetActive(false);
            }
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

            UpdateBubble();
            UpdateCookingParticle();
        }

        private void OnDisable()
        {
            if (state == State.Selecting)
            {
                selectingInteractor = null;
                state = State.Idle;
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
                    ConfirmSelection(interactor);
                    break;

                case State.Idle:
                    EnterSelectionMode(interactor);
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

        private void EnterSelectionMode(GameObject interactor)
        {
            if (recipes.Count == 0)
            {
                return;
            }

            selectingIndex = Mathf.Max(0, selectedRecipe != null ? recipes.IndexOf(selectedRecipe) : 0);
            selectingInteractor = interactor;
            state = State.Selecting;
        }

        public bool CanControlSelection(GameObject interactor) =>
            state == State.Selecting && selectingInteractor == interactor;

        public void ConfirmSelection(GameObject interactor)
        {
            if (!CanControlSelection(interactor) || recipes.Count == 0)
            {
                return;
            }

            selectedRecipe = recipes[selectingIndex];
            selectingInteractor = null;
            state = State.Idle;
        }

        public void CancelSelection(GameObject interactor)
        {
            if (CanControlSelection(interactor))
            {
                selectingInteractor = null;
                state = State.Idle;
            }
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

        // 선택 중이면 지금 스크롤로 보고 있는 후보를, 아니면 확정된 레시피를 보여준다.
        private RecipeData BubbleRecipe => state == State.Selecting && recipes.Count > 0
            ? recipes[selectingIndex]
            : selectedRecipe;

        // 플레이어가 가까이 오면 뜬다. 등장 연출(Scale 0->1)은 recipeBubbleRoot 프리팹에 붙은
        // ScaleRevealAnimator가 SetActive(true) 시점에 알아서 재생하므로 여기선 켜고 끄기만 한다.
        private void UpdateBubble()
        {
            bool shouldShow = recipeBubbleRoot != null && IsPlayerNearby();

            if (shouldShow != bubbleVisible)
            {
                bubbleVisible = shouldShow;
                if (recipeBubbleRoot != null)
                {
                    recipeBubbleRoot.SetActive(bubbleVisible);
                }
            }

            if (bubbleVisible)
            {
                string bubbleText = BuildBubbleText();
                if (recipeBubbleText != null)
                {
                    recipeBubbleText.text = bubbleText;
                }

                if (recipeCanvasText != null)
                {
                    recipeCanvasText.text = bubbleText;
                }
            }
        }

        private string BuildBubbleText()
        {
            RecipeData recipe = BubbleRecipe;
            string key = bubblePlayer != null ? bubblePlayer.InteractKeyLabel : "E";

            return state switch
            {
                State.Selecting => recipe != null
                    ? $"<color=#9B5A2E><size=70%>레시피 선택</size></color>\n" +
                      $"<b>{GetRecipeName(recipe)}</b>\n" +
                      $"<size=52%><color=#6F6256>위/아래 변경  ·  {key} 확정  ·  ESC 취소</color></size>"
                    : "<b>레시피 없음</b>",

                State.Cooking =>
                    "<color=#C85B32><size=70%>맛있게 조리 중</size></color>\n" +
                    $"<b>{GetRecipeName(selectedRecipe)}</b>\n" +
                    $"<size=60%><color=#6F6256>{Mathf.CeilToInt(remainingCookTime)}초  ·  {key} 가속</color></size>",

                State.Ready => outputIsFailed
                    ? $"<color=#A33A32><b>요리 실패</b></color>\n<size=60%>{key} 치우기</size>"
                    : $"<color=#3E7D4C><b>완성!</b></color>\n{GetRecipeName(selectedRecipe)}\n" +
                      $"<size=60%><color=#6F6256>{key} 가져가기</color></size>",

                _ => recipe == null
                    ? "<color=#9B5A2E><b>조리대</b></color>\n" +
                      $"<size=62%><color=#6F6256>{key} 레시피 선택</color></size>"
                    : $"<color=#9B5A2E><b>{GetRecipeName(recipe)}</b></color>\n" +
                      $"<size=55%>{BuildIngredientSummary(recipe)}</size>\n" +
                      $"<size=52%><color=#6F6256>재료를 들고 {key}</color></size>"
            };
        }

        private static string GetRecipeName(RecipeData recipe)
        {
            if (recipe == null)
            {
                return "요리";
            }

            ItemBase result = recipe.Result.Item;
            return result != null && !string.IsNullOrWhiteSpace(result.DisplayName)
                ? result.DisplayName
                : recipe.RecipeId;
        }

        private string BuildIngredientSummary(RecipeData recipe)
        {
            StringBuilder builder = new();
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                ItemAmount ingredient = recipe.Ingredients[i];
                if (ingredient.Item == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("  ·  ");
                }

                string name = string.IsNullOrWhiteSpace(ingredient.Item.DisplayName)
                    ? ingredient.Item.ItemId
                    : ingredient.Item.DisplayName;
                int current = ingredientInventory.CountOf(ingredient.Item);
                builder.Append(name).Append(' ').Append(current).Append('/').Append(ingredient.Amount);
            }

            return builder.Length > 0 ? builder.ToString() : "기본 재료";
        }

        private void ConfigureBubbleSorting()
        {
            SpriteRenderer background = recipeBubbleRoot.GetComponent<SpriteRenderer>();
            if (background == null)
            {
                return;
            }

            const int bubbleSortingOrder = 1000;
            background.sortingOrder = bubbleSortingOrder;

            Transform shadowTransform = recipeBubbleRoot.transform.Find("BubbleShadow");
            if (shadowTransform != null && shadowTransform.TryGetComponent(out SpriteRenderer shadow))
            {
                shadow.sortingLayerID = background.sortingLayerID;
                shadow.sortingOrder = bubbleSortingOrder - 1;
            }

            if (recipeBubbleText is TextMeshPro worldText && worldText.TryGetComponent(out MeshRenderer textRenderer))
            {
                RectTransform textTransform = worldText.rectTransform;
                textTransform.localPosition = new Vector3(0.6f, 0.35f, -0.2f);
                textTransform.localScale = Vector3.one * 0.04f;
                textTransform.sizeDelta = new Vector2(34f, 13f);

                worldText.enableAutoSizing = true;
                worldText.fontSizeMin = 12f;
                worldText.fontSizeMax = 24f;
                worldText.alignment = TextAlignmentOptions.Center;
                worldText.overflowMode = TextOverflowModes.Overflow;
                worldText.color = new Color32(61, 39, 29, 255);

                textRenderer.sortingLayerID = background.sortingLayerID;
                textRenderer.sortingOrder = bubbleSortingOrder + 1;
                textRenderer.enabled = true;
                worldText.ForceMeshUpdate();
            }
            else if (recipeBubbleText is TextMeshProUGUI canvasText)
            {
                Canvas canvas = canvasText.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingLayerID = background.sortingLayerID;
                    canvas.sortingOrder = bubbleSortingOrder + 1;
                }

                canvasText.enableAutoSizing = true;
                canvasText.fontSizeMin = 12f;
                canvasText.fontSizeMax = 26f;
                canvasText.alignment = TextAlignmentOptions.Center;
                canvasText.overflowMode = TextOverflowModes.Overflow;
                canvasText.color = new Color32(61, 39, 29, 255);
                canvasText.ForceMeshUpdate();
            }

            if (recipeCanvasText != null)
            {
                Canvas canvas = recipeCanvasText.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingLayerID = background.sortingLayerID;
                    canvas.sortingOrder = bubbleSortingOrder + 1;
                }

                recipeCanvasText.color = new Color32(61, 39, 29, 255);
                recipeCanvasText.resizeTextForBestFit = true;
                recipeCanvasText.resizeTextMinSize = 18;
                recipeCanvasText.resizeTextMaxSize = 38;
                recipeCanvasText.horizontalOverflow = HorizontalWrapMode.Wrap;
                recipeCanvasText.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private bool IsPlayerNearby()
        {
            bubblePlayer = null;
            int hitCount = Physics2D.OverlapCircle(transform.position, bubbleShowRadius, ProximityFilter, BubbleProximityHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = BubbleProximityHits[i];
                if (hit != null && hit.GetComponentInParent<ISingleItemCarrier>() != null)
                {
                    bubblePlayer = hit.GetComponentInParent<PlayerController>();
                    return true;
                }
            }

            return false;
        }

        // 조리 중일 때만 재생. 힘쓰는 느낌을 주는 파티클(불꽃, 김 등)이라 State.Cooking 동안만 튼다.
        private void UpdateCookingParticle()
        {
            if (cookingParticle == null)
            {
                return;
            }

            bool shouldPlay = state == State.Cooking;
            if (shouldPlay == particlePlaying)
            {
                return;
            }

            particlePlaying = shouldPlay;
            if (particlePlaying)
            {
                cookingParticle.Play();
            }
            else
            {
                cookingParticle.Stop();
            }
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
