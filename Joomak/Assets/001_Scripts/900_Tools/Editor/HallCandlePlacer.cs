using System.Collections.Generic;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _001_Scripts._900_Tools.Editor
{
    // 기획서 8번 촛불 관리 이벤트용. 촛불 프리팹(작은 노란 직사각형 플레이스홀더)을 만들고
    // 열린 씬 홀 사이드에 6개 배치한 뒤 EventManager.candles에 연결한다.
    // 아트가 나오면 프리팹의 Visual 스프라이트만 갈아끼우면 된다.
    public static class HallCandlePlacer
    {
        private const string LightOnPath = "Assets/002_Resources/000_Images/Structure/Light_On.png";
        private const string LightOffPath = "Assets/002_Resources/000_Images/Structure/Light_Off.png";
        private const string CandlePrefabPath = "Assets/003_Prefabs/Hall/Candle.prefab";
        private const string CandleRootName = "Hall_Candles";
        private const string ScenePath = "Assets/000_Scenes/InGame.unity";

        // 기획서 8번: 촛불 6개. 홀(x -36.8 ~ -15.3) 사이드에 위/가운데 두 줄로 흩어 놓는다.
        // 테이블(y≈3)·서빙카운터(x-15,y0~6)와 겹치지 않는 자리. 플레이스홀더 좌표라 이동해도 된다.
        private static readonly Vector2[] Positions =
        {
            new(-34f, 17f), new(-26f, 17f), new(-18f, 17f),
            new(-34f, 9f),  new(-26f, 9f),  new(-18f, 9f)
        };

        [MenuItem("Joomak/Hall/Place Candles")]
        public static void PlaceCandles()
        {
            GameObject prefab = LoadOrCreatePrefab();
            if (prefab == null)
            {
                return;
            }

            // 배치모드에서는 씬이 자동으로 열리지 않으므로 명시적으로 연다.
            // (에디터에서 이미 그 씬을 열어둔 상태라면 그대로 쓰인다.)
            if (Object.FindAnyObjectByType<EventManager>() == null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EventManager eventManager = Object.FindAnyObjectByType<EventManager>();
            if (eventManager == null)
            {
                Debug.LogError($"[HallCandlePlacer] {ScenePath}에 EventManager가 없습니다.");
                return;
            }

            // 여러 번 눌러도 쌓이지 않게 이전 것을 지우고 다시 놓는다.
            GameObject old = GameObject.Find(CandleRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            GameObject root = new(CandleRootName);
            List<Candle> candles = new();
            foreach (Vector2 pos in Positions)
            {
                GameObject candle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                candle.name = $"Candle_{candles.Count}";
                candle.transform.position = new Vector3(pos.x, pos.y, 0f);
                candles.Add(candle.GetComponent<Candle>());
            }

            SerializedObject so = new(eventManager);
            SerializedProperty prop = so.FindProperty("candles");
            prop.arraySize = candles.Count;
            for (int i = 0; i < candles.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = candles[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(eventManager);

            UnityEngine.SceneManagement.Scene scene = eventManager.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[HallCandlePlacer] 촛불 {candles.Count}개 배치 + EventManager 연결 + 저장 완료.");
        }

        // 프리팹을 매번 새로 만들어 덮어쓴다(같은 경로 = GUID 유지). 스프라이트가 바뀌어도 갱신된다.
        private static GameObject LoadOrCreatePrefab()
        {
            Sprite litSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LightOnPath);
            Sprite unlitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LightOffPath);
            if (litSprite == null || unlitSprite == null)
            {
                Debug.LogError($"[HallCandlePlacer] 촛불 스프라이트를 못 찾음: {LightOnPath} / {LightOffPath}");
                return null;
            }

            GameObject root = new("Candle");

            // 로직은 루트, 그래픽은 Visual 자식. 아트가 또 바뀌면 이 스프라이트만 갈면 된다.
            GameObject visual = new("Visual");
            visual.transform.SetParent(root.transform, false);

            SpriteRenderer flame = visual.AddComponent<SpriteRenderer>();
            flame.sprite = litSprite;
            flame.color = Color.white;
            flame.sortingOrder = 4;

            // 스프라이트 원본 크기를 대략 타일 한 칸(1.5유닛) 높이로 맞춘다.
            float spriteHeight = Mathf.Max(0.01f, litSprite.bounds.size.y);
            float scale = 1.5f / spriteHeight;
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            Vector2 worldSize = litSprite.bounds.size * scale;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = worldSize;

            Candle candle = root.AddComponent<Candle>();
            SerializedObject so = new(candle);
            so.FindProperty("flame").objectReferenceValue = flame;
            so.FindProperty("litSprite").objectReferenceValue = litSprite;
            so.FindProperty("unlitSprite").objectReferenceValue = unlitSprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CandlePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
