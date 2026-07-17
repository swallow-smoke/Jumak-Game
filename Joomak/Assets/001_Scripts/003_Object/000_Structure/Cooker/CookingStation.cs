using System.Collections.Generic;
using System.Text;
using _001_Scripts._001_Manager;
using _001_Scripts._002_Controller;
using _001_Scripts._003_Object._000_Structure.Interface;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.Item.Interface;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Text recipeLabel;

        [Header("Recipe Bubble")]
        [Tooltip("플레이어가 이 반경 안에 들어오면 말풍선을 띄운다. 나타날 때 Scale 연출은 프리팹의 ScaleRevealAnimator가 담당한다.")]
        [SerializeField, Min(0f)] private float bubbleShowRadius = 2.5f;
        [SerializeField] private GameObject recipeBubbleRoot;
        [SerializeField] private Text recipeBubbleText;
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

        [Header("Audio")]
        [Tooltip("조리대에서 레시피 선택, 재료 투입, 조리 가속, 완성품 수령에 성공했을 때 재생합니다.")]
        [SerializeField] private AudioClip interactionSfx;
        [Tooltip("실제로 조리 중인 동안 반복 재생합니다.")]
        [SerializeField] private AudioClip cookingLoopSfx;
        [SerializeField, Range(0f, 1f)] private float cookingLoopVolume = 0.75f;
        [SerializeField] private AudioSource cookingAudioSource;

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

            interactionSfx ??= Resources.Load<AudioClip>("006_Audio/interactionSound");
            cookingLoopSfx ??= Resources.Load<AudioClip>("006_Audio/grill_sound");
            EnsureCookingParticle();

            if (cookingAudioSource == null)
            {
                cookingAudioSource = gameObject.AddComponent<AudioSource>();
            }

            cookingAudioSource.playOnAwake = false;
            cookingAudioSource.loop = true;
            cookingAudioSource.spatialBlend = 0f;

            if (recipeBubbleRoot != null)
            {
                recipeBubbleRoot.transform.position = transform.position + bubbleWorldOffset;
                recipeBubbleRoot.transform.localScale = Vector3.one * bubbleWorldScale;

                recipeBubbleText = recipeBubbleRoot.GetComponentInChildren<Text>(true);

                ConfigureBubbleSorting();
                recipeBubbleRoot.SetActive(false);
            }

            // 구형 조리대 프리팹에 있던 Screen Space 텍스트는 말풍선과 위치가 따로 놀기 때문에 숨긴다.
            if (recipeLabel != null)
            {
                recipeLabel.gameObject.SetActive(false);
            }
        }

        private void EnsureCookingParticle()
        {
            if (cookingParticle != null)
            {
                return;
            }

            GameObject particleObject = new("Cooking Particle");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.localPosition = new Vector3(0f, 0.55f, -0.15f);

            cookingParticle = particleObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = cookingParticle.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.maxParticles = 48;
            main.startColor = stationType == CookingStationType.Griddle
                ? new ParticleSystem.MinMaxGradient(new Color32(255, 119, 35, 220), new Color32(255, 220, 120, 200))
                : new ParticleSystem.MinMaxGradient(new Color32(255, 250, 235, 190), new Color32(190, 205, 210, 145));

            ParticleSystem.EmissionModule emission = cookingParticle.emission;
            emission.rateOverTime = stationType == CookingStationType.Griddle ? 14f : 9f;

            ParticleSystem.ShapeModule shape = cookingParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = stationType == CookingStationType.Griddle ? 0.34f : 0.24f;
            shape.radiusThickness = 1f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = cookingParticle.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.82f, 0.62f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.16f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = fade;

            ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sortingOrder = 120;
            cookingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            UpdateCookingAudio();
        }

        private void OnDisable()
        {
            if (cookingAudioSource != null)
            {
                cookingAudioSource.Stop();
            }

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
                    PlayInteractionSfx();
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
            PlayInteractionSfx();
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
            PlayInteractionSfx();
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

            if (!carrier.TryConsumeHeldItem(heldItem))
            {
                ingredientInventory.TryRemove(heldItem.Item, 1);
                return;
            }

            PlayInteractionSfx();

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
            get => UpgradeApi.CookTimeMultiplier;
        }

        private static float FailureDelayMultiplier
        {
            get => UpgradeApi.FailureDelayMultiplier;
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
            PlayInteractionSfx();
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

            }
        }

        private string BuildBubbleText()
        {
            RecipeData recipe = BubbleRecipe;
            string key = bubblePlayer != null ? bubblePlayer.InteractKeyLabel : "E";

            return state switch
            {
                State.Selecting => recipe != null
                    ? $"<color=#9B5A2E>레시피 선택</color>\n" +
                      $"<b>{GetRecipeName(recipe)}</b>\n" +
                      $"<color=#6F6256>위/아래 변경  ·  {key} 확정  ·  ESC 취소</color>"
                    : "<b>레시피 없음</b>",

                State.Cooking =>
                    "<color=#C85B32>맛있게 조리 중</color>\n" +
                    $"<b>{GetRecipeName(selectedRecipe)}</b>\n" +
                    $"<color=#6F6256>{Mathf.CeilToInt(remainingCookTime)}초  ·  {key} 가속</color>",

                State.Ready => outputIsFailed
                    ? $"<color=#A33A32><b>요리 실패</b></color>\n{key} 치우기"
                    : $"<color=#3E7D4C><b>완성!</b></color>\n{GetRecipeName(selectedRecipe)}\n" +
                      $"<color=#6F6256>{key} 가져가기</color>",

                _ => recipe == null
                    ? "<color=#9B5A2E><b>조리대</b></color>\n" +
                      $"<color=#6F6256>{key} 레시피 선택</color>"
                    : $"<color=#9B5A2E><b>{GetRecipeName(recipe)}</b></color>\n" +
                      $"{BuildIngredientSummary(recipe)}\n" +
                      $"<color=#6F6256>재료를 들고 {key}</color>"
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

            if (recipeBubbleText != null)
            {
                Canvas canvas = recipeBubbleText.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    canvas.overrideSorting = true;
                    canvas.sortingLayerID = background.sortingLayerID;
                    canvas.sortingOrder = bubbleSortingOrder + 1;
                }

                recipeBubbleText.color = new Color32(61, 39, 29, 255);
                recipeBubbleText.resizeTextForBestFit = true;
                recipeBubbleText.resizeTextMinSize = 18;
                recipeBubbleText.resizeTextMaxSize = 38;
                recipeBubbleText.alignment = TextAnchor.MiddleCenter;
                recipeBubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                recipeBubbleText.verticalOverflow = VerticalWrapMode.Truncate;
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

        private void UpdateCookingAudio()
        {
            if (cookingAudioSource == null)
            {
                return;
            }

            bool shouldPlay = state == State.Cooking && cookingLoopSfx != null;
            if (!shouldPlay)
            {
                if (cookingAudioSource.isPlaying)
                {
                    cookingAudioSource.Stop();
                }

                return;
            }

            cookingAudioSource.volume = (AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f)
                                        * cookingLoopVolume;

            if (cookingAudioSource.clip != cookingLoopSfx)
            {
                cookingAudioSource.Stop();
                cookingAudioSource.clip = cookingLoopSfx;
            }

            if (!cookingAudioSource.isPlaying)
            {
                cookingAudioSource.Play();
            }
        }

        private void PlayInteractionSfx()
        {
            AudioManager.Instance?.PlaySfx(interactionSfx);
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
