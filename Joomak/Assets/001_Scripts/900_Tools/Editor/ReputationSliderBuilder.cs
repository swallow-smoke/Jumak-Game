using _001_Scripts._004_UI.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._900_Tools.Editor
{
    public static class ReputationSliderBuilder
    {
        private const string FontPath = "Assets/002_Resources/005_Fonts/CookieRun Bold.otf";

        [MenuItem("Joomak/UI/Add Reputation Slider")]
        public static void Build()
        {
            Canvas canvas = FindRootCanvas();
            if (canvas == null)
            {
                Debug.LogError("[ReputationSliderBuilder] 활성 씬에서 Canvas를 찾지 못했습니다.");
                return;
            }

            Transform existing = canvas.transform.Find("ReputationSlider");
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[ReputationSliderBuilder] 명성 슬라이더가 이미 존재합니다.");
                return;
            }

            GameObject root = new("ReputationSlider", typeof(RectTransform), typeof(Image), typeof(Slider), typeof(Shadow), typeof(ReputationSliderView));
            Undo.RegisterCreatedObjectUndo(root, "Add Reputation Slider");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -22f);
            rootRect.sizeDelta = new Vector2(360f, 34f);

            Image background = root.GetComponent<Image>();
            background.color = new Color32(58, 35, 25, 225);

            Shadow shadow = root.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
            shadow.effectDistance = new Vector2(3f, -3f);

            RectTransform fillArea = CreateRect(root.transform, "Fill Area");
            Stretch(fillArea, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            GameObject fillObject = new("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillArea, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color32(242, 171, 46, 255);

            GameObject labelObject = new("ValueLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, Vector2.zero, Vector2.zero);

            Text label = labelObject.GetComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            label.text = "명성  20 / 100";
            label.fontSize = 21;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color32(255, 248, 225, 255);
            label.raycastTarget = false;

            Shadow labelShadow = labelObject.GetComponent<Shadow>();
            labelShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            labelShadow.effectDistance = new Vector2(1f, -1f);

            Slider slider = root.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 20f;

            SerializedObject viewData = new(root.GetComponent<ReputationSliderView>());
            viewData.FindProperty("slider").objectReferenceValue = slider;
            viewData.FindProperty("valueText").objectReferenceValue = label;
            viewData.FindProperty("fillImage").objectReferenceValue = fill;
            viewData.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            Selection.activeGameObject = root;
            Debug.Log("[ReputationSliderBuilder] 명성 슬라이더를 Canvas 상단에 추가했습니다.");
        }

        private static Canvas FindRootCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isRootCanvas && canvas.gameObject.scene.IsValid())
                {
                    return canvas;
                }
            }

            return null;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minOffset;
            rect.offsetMax = maxOffset;
        }
    }
}
