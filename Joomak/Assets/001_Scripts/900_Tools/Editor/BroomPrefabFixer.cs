using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._005_Data._000_Item;
using UnityEditor;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    // 빗자루 프리팹의 두 가지 집기 문제를 고친다.
    //   1) WorldItem.item 이 비어 있어(=null) 집어도 빗자루(Tool)로 인식되지 않는다.
    //   2) 콜라이더가 얇은 비트리거 막대(0.24 높이)라, 플레이어 중심에서 수평으로 나가는
    //      상호작용 레이캐스트가 위로 지나가 놓친다. 다른 집는 아이템처럼 넉넉한 트리거로 바꾼다.
    public static class BroomPrefabFixer
    {
        private const string BroomPrefabPath = "Assets/003_Prefabs/broomstick.prefab";
        private const string BroomItemPath = "Assets/002_Resources/001_Datas/broom_stick.asset";

        [MenuItem("Joomak/Hall/Fix Broom Prefab")]
        public static void Fix()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BroomPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[BroomPrefabFixer] 프리팹을 못 엶: {BroomPrefabPath}");
                return;
            }

            try
            {
                WorldItem worldItem = root.GetComponent<WorldItem>();
                if (worldItem == null)
                {
                    Debug.LogError("[BroomPrefabFixer] WorldItem 컴포넌트가 없습니다.");
                    return;
                }

                ItemBase broomItem = AssetDatabase.LoadAssetAtPath<ItemBase>(BroomItemPath);
                if (broomItem == null)
                {
                    Debug.LogError($"[BroomPrefabFixer] 빗자루 ItemData를 못 찾음: {BroomItemPath}");
                    return;
                }

                // 1) item 연결
                SerializedObject so = new(worldItem);
                so.FindProperty("item").objectReferenceValue = broomItem;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 2) 콜라이더를 넉넉한 트리거 원으로.
                // RequireComponent(Collider2D) 때문에 기존 것을 먼저 지우면 예외가 나므로,
                // 새 콜라이더를 먼저 붙이고 나서 나머지를 치운다.
                CircleCollider2D pickup = root.AddComponent<CircleCollider2D>();
                pickup.radius = 0.6f;
                pickup.isTrigger = true;

                foreach (Collider2D old in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (old != pickup)
                    {
                        Object.DestroyImmediate(old);
                    }
                }

                // 3) 진짜 원인: 플레이어 상호작용 레이캐스트는 Interaction 레이어만 본다.
                // 빗자루가 Default(0) 레이어라 아예 감지되지 않았다.
                int layer = LayerMask.NameToLayer("Interaction");
                if (layer >= 0)
                {
                    SetLayerRecursive(root, layer);
                }

                PrefabUtility.SaveAsPrefabAsset(root, BroomPrefabPath);
                Debug.Log($"[BroomPrefabFixer] 완료: item={broomItem.name}, 트리거 원(r=0.6), 레이어=Interaction({layer}).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
