using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._005_Data._000_Item
{
    // dishId(완성 요리의 ItemId) -> RecipeData 조회. ItemDB와 같은 구조.
    // OrderPanel처럼 "이 요리는 무슨 레시피로 만드나"를 알아야 하는 UI가 쓴다.
    [CreateAssetMenu(menuName = "Joomak/Items/Recipe Database", fileName = "RecipeDatabase")]
    public sealed class RecipeDB : ScriptableObject
    {
        [SerializeField] private List<RecipeData> recipes = new();

        private Dictionary<string, RecipeData> recipeByDishId;

        public IReadOnlyList<RecipeData> Recipes => recipes;

        public bool TryGetByDishId(string dishId, out RecipeData recipe)
        {
            if (string.IsNullOrWhiteSpace(dishId))
            {
                recipe = null;
                return false;
            }

            BuildIndexIfNeeded();
            return recipeByDishId.TryGetValue(dishId, out recipe);
        }

        private void OnEnable()
        {
            recipeByDishId = null;
        }

        private void OnValidate()
        {
            recipeByDishId = null;
        }

        private void BuildIndexIfNeeded()
        {
            if (recipeByDishId != null)
            {
                return;
            }

            recipeByDishId = new Dictionary<string, RecipeData>();
            foreach (RecipeData recipe in recipes)
            {
                ItemBase result = recipe != null ? recipe.Result.Item : null;
                if (result == null || string.IsNullOrWhiteSpace(result.ItemId))
                {
                    continue;
                }

                if (!recipeByDishId.TryAdd(result.ItemId, recipe))
                {
                    Debug.LogWarning($"Duplicate recipe result dish id: {result.ItemId}", this);
                }
            }
        }
    }
}
