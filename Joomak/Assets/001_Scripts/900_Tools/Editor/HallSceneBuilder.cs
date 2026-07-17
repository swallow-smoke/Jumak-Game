using System.Collections.Generic;
using System.IO;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._001_Entity.Item;
using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._005_Data._000_Item;
using _001_Scripts._006_Debug;
using _001_Scripts._002_Controller;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _001_Scripts._900_Tools.Editor
{
    // 홀 테스트용 에셋/프리팹/씬을 통째로 생성한다. 몇 번을 실행해도 같은 결과가 나온다.
    // 그래픽은 전부 Visual 자식 오브젝트에 몰려 있으므로, 아트가 나오면 그쪽 스프라이트만 교체하면 된다.
    public static class HallSceneBuilder
    {
        private const string ImageFolder = "Assets/002_Resources/000_Images";
        private const string ItemFolder = "Assets/002_Resources/001_Datas";
        private const string PrefabFolder = "Assets/002_Resources/004_Prefabs";
        private const string SpritePath = ImageFolder + "/Placeholder_Square.png";
        private const string DatabasePath = ItemFolder + "/ItemDatabase.asset";
        private const string ScenePath = "Assets/000_Scenes/HallTest.unity";

        private static readonly Color PlateColor = new(0.85f, 0.85f, 0.9f);

        [MenuItem("Joomak/Hall/Build Hall Test Scene")]
        public static void BuildAll()
        {
            EnsureFolder(ImageFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(PrefabFolder);

            Sprite sprite = CreatePlaceholderSprite();

            List<ItemBase> allItems = new();
            ItemBase plate = CreateItem("plate", "접시", ItemCategory.Plate, 0);
            allItems.Add(plate);

            List<ItemBase> menu = new();
            foreach (DishDef dish in Dishes)
            {
                ItemBase item = CreateItem(dish.Id, dish.DisplayName, ItemCategory.Dish, dish.Level);
                menu.Add(item);
                allItems.Add(item);
            }

            // 아이템마다 프리팹을 따로 둔다. 아트가 나오면 각 프리팹의 Visual만 교체하면 된다.
            LinkWorldPrefab(plate, sprite, PlateColor);
            for (int i = 0; i < menu.Count; i++)
            {
                LinkWorldPrefab(menu[i], sprite, Dishes[i].Color);
            }

            CreateDatabase(allItems);
            CreateCustomerPrefab(sprite);
            CreatePlayerPrefab(sprite);

            AssetDatabase.SaveAssets();
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HallSceneBuilder] 완료. 씬: {ScenePath}");
        }

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // NewScene이 안 쓰는 에셋을 언로드하면서 위에서 만든 ScriptableObject 참조가 죽는다.
            // 그래서 씬을 연 "뒤에" 경로로 다시 읽어야 한다. 이 순서를 바꾸면 참조가 전부 null로 박힌다.
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            ItemDB database = AssetDatabase.LoadAssetAtPath<ItemDB>(DatabasePath);
            ItemBase plate = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/plate.asset");
            GameObject customerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Customer.prefab");
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Player_Hall.prefab");

            List<ItemBase> menu = new();
            foreach (DishDef dish in Dishes)
            {
                menu.Add(AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemFolder}/{dish.Id}.asset"));
            }

            AssertLoaded(sprite, database, plate, customerPrefab, playerPrefab, menu);

            // Full HD 16:9 / 100 PPU 기준 월드는 19.2 x 10.8. 왼쪽이 주방, 오른쪽이 홀.
            Camera camera = Camera.main;
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.11f, 0.09f, 0.08f);

            CreateSpriteObject("Floor_Kitchen", new Vector3(-4.8f, 0f, 0f), new Vector2(9.6f, 10.8f), new Color(0.22f, 0.19f, 0.17f), sprite, -20);
            CreateSpriteObject("Floor_Hall", new Vector3(4.8f, 0f, 0f), new Vector2(9.6f, 10.8f), new Color(0.30f, 0.24f, 0.19f), sprite, -20);
            CreateSpriteObject("Divider_RedLine", Vector3.zero, new Vector2(0.08f, 10.8f), new Color(0.8f, 0.15f, 0.15f), sprite, -10);

            GameObject counterObject = CreateSpriteObject("ServingCounter", new Vector3(0.75f, 0f, 0f), new Vector2(0.9f, 2.4f), new Color(0.55f, 0.38f, 0.22f), sprite, 1);
            BoxCollider2D counterCollider = counterObject.AddComponent<BoxCollider2D>();
            counterCollider.size = new Vector2(0.9f, 2.4f);
            ServingCounter servingCounter = counterObject.AddComponent<ServingCounter>();
            SerializedObject counterData = new(servingCounter);
            counterData.FindProperty("structureId").stringValue = "ServingCounter";
            counterData.ApplyModifiedPropertiesWithoutUndo();

            Vector3[] tablePositions = { new(3.4f, 2.6f, 0f), new(3.4f, -2.6f, 0f), new(6.9f, 0f, 0f) };
            List<DiningTable> tables = new();
            for (int i = 0; i < tablePositions.Length; i++)
            {
                tables.Add(CreateTable($"DiningTable_{i}", tablePositions[i], sprite, plate));
            }

            CustomerEntrance entrance = CreateEntrance();

            GameObject reputationObject = new("ReputationManager");
            reputationObject.AddComponent<ReputationManager>();

            GameObject hallObject = new("HallManager");
            HallManager hallManager = hallObject.AddComponent<HallManager>();
            SerializedObject hallData = new(hallManager);
            hallData.FindProperty("customerPrefab").objectReferenceValue = customerPrefab.GetComponent<Customer>();
            hallData.FindProperty("entrance").objectReferenceValue = entrance;
            hallData.FindProperty("spawnInterval").floatValue = 8f;
            SetObjectArray(hallData.FindProperty("tables"), tables);
            SetObjectArray(hallData.FindProperty("menu"), menu);
            hallData.ApplyModifiedPropertiesWithoutUndo();

            GameObject stubObject = new("KitchenStub (Debug)");
            KitchenStub stub = stubObject.AddComponent<KitchenStub>();
            SerializedObject stubData = new(stub);
            stubData.FindProperty("itemDatabase").objectReferenceValue = database;
            stubData.FindProperty("servingCounter").objectReferenceValue = servingCounter;
            stubData.FindProperty("cookSeconds").floatValue = 5f;
            stubData.ApplyModifiedPropertiesWithoutUndo();

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(4.5f, -1f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static DiningTable CreateTable(string name, Vector3 position, Sprite sprite, ItemBase plate)
        {
            GameObject tableObject = CreateSpriteObject(name, position, new Vector2(1.3f, 1.3f), new Color(0.62f, 0.45f, 0.28f), sprite, 1);
            BoxCollider2D collider = tableObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.3f, 1.3f);

            DiningTable table = tableObject.AddComponent<DiningTable>();

            List<Seat> seats = new();
            for (int i = 0; i < 2; i++)
            {
                GameObject seatObject = new($"Seat_{i}");
                seatObject.transform.SetParent(tableObject.transform, false);
                seatObject.transform.localPosition = new Vector3(i == 0 ? -1.05f : 1.05f, 0f, 0f);
                seats.Add(seatObject.AddComponent<Seat>());
            }

            SerializedObject tableData = new(table);
            tableData.FindProperty("plateItem").objectReferenceValue = plate;
            SetObjectArray(tableData.FindProperty("seats"), seats);
            tableData.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private static CustomerEntrance CreateEntrance()
        {
            GameObject entranceObject = new("CustomerEntrance");
            CustomerEntrance entrance = entranceObject.AddComponent<CustomerEntrance>();

            Transform spawn = CreateChildPoint(entranceObject.transform, "SpawnPoint", new Vector3(9.3f, -4.9f, 0f));
            Transform exit = CreateChildPoint(entranceObject.transform, "ExitPoint", new Vector3(9.3f, -4.9f, 0f));

            List<Transform> waitingSpots = new();
            for (int i = 0; i < 3; i++)
            {
                waitingSpots.Add(CreateChildPoint(entranceObject.transform, $"WaitingSpot_{i}", new Vector3(8.5f, -3.6f + i * 1.2f, 0f)));
            }

            SerializedObject entranceData = new(entrance);
            entranceData.FindProperty("spawnPoint").objectReferenceValue = spawn;
            entranceData.FindProperty("exitPoint").objectReferenceValue = exit;
            SetObjectArray(entranceData.FindProperty("waitingSpots"), waitingSpots);
            entranceData.ApplyModifiedPropertiesWithoutUndo();
            return entrance;
        }

        private static GameObject CreateCustomerPrefab(Sprite sprite)
        {
            GameObject root = new("Customer");
            AddVisual(root.transform, sprite, new Color(0.85f, 0.5f, 0.35f), new Vector2(0.7f, 0.9f), 5);

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true;

            root.AddComponent<Customer>();

            string path = $"{PrefabFolder}/Customer.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(Sprite sprite)
        {
            GameObject root = new("Player_Hall");
            AddVisual(root.transform, sprite, new Color(0.95f, 0.62f, 0.15f), new Vector2(0.7f, 0.9f), 10);

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            Transform hand = CreateChildPoint(root.transform, "HandPoint", new Vector3(0f, 0.6f, 0f));

            // PlayerController가 RequireComponent로 요구하므로 먼저 붙여야 한다.
            SingleItemCarrier carrier = root.AddComponent<SingleItemCarrier>();
            SerializedObject carrierData = new(carrier);
            carrierData.FindProperty("handPoint").objectReferenceValue = hand;
            carrierData.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CustomerEscort>();

            // 기획서 2번: P2 홀 담당은 방향키 + L
            PlayerController controller = root.AddComponent<PlayerController>();
            SerializedObject controllerData = new(controller);
            controllerData.FindProperty("moveUpKey").intValue = (int)Key.UpArrow;
            controllerData.FindProperty("moveDownKey").intValue = (int)Key.DownArrow;
            controllerData.FindProperty("moveLeftKey").intValue = (int)Key.LeftArrow;
            controllerData.FindProperty("moveRightKey").intValue = (int)Key.RightArrow;
            controllerData.FindProperty("interactKey").intValue = (int)Key.L;
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabFolder}/Player_Hall.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void LinkWorldPrefab(ItemBase item, Sprite sprite, Color color)
        {
            GameObject root = new(item.ItemId);
            AddVisual(root.transform, sprite, color, new Vector2(0.45f, 0.45f), 20);

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.25f;
            collider.isTrigger = true;

            WorldItem worldItem = root.AddComponent<WorldItem>();
            SerializedObject worldItemData = new(worldItem);
            worldItemData.FindProperty("item").objectReferenceValue = item;
            worldItemData.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabFolder}/Item_{item.ItemId}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            SerializedObject itemData = new(item);
            itemData.FindProperty("worldPrefab").objectReferenceValue = prefab;
            itemData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemData CreateItem(string id, string displayName, ItemCategory category, int level)
        {
            string path = $"{ItemFolder}/{id}.asset";
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
            data.FindProperty("processingLevel").intValue = level;
            data.FindProperty("maxStack").intValue = 99;
            data.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static ItemDB CreateDatabase(List<ItemBase> items)
        {
            ItemDB database = AssetDatabase.LoadAssetAtPath<ItemDB>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDB>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            SerializedObject data = new(database);
            SetObjectArray(data.FindProperty("items"), items);
            data.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static Sprite CreatePlaceholderSprite()
        {
            if (!File.Exists(SpritePath))
            {
                // 100px @ 100PPU = 정확히 1x1 월드 유닛. 스케일 = 원하는 크기가 된다.
                Texture2D texture = new(100, 100, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[100 * 100];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static GameObject CreateSpriteObject(string name, Vector3 position, Vector2 size, Color color, Sprite sprite, int sortingOrder)
        {
            GameObject root = new(name);
            root.transform.position = position;
            AddVisual(root.transform, sprite, color, size, sortingOrder);
            return root;
        }

        // 로직은 루트, 그래픽은 Visual 자식. 아트 교체 시 이 자식만 손대면 된다.
        private static void AddVisual(Transform parent, Sprite sprite, Color color, Vector2 size, int sortingOrder)
        {
            GameObject visual = new("Visual");
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private static Transform CreateChildPoint(Transform parent, string name, Vector3 localPosition)
        {
            GameObject point = new(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            return point.transform;
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                {
                    throw new System.InvalidOperationException($"{property.propertyPath}[{i}]에 null을 넣으려 한다. 에셋이 언로드됐는지 확인할 것.");
                }

                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        // 참조가 null인 채로 씬이 저장되면 손님이 주문할 요리를 못 받는 식으로 조용히 망가진다. 여기서 잡는다.
        private static void AssertLoaded(
            Sprite sprite,
            ItemDB database,
            ItemBase plate,
            GameObject customerPrefab,
            GameObject playerPrefab,
            IReadOnlyList<ItemBase> menu)
        {
            Require(sprite, nameof(sprite));
            Require(database, nameof(database));
            Require(plate, nameof(plate));
            Require(customerPrefab, nameof(customerPrefab));
            Require(playerPrefab, nameof(playerPrefab));

            for (int i = 0; i < menu.Count; i++)
            {
                Require(menu[i], $"menu[{i}] ({Dishes[i].Id})");
            }
        }

        private static void Require(Object asset, string label)
        {
            if (asset == null)
            {
                throw new System.InvalidOperationException($"[HallSceneBuilder] 에셋을 못 읽었다: {label}");
            }
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

        // 기획서 5-2 요리 목록
        private static readonly DishDef[] Dishes =
        {
            new("rice", "쌀밥", 1, new Color(0.95f, 0.95f, 0.88f)),
            new("pickled_radish", "무짠지", 1, new Color(0.95f, 0.85f, 0.45f)),
            new("boiled_meat", "삶은 고기", 1, new Color(0.72f, 0.42f, 0.36f)),
            new("kimchi", "김치", 1, new Color(0.85f, 0.25f, 0.18f)),
            new("soybean_soup", "된장국", 2, new Color(0.72f, 0.60f, 0.35f)),
            new("bindaetteok", "빈대떡", 2, new Color(0.88f, 0.72f, 0.35f)),
            new("gukbap", "국밥", 2, new Color(0.80f, 0.55f, 0.40f)),
            new("sikhye", "식혜", 1, new Color(0.90f, 0.80f, 0.60f))
        };

        private readonly struct DishDef
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly int Level;
            public readonly Color Color;

            public DishDef(string id, string displayName, int level, Color color)
            {
                Id = id;
                DisplayName = displayName;
                Level = level;
                Color = color;
            }
        }
    }
}
