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
        [SerializeField] private Text ingredientsText;

        [Header("Spawn Animation")]
        [SerializeField, Min(0f)] private float spawnDropDistance = 40f;
        [SerializeField, Min(0f)] private float spawnDuration = 0.6f;

        private RectTransform rectTransform;
        private Tween spawnTween;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
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
