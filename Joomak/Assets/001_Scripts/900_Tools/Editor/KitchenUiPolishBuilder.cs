using System.Collections.Generic;
using _001_Scripts._003_Object._000_Structure.Cooker;
using _001_Scripts._003_Object.Interface;
using _001_Scripts._004_UI.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._900_Tools.Editor
{
    public static class KitchenUiPolishBuilder
    {
        private const string BubblePrefabPath = "Assets/003_Prefabs/UI/conversation.prefab";
        private const string FontPath = "Assets/002_Resources/005_Fonts/CookieRun Bold.otf";

        [MenuItem("Joomak/Kitchen/Polish Kitchen UI")]
        public static void Build()
        {
            PolishBubblePrefab();
            WireInteractionBubbles();
            WireOpenSceneCookingStations();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KitchenUiPolishBuilder] 주방 말풍선 UI 정리 완료.");
        }

        private static void PolishBubblePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BubblePrefabPath);
            try
            {
                root.name = "RecipeBubble";

                SpriteRenderer background = root.GetComponent<SpriteRenderer>();
                if (background == null)
                {
                    throw new MissingComponentException("Recipe bubble prefab needs a SpriteRenderer.");
                }

                background.color = Hex("FFF6DF");

                SpriteRenderer shadow = GetOrCreateShadow(root.transform);
                shadow.sprite = background.sprite;
                shadow.sharedMaterial = background.sharedMaterial;
                shadow.color = new Color(0.18f, 0.09f, 0.04f, 0.32f);
                shadow.sortingLayerID = background.sortingLayerID;
                shadow.sortingOrder = background.sortingOrder - 1;

                Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                UnityEngine.UI.Text text = GetOrCreateText(root.transform, background);
                text.font = font;
                text.text = "<color=#9B5A2E><b>조리대</b></color>\nE로 레시피 선택";
                text.fontSize = 38;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 18;
                text.resizeTextMaxSize = 38;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Hex("3D271D");
                text.supportRichText = true;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;

                ScaleRevealAnimator animator = root.GetComponent<ScaleRevealAnimator>();
                if (animator != null)
                {
                    SerializedObject animationData = new(animator);
                    animationData.FindProperty("duration").floatValue = 0.2f;
                    animationData.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, BubblePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer GetOrCreateShadow(Transform root)
        {
            Transform existing = root.Find("BubbleShadow");
            GameObject shadowObject;
            if (existing == null)
            {
                shadowObject = new GameObject("BubbleShadow", typeof(SpriteRenderer));
                shadowObject.transform.SetParent(root, false);
            }
            else
            {
                shadowObject = existing.gameObject;
            }

            shadowObject.transform.localPosition = new Vector3(0.055f, -0.055f, 0.01f);
            shadowObject.transform.localRotation = Quaternion.identity;
            shadowObject.transform.localScale = Vector3.one;
            return shadowObject.GetComponent<SpriteRenderer>();
        }

        private static UnityEngine.UI.Text GetOrCreateText(Transform root, SpriteRenderer background)
        {
            Transform oldWorldText = root.Find("RecipeText");
            if (oldWorldText != null)
            {
                Object.DestroyImmediate(oldWorldText.gameObject);
            }

            Transform existingCanvas = root.Find("RecipeCanvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas.gameObject);
            }

            GameObject canvasObject = new("RecipeCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root, false);
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.localPosition = new Vector3(0.3f, 0.25f, -0.2f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.005f;
            canvasRect.sizeDelta = new Vector2(180f, 80f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = background.sortingLayerID;
            canvas.sortingOrder = 1001;

            GameObject textObject = new("RecipeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            UnityEngine.UI.Text text = textObject.GetComponent<UnityEngine.UI.Text>();
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 8f);
            rect.offsetMax = new Vector2(-10f, -8f);
            return text;
        }

        private static void WireInteractionBubbles()
        {
            GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubblePrefabPath);
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/003_Prefabs" });
            foreach (string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                if (path == BubblePrefabPath)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    HashSet<GameObject> processedObjects = new();
                    MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (behaviour is not IInteractable || !processedObjects.Add(behaviour.gameObject))
                        {
                            continue;
                        }

                        if (behaviour is CookingStation station)
                        {
                            WireCookingStation(station, bubblePrefab);
                        }
                        else
                        {
                            WireGenericPrompt(behaviour, bubblePrefab);
                        }

                        changed = true;
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void WireCookingStation(CookingStation station, GameObject bubblePrefab)
        {
            Transform existing = station.transform.Find("RecipeBubble");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, station.transform);
            bubble.name = "RecipeBubble";
            bubble.SetActive(false);

            SerializedObject stationData = new(station);
            SerializedProperty labelProperty = stationData.FindProperty("recipeLabel");
            if (labelProperty.objectReferenceValue is Text label)
            {
                label.gameObject.SetActive(false);
            }

            labelProperty.objectReferenceValue = null;
            stationData.FindProperty("recipeBubbleRoot").objectReferenceValue = bubble;
            stationData.FindProperty("recipeBubbleText").objectReferenceValue = null;
            stationData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGenericPrompt(MonoBehaviour interactable, GameObject bubblePrefab)
        {
            Transform existing = interactable.transform.Find("InteractionBubble");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(bubblePrefab, interactable.transform);
            bubble.name = "InteractionBubble";
            bubble.SetActive(false);

            InteractionPromptBubble prompt = interactable.GetComponent<InteractionPromptBubble>();
            if (prompt == null)
            {
                prompt = interactable.gameObject.AddComponent<InteractionPromptBubble>();
            }

            (string title, string hint) = GetPrompt(interactable.GetType().Name);
            SerializedObject promptData = new(prompt);
            promptData.FindProperty("bubbleRoot").objectReferenceValue = bubble;
            promptData.FindProperty("promptText").objectReferenceValue = bubble.GetComponentInChildren<UnityEngine.UI.Text>(true);
            promptData.FindProperty("title").stringValue = title;
            promptData.FindProperty("actionHint").stringValue = hint;
            promptData.FindProperty("worldScale").floatValue = 1.15f;
            promptData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (string title, string hint) GetPrompt(string typeName)
        {
            return typeName switch
            {
                "SupplyBox" => ("재료 상자", "E 재료 꺼내기"),
                "StorageStructure" => ("보관함", "E 보관 / 꺼내기"),
                "BundleUnpackingStation" => ("포장 해체대", "E 포장 풀기"),
                "ServingCounter" => ("서빙대", "E 전달 / 가져오기"),
                "DiningTable" => ("식탁", "E 음식 서빙"),
                "Candle" => ("촛불", "E 불 밝히기"),
                "Trash" => ("쓰레기", "E 치우기"),
                "Customer" => ("손님", "E 상호작용"),
                _ => ("아이템", "E 줍기")
            };
        }

        private static void WireOpenSceneCookingStations()
        {
            CookingStation[] stations = Object.FindObjectsByType<CookingStation>(FindObjectsInactive.Include);
            foreach (CookingStation station in stations)
            {
                Transform recipeBubble = station.transform.Find("RecipeBubble");
                if (recipeBubble == null)
                {
                    continue;
                }

                SerializedObject stationData = new(station);
                stationData.FindProperty("recipeBubbleRoot").objectReferenceValue = recipeBubble.gameObject;
                stationData.FindProperty("recipeBubbleText").objectReferenceValue = null;
                stationData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(station);

                for (int i = 0; i < station.transform.childCount; i++)
                {
                    Transform child = station.transform.GetChild(i);
                    if (child != recipeBubble && child.name.StartsWith("conversation"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.white;
        }
    }
}
