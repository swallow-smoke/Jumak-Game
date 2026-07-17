using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._005_Data._000_Item;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    // 열려 있는 씬의 HallManager.menu를 요리 목록으로 채운다.
    // 씬의 다른 연결(손님 프리팹, 입구, 테이블 등)은 절대 건드리지 않는다.
    //
    // 손님이 안 나오던 원인: menu가 비어 있으면 TrySpawnCustomer가 즉시 return한다.
    public static class HallMenuFiller
    {
        private const string ItemFolder = "Assets/002_Resources/001_Datas";

        // 손님이 주문하면 안 되는 요리. 실패한 요리는 Dish로 분류돼 있어도 메뉴에서 뺀다.
        private static readonly HashSet<string> Excluded = new() { "failed_dish" };

        [MenuItem("Joomak/Hall/Fill HallManager Menu")]
        public static void FillMenu()
        {
            HallManager hall = Object.FindAnyObjectByType<HallManager>();
            if (hall == null)
            {
                Debug.LogError("[HallMenuFiller] 열린 씬에 HallManager가 없습니다. 씬을 먼저 여세요.");
                return;
            }

            List<ItemBase> dishes = LoadOrderableDishes();
            if (dishes.Count == 0)
            {
                Debug.LogError("[HallMenuFiller] Dish 카테고리 요리를 찾지 못했습니다.");
                return;
            }

            SerializedObject so = new(hall);
            SerializedProperty menu = so.FindProperty("menu");
            menu.arraySize = dishes.Count;
            for (int i = 0; i < dishes.Count; i++)
            {
                menu.GetArrayElementAtIndex(i).objectReferenceValue = dishes[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hall);
            EditorSceneManager.MarkSceneDirty(hall.gameObject.scene);

            Debug.Log($"[HallMenuFiller] menu에 요리 {dishes.Count}종 등록. 씬을 저장(Ctrl+S)하세요.");
        }

        private static List<ItemBase> LoadOrderableDishes()
        {
            List<ItemBase> dishes = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { ItemFolder }))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && item.Category == ItemCategory.Dish && !Excluded.Contains(item.ItemId))
                {
                    dishes.Add(item);
                }
            }

            return dishes;
        }
    }
}
