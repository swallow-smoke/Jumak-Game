using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _001_Scripts._900_Tools.Editor
{
    // 홀 오브젝트를 씬에 배치한다. 씬을 새로 만들지 않고 기존 씬을 열어 추가만 한다.
    //
    // 좌표 기준 (씬 실측):
    //   칠해진 바닥  = 월드 x -36.8 ~ 6.1, y -3.1 ~ 21.5
    //   홀/주방 경계 = x -15.3 의 Border 벽 (폭 1). 원점(0)이 아니다.
    //   홀           = x -36.8 ~ -15.8   /  주방 = x -14.8 ~ 6.1
    //   ServingCounter = (-15.25, 3.22), 세로 6 -> y 0.22 ~ 6.22
    //   타일 한 칸   = 1.5333 유닛
    public static class HallScenePlacer
    {
        private const float Tile = 1.5333f;
        private const string ScenePath = "Assets/000_Scenes/SampleScene.unity";
        private const string TablePrefabPath = "Assets/003_Prefabs/Hall/DiningTable.prefab";
        private const string CustomerPrefabPath = "Assets/003_Prefabs/Hall/Customer.prefab";
        private const string PlayerPrefabPath = "Assets/003_Prefabs/Hall/Player_Hall.prefab";
        private const string ItemFolder = "Assets/002_Resources/001_Datas";
        private const string TableRootName = "Hall_Tables";
        private const string HallRootName = "Hall_System";

        // 최종 형태는 가로 2 x 세로 3 = 6개. 지금은 1행(카운터 쪽)에 2개만 놓는다.
        //
        // 열 좌표를 이렇게 잡은 이유:
        //   좌석이 테이블 좌우 ±2.3에 붙으므로 한 테이블이 가로로 약 4.6을 먹는다.
        //   - 오른쪽 열(-21.47): 오른쪽 좌석이 -19.17 -> 카운터(-15.75)까지 3.4 여유. 지나다닐 수 있다.
        //   - 왼쪽 열(-30.67):  왼쪽 좌석이 -32.97 -> 바닥 끝(-36.8)까지 3.8 여유.
        //   - 두 열 사이 통로 4.6.
        // 전부 타일 크기(1.5333)의 배수라 격자에 딱 맞는다.
        private static readonly float[] ColumnCells = { -20f, -14f };

        // 1행의 2개만 활성 상태로 시작한다. 2행/3행의 4개는 테이블 추가 구매마다 순서대로 열린다.
        private static readonly float[] RowCells = { 2f, 7f, 12f };

        // 손님이 실제로 등장하려면 매니저 + 입구 + 플레이어가 씬에 있어야 한다.
        // 테이블 배치와 따로 돌릴 수 있게 메뉴를 나눠둔다.
        [MenuItem("Joomak/Hall/Place Hall Managers And Player")]
        public static void PlaceManagersAndPlayer()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject old = GameObject.Find(HallRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            GameObject root = new(HallRootName);

            CustomerEntrance entrance = BuildEntrance(root.transform);
            BuildManagers(root.transform, entrance);
            BuildPlayer(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HallScenePlacer] 매니저/입구/플레이어 배치 완료 ({HallRootName})");
        }

        // 입구는 홀 왼쪽 위. 손님이 여기 생겨 대기하다가 아래쪽 테이블로 안내된다.
        private static CustomerEntrance BuildEntrance(Transform parent)
        {
            GameObject host = new("CustomerEntrance");
            host.transform.SetParent(parent, false);
            CustomerEntrance entrance = host.AddComponent<CustomerEntrance>();

            Transform spawn = CreateChild(host.transform, "SpawnPoint", new Vector3(-34.5f, 18.4f, 0f));
            Transform exit = CreateChild(host.transform, "ExitPoint", new Vector3(-34.5f, 18.4f, 0f));

            List<Transform> waiting = new()
            {
                CreateChild(host.transform, "WaitingSpot_0", new Vector3(-31.5f, 18.4f, 0f)),
                CreateChild(host.transform, "WaitingSpot_1", new Vector3(-31.5f, 15.3f, 0f)),
                CreateChild(host.transform, "WaitingSpot_2", new Vector3(-31.5f, 12.3f, 0f))
            };

            SerializedObject data = new(entrance);
            data.FindProperty("spawnPoint").objectReferenceValue = spawn;
            data.FindProperty("exitPoint").objectReferenceValue = exit;
            SetObjectArray(data.FindProperty("waitingSpots"), waiting);
            data.ApplyModifiedPropertiesWithoutUndo();
            return entrance;
        }

        private static void BuildManagers(Transform parent, CustomerEntrance entrance)
        {
            // 손님 방치/이벤트 실패 시 명성을 깎는다. 없으면 페널티가 조용히 무시된다.
            CreateChild(parent, "ReputationManager", Vector3.zero).gameObject.AddComponent<ReputationManager>();

            // 손님이 식사를 마치고 낸 전이 여기 쌓인다. 없으면 계산이 조용히 무시된다.
            CreateChild(parent, "GameManager", Vector3.zero).gameObject.AddComponent<GameManager>();

            GameObject hallHost = CreateChild(parent, "HallManager", Vector3.zero).gameObject;
            HallManager hall = hallHost.AddComponent<HallManager>();

            Customer customerPrefab = AssetDatabase
                .LoadAssetAtPath<GameObject>(CustomerPrefabPath)?.GetComponent<Customer>();
            Require(customerPrefab, "Customer.prefab");

            SerializedObject data = new(hall);
            SetObjectArray(data.FindProperty("customerPrefabs"), new[] { customerPrefab });
            data.FindProperty("entrance").objectReferenceValue = entrance;

            // 기획서 9번 1라운드는 20초지만, 지금은 손님이 오는 걸 눈으로 보려고 짧게 둔다.
            data.FindProperty("spawnInterval").floatValue = 8f;

            SetObjectArray(data.FindProperty("tables"), FindPlacedTables());
            SetObjectArray(data.FindProperty("menu"), LoadDishes());
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildPlayer(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Require(prefab, "Player_Hall.prefab");

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            player.transform.position = new Vector3(-25f, 8f, 0f);
        }

        // 이미 씬에 놓인 테이블을 이름 순서대로 집어온다. 그 순서가 곧 잠금 해제 순서다.
        private static List<DiningTable> FindPlacedTables()
        {
            List<DiningTable> tables = new();
            GameObject root = GameObject.Find(TableRootName);
            if (root == null)
            {
                Debug.LogWarning($"[HallScenePlacer] {TableRootName}이 없다. 테이블 배치를 먼저 실행할 것.");
                return tables;
            }

            foreach (Transform child in root.transform)
            {
                if (child.TryGetComponent(out DiningTable table))
                {
                    tables.Add(table);
                }
            }

            return tables;
        }

        // 손님이 주문할 수 있는 건 Dish 카테고리뿐이다. 재료나 접시는 메뉴에 오르면 안 된다.
        private static List<ItemBase> LoadDishes()
        {
            List<ItemBase> dishes = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { ItemFolder }))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && item.Category == ItemCategory.Dish)
                {
                    dishes.Add(item);
                }
            }

            Debug.Log($"[HallScenePlacer] 메뉴에 요리 {dishes.Count}종 등록");
            return dishes;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 position)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            return child.transform;
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void Require(Object asset, string label)
        {
            if (asset == null)
            {
                throw new System.InvalidOperationException($"[HallScenePlacer] 에셋을 못 읽었다: {label}");
            }
        }

        [MenuItem("Joomak/Hall/Place Hall Tables")]
        public static void PlaceTables()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HallScenePlacer] 테이블 프리팹이 없다: {TablePrefabPath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 여러 번 실행해도 쌓이지 않도록 이전 것을 지우고 다시 놓는다.
            GameObject root = GameObject.Find(TableRootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            root = new GameObject(TableRootName);

            List<DiningTable> placed = new();
            foreach (float rowCell in RowCells)
            {
                foreach (float columnCell in ColumnCells)
                {
                    Vector3 position = new(columnCell * Tile, rowCell * Tile, 0f);

                    GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                    table.name = $"DiningTable_{placed.Count}";
                    table.transform.position = position;

                    placed.Add(table.GetComponent<DiningTable>());
                    Debug.Log($"[HallScenePlacer] {table.name} @ ({position.x:F2}, {position.y:F2})");
                }
            }

            HallManager hall = Object.FindAnyObjectByType<HallManager>();
            if (hall != null)
            {
                SerializedObject hallData = new(hall);
                SetObjectArray(hallData.FindProperty("tables"), placed);
                hallData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hall);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[HallScenePlacer] 테이블 {placed.Count}개 배치 완료. " +
                      $"HallManager를 씬에 넣고 tables 리스트에 {TableRootName} 자식들을 순서대로 연결할 것.");
        }
    }
}
