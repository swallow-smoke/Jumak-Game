using _001_Scripts._001_Manager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _001_Scripts._900_Tools.Editor
{
    // 먼지(청소 이벤트) 두 가지를 고친다.
    //   1) 스폰 영역(trashArea)을 홀 안(x < -15.3)으로 제한한다. 옛 값은 주방까지 넘어갔다.
    //   2) dust 프리팹을 Interaction 레이어(7)로 올린다. Default(0)면 빗자루 스윙에 안 맞아 못 치운다.
    public static class DustFixer
    {
        private const string ScenePath = "Assets/000_Scenes/InGame.unity";
        private const string DustPrefabPath = "Assets/003_Prefabs/dust.prefab";

        // 홀 바닥(x -36.8 ~ -15.3, y -3.1 ~ 21.5) 안쪽. xMax(-17)이 경계(-15.3)를 넘지 않는다.
        private static readonly Rect HallTrashArea = new(-35f, -2f, 18f, 21f);

        [MenuItem("Joomak/Hall/Fix Dust (Trash)")]
        public static void Fix()
        {
            FixDustPrefabLayer();
            FixTrashArea();
        }

        private static void FixDustPrefabLayer()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(DustPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[DustFixer] 먼지 프리팹을 못 엶: {DustPrefabPath}");
                return;
            }

            try
            {
                int layer = LayerMask.NameToLayer("Interaction");
                if (layer >= 0)
                {
                    SetLayerRecursive(root, layer);
                    PrefabUtility.SaveAsPrefabAsset(root, DustPrefabPath);
                    Debug.Log($"[DustFixer] dust 프리팹 레이어 -> Interaction({layer})");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void FixTrashArea()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EventManager eventManager = Object.FindAnyObjectByType<EventManager>();
            if (eventManager == null)
            {
                Debug.LogError($"[DustFixer] {ScenePath}에 EventManager가 없습니다.");
                return;
            }

            SerializedObject so = new(eventManager);
            SerializedProperty area = so.FindProperty("trashArea");
            area.rectValue = HallTrashArea;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(eventManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DustFixer] trashArea -> {HallTrashArea} (홀 전용) + 저장 완료.");
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
