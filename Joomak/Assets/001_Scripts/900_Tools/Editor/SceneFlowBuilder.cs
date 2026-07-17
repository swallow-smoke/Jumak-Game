using System.Collections.Generic;
using System.Linq;
using _001_Scripts._000_Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._900_Tools.Editor
{
    public static class SceneFlowBuilder
    {
        private const string TitleScenePath = "Assets/000_Scenes/Title.unity";
        private const string LoadingScenePath = "Assets/000_Scenes/Loading.unity";
        private const string GameScenePath = "Assets/000_Scenes/InGame.unity";
        private const string UpgradeScenePath = "Assets/000_Scenes/Upgrade.unity";
        private const string RegularFontPath = "Assets/002_Resources/005_Fonts/CookieRun Regular.otf";
        private const string BoldFontPath = "Assets/002_Resources/005_Fonts/CookieRun Bold.otf";

        private static readonly Color Ink = Hex("3D271D");
        private static readonly Color Cream = Hex("FFF6DF");
        private static readonly Color Brown = Hex("6C3522");
        private static readonly Color Gold = Hex("D79A3B");
        private static readonly Color DarkBrown = Hex("271811");

        [MenuItem("Joomak/Scene Flow/Build Title And Loading")]
        public static void Build()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            string returnScenePath = currentScene.path;
            if (currentScene.isDirty)
            {
                Debug.LogError("[SceneFlowBuilder] 현재 씬에 저장하지 않은 변경이 있습니다. 먼저 저장한 뒤 다시 실행하세요.");
                return;
            }

            Font regular = AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
            Font bold = AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath);
            if (regular == null || bold == null)
            {
                Debug.LogError("[SceneFlowBuilder] CookieRun 폰트를 찾지 못했습니다.");
                return;
            }

            BuildTitleScene(regular, bold);
            BuildLoadingScene(regular, bold);
            RegisterBuildScenes();

            if (!string.IsNullOrWhiteSpace(returnScenePath))
            {
                EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneFlowBuilder] Title -> Loading -> InGame 씬 흐름 구성을 완료했습니다.");
        }

        private static void BuildTitleScene(Font regular, Font bold)
        {
            Scene scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            RemoveExistingFlowObjects(scene);

            SceneLoader loader = new GameObject("SceneLoader").AddComponent<SceneLoader>();
            Canvas canvas = CreateCanvas("TitleCanvas", Hex("E8C98E"));

            Image panel = CreateImage(canvas.transform, "TitlePanel", Cream);
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                Vector2.zero, new Vector2(660f, 570f));
            panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.02f, 0.45f);

            Text title = CreateText(panel.transform, "GameTitle", "주  막", 88, Brown, bold, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -95f), new Vector2(560f, 120f));

            Text subtitle = CreateText(panel.transform, "Subtitle", "오늘도 따뜻한 한 상을 준비합니다", 27, Ink, regular);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -188f), new Vector2(560f, 50f));

            Button startButton = CreateButton(panel.transform, "StartButton", "영업 시작", bold, Gold, DarkBrown);
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(0f, -55f), new Vector2(390f, 82f));
            UnityEventTools.AddPersistentListener(startButton.onClick, loader.LoadGame);

            Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료", regular, Brown, Cream);
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(0f, -160f), new Vector2(390f, 68f));
            UnityEventTools.AddPersistentListener(quitButton.onClick, loader.QuitGame);

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildLoadingScene(Font regular, Font bold)
        {
            Scene scene = EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
            RemoveExistingFlowObjects(scene);

            SceneLoader loader = new GameObject("SceneLoader").AddComponent<SceneLoader>();
            Canvas canvas = CreateCanvas("LoadingCanvas", DarkBrown);

            Text title = CreateText(canvas.transform, "LoadingTitle", "주막을 준비하는 중...", 48, Cream, bold, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(0f, 105f), new Vector2(760f, 80f));

            Slider slider = CreateProgressBar(canvas.transform);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                Vector2.zero, new Vector2(760f, 42f));

            Text percent = CreateText(canvas.transform, "ProgressText", "0%", 30, Cream, regular, FontStyle.Bold);
            SetRect(percent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(0f, -70f), new Vector2(300f, 55f));

            Text hint = CreateText(canvas.transform, "LoadingHint", "맛있는 음식과 따뜻한 자리를 마련하고 있습니다", 23,
                new Color(1f, 0.84f, 0.55f, 1f), regular);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(0f, -145f), new Vector2(850f, 50f));

            SerializedObject loaderData = new(loader);
            loaderData.FindProperty("progressBar").objectReferenceValue = slider;
            loaderData.FindProperty("progressText").objectReferenceValue = percent;
            loaderData.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Canvas CreateCanvas(string name, Color backgroundColor)
        {
            GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(canvas.transform, "Background", backgroundColor);
            Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();
            return canvas;
        }

        private static Slider CreateProgressBar(Transform parent)
        {
            GameObject root = new("ProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(parent, false);
            Image background = root.GetComponent<Image>();
            background.color = new Color(1f, 0.94f, 0.78f, 0.2f);

            Image fill = CreateImage(root.transform, "Fill", Gold);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(6f, 6f);
            fillRect.offsetMax = new Vector2(-6f, -6f);

            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fillRect;
            slider.targetGraphic = background;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Color background, Color textColor)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = background;
            root.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.02f, 0.35f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.7f, 1f);
            colors.pressedColor = new Color(0.82f, 0.68f, 0.48f, 1f);
            button.colors = colors;

            Text text = CreateText(root.transform, "Label", label, 31, textColor, font, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Color color,
            Font font,
            FontStyle style = FontStyle.Normal)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void RemoveExistingFlowObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name is "SceneLoader" or "TitleCanvas" or "LoadingCanvas" or "EventSystem")
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void RegisterBuildScenes()
        {
            string[] required = { TitleScenePath, LoadingScenePath, GameScenePath, UpgradeScenePath };
            List<EditorBuildSettingsScene> scenes = required
                .Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToList();

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (scenes.All(scene => scene.path != existing.path))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString($"#{value}", out Color color);
            return color;
        }
    }
}
