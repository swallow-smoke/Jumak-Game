using System.Text;
using _001_Scripts._005_Data._000_Item;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    // 레시피 카드: 위쪽엔 완성 요리 이미지, 아래쪽엔 필요한 재료를 "- 이름 xN"으로 나열한다.
    // 소환되면(OnEnable) 살짝 아래로 내려간 자리에서 원래 자리로 elastic하게 튀어 올라온다.
    [RequireComponent(typeof(RectTransform))]
    public sealed class Poster : MonoBehaviour
    {
        [SerializeField] private Image resultImage;
        [SerializeField] private Text foodNameText;
        [SerializeField] private Text stationNameText;
        [SerializeField] private Text ingredientsText;

        [Header("Spawn Animation")]
        [SerializeField, Min(0f)] private float spawnDropDistance = 40f;
        [SerializeField, Min(0f)] private float spawnDuration = 0.6f;

        private RectTransform rectTransform;
        private Tween spawnTween;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            EnsureHeaderTexts();
        }

        private void OnEnable()
        {
            PlaySpawnAnimation();
        }

        private void OnDisable()
        {
            spawnTween?.Kill();
        }

        private void PlaySpawnAnimation()
        {
            Vector2 restPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = restPosition + new Vector2(0f, -spawnDropDistance);

            spawnTween?.Kill();
            spawnTween = rectTransform
                .DOAnchorPos(restPosition, spawnDuration)
                .SetEase(Ease.OutElastic);
        }

        public void Show(RecipeData recipe)
        {
            if (recipe == null)
            {
                Clear();
                return;
            }

            SetResultImage(recipe.Result.Item);
            SetHeaderTexts(recipe);
            SetIngredientsText(recipe);
        }

        public void Clear()
        {
            if (resultImage != null)
            {
                resultImage.sprite = null;
                resultImage.enabled = false;
            }

            if (ingredientsText != null)
            {
                ingredientsText.text = string.Empty;
            }

            if (foodNameText != null)
            {
                foodNameText.text = string.Empty;
            }

            if (stationNameText != null)
            {
                stationNameText.text = string.Empty;
            }
        }

        private void SetHeaderTexts(RecipeData recipe)
        {
            ItemBase result = recipe.Result.Item;
            if (foodNameText != null)
            {
                foodNameText.text = result != null && !string.IsNullOrWhiteSpace(result.DisplayName)
                    ? result.DisplayName
                    : recipe.name;
            }

            if (stationNameText != null)
            {
                stationNameText.text = $"제작: {recipe.StationDisplayName}";
            }
        }

        private void EnsureHeaderTexts()
        {
            Font font = ingredientsText != null
                ? ingredientsText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            foodNameText ??= CreateHeaderText("Food Name", font, 25, FontStyle.Bold,
                new Vector2(0f, 251f), new Vector2(268f, 42f), new Color32(73, 39, 23, 255));
            stationNameText ??= CreateHeaderText("Station Name", font, 17, FontStyle.Bold,
                new Vector2(0f, 216f), new Vector2(268f, 32f), new Color32(145, 82, 43, 255));
        }

        private Text CreateHeaderText(string objectName, Font font, int fontSize, FontStyle style,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.one * 0.5f;
            rect.anchorMax = Vector2.one * 0.5f;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void SetResultImage(ItemBase result)
        {
            if (resultImage == null)
            {
                return;
            }

            Sprite sprite = GetDisplaySprite(result);
            resultImage.sprite = sprite;
            resultImage.enabled = sprite != null;
        }

        private void SetIngredientsText(RecipeData recipe)
        {
            if (ingredientsText == null)
            {
                return;
            }

            StringBuilder builder = new();
            foreach (ItemAmount ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null)
                {
                    continue;
                }

                builder.AppendLine($"- {ingredient.Item.DisplayName} x{ingredient.Amount}");
            }

            ingredientsText.text = builder.ToString().TrimEnd();
        }

        // SupplyBox/ServingCounter와 같은 방식: 아이템 전용 스프라이트 필드가 없어서
        // worldPrefab에 붙은 스프라이트를 그대로 가져다 쓴다.
        private static Sprite GetDisplaySprite(ItemBase item)
        {
            return item != null && item.WorldPrefab != null
                ? item.WorldPrefab.GetComponentInChildren<SpriteRenderer>()?.sprite
                : null;
        }
    }
}
