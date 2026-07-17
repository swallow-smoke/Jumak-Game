using System.IO;
using _001_Scripts._005_Data._000_Item;
using UnityEditor;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    // 국밥/쌀밥/된장국/빈대떡/삶은 고기/김치/무짠지/막걸리 레시피를 SO로 만든다.
    // 재료·요리 ItemData는 절반 정도 Assets/002_Resources/001_Datas/Food에 이미 있어서 그건 로드만 하고,
    // 없는 것(마늘, 누룩, 막걸리)만 새로 만든다. 몇 번을 실행해도 같은 결과가 나온다.
    public static class RecipeDataBuilder
    {
        private const string FoodFolder = "Assets/002_Resources/001_Datas/Food";
        private const string RecipeFolder = "Assets/002_Resources/001_Datas/Recipe";

        [MenuItem("Joomak/Kitchen/Build Recipe Data")]
        public static void BuildAll()
        {
            EnsureFolder(RecipeFolder);

            // 이미 있는 재료
            ItemData rice = LoadFoodItem("rice");
            ItemData meat = LoadFoodItem("raw_meat");
            ItemData radish = LoadFoodItem("radish");
            ItemData beanPaste = LoadFoodItem("bean_paste");
            ItemData greenBean = LoadFoodItem("green_been");
            ItemData cabbage = LoadFoodItem("cabbage");

            // 없어서 새로 만드는 재료
            ItemData garlic = CreateFoodItem("garlic", "마늘", ItemCategory.Ingredient);
            ItemData nuruk = CreateFoodItem("nuruk", "누룩", ItemCategory.Ingredient);

            // 이미 있는 요리 결과물
            ItemData cookedRice = LoadFoodItem("cooked_rice");
            ItemData beanSoup = LoadFoodItem("bean_soup");
            ItemData riceCake = LoadFoodItem("rice_cake");
            ItemData boiledMeat = LoadFoodItem("bolied_meat");
            ItemData kimchi = LoadFoodItem("kimchi");
            ItemData cookedRadish = LoadFoodItem("cooked_radish");
            ItemData gukbab = FixGukbabStub();

            // 없어서 새로 만드는 요리
            ItemData makgeolli = CreateFoodItem("makgeolli", "막걸리", ItemCategory.Dish);

            CreateRecipe("gukbab_recipe", CookingStationType.Cauldron, gukbab, 6f,
                (rice, 1), (meat, 1), (radish, 1));
            CreateRecipe("cooked_rice_recipe", CookingStationType.Cauldron, cookedRice, 3f,
                (rice, 1));
            CreateRecipe("bean_soup_recipe", CookingStationType.Cauldron, beanSoup, 4f,
                (beanPaste, 1), (radish, 1));
            CreateRecipe("rice_cake_recipe", CookingStationType.Griddle, riceCake, 5f,
                (greenBean, 2), (meat, 1));
            CreateRecipe("boiled_meat_recipe", CookingStationType.Cauldron, boiledMeat, 5f,
                (meat, 2), (beanPaste, 1));
            CreateRecipe("kimchi_recipe", CookingStationType.PicklingTable, kimchi, 4f,
                (cabbage, 2), (garlic, 1));
            CreateRecipe("cooked_radish_recipe", CookingStationType.PicklingTable, cookedRadish, 3f,
                (radish, 2));
            CreateRecipe("makgeolli_recipe", CookingStationType.FermentationJar, makgeolli, 8f,
                (rice, 2), (nuruk, 1));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RecipeDataBuilder] 완료.");
        }

        // gukbab itemId는 이미 result_food_test.asset에 들어있다 (displayName/category만 스텁 상태).
        private static ItemData FixGukbabStub()
        {
            ItemData item = LoadFoodItem("result_food_test");
            SerializedObject data = new(item);
            data.FindProperty("displayName").stringValue = "국밥";
            data.FindProperty("category").intValue = (int)ItemCategory.Dish;
            data.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static void CreateRecipe(
            string id, CookingStationType stationType, ItemData result, float cookTime,
            params (ItemData item, int amount)[] ingredients)
        {
            string path = $"{RecipeFolder}/{id}.asset";
            RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeData>();
                AssetDatabase.CreateAsset(recipe, path);
            }

            SerializedObject data = new(recipe);
            data.FindProperty("recipeId").stringValue = id;
            data.FindProperty("stationType").enumValueIndex = (int)stationType;
            data.FindProperty("cookTime").floatValue = cookTime;

            SerializedProperty resultProp = data.FindProperty("result");
            resultProp.FindPropertyRelative("item").objectReferenceValue = result;
            resultProp.FindPropertyRelative("amount").intValue = 1;

            SerializedProperty ingredientsProp = data.FindProperty("ingredients");
            ingredientsProp.arraySize = ingredients.Length;
            for (int i = 0; i < ingredients.Length; i++)
            {
                SerializedProperty element = ingredientsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = ingredients[i].item;
                element.FindPropertyRelative("amount").intValue = ingredients[i].amount;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemData LoadFoodItem(string fileName)
        {
            string path = $"{FoodFolder}/{fileName}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                throw new System.InvalidOperationException($"[RecipeDataBuilder] 기존 아이템 애셋을 못 찾음: {path}");
            }

            return item;
        }

        private static ItemData CreateFoodItem(string id, string displayName, ItemCategory category)
        {
            string path = $"{FoodFolder}/{id}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject data = new(item);
            data.FindProperty("itemId").stringValue = id;
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("category").intValue = (int)category;
            data.FindProperty("maxStack").intValue = 99;
            data.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
