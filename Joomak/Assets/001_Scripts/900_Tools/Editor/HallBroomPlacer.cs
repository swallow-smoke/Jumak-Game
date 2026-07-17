using _001_Scripts._003_Object._001_Entity.Item;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _001_Scripts._900_Tools.Editor
{
    // 기획서 3번/8-1: 빗자루를 홀 구역 왼쪽 최하단에 아이템으로 배치한다.
    // 이미 만들어진 broomstick.prefab(WorldItem)을 씬에 놓기만 한다.
    public static class HallBroomPlacer
    {
        private const string ScenePath = "Assets/000_Scenes/InGame.unity";
        private const string BroomPrefabPath = "Assets/003_Prefabs/broomstick.prefab";
        private const string BroomName = "Broomstick";

        // 홀 바닥(x -36.8 ~ -15.3, y -3.1 ~ 21.5)의 왼쪽 최하단, 모서리에서 조금 안쪽.
        private static readonly Vector3 SpawnPosition = new(-34f, -1.5f, 0f);

        [MenuItem("Joomak/Hall/Place Broom")]
        public static void PlaceBroom()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BroomPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HallBroomPlacer] 빗자루 프리팹이 없습니다: {BroomPrefabPath}");
                return;
            }

            // 배치모드에서는 씬이 자동으로 안 열린다. 에디터에서 이미 열려 있으면 그대로 쓴다.
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 여러 번 눌러도 빗자루가 늘어나지 않게 이전 것을 지운다.
            GameObject old = GameObject.Find(BroomName);
            if (old != null && old.GetComponent<WorldItem>() != null)
            {
                Object.DestroyImmediate(old);
            }

            GameObject broom = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            broom.name = BroomName;
            broom.transform.position = SpawnPosition;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HallBroomPlacer] 빗자루 배치 @ {SpawnPosition} + 저장 완료.");
        }
    }
}
