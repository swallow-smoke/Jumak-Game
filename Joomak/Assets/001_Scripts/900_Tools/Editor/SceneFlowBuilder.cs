using System.Collections.Generic;
using System.Linq;
using _001_Scripts._000_Core;
using _001_Scripts._001_Manager;
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
        private const string TitleBackgroundPath = "Assets/002_Resources/000_Images/TiTle.png";
        private const string LoadingBackgroundPath = "Assets/002_Resources/000_Images/loading.png";

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
            Canvas canvas = CreateCanvas("TitleCanvas", DarkBrown);
            Image background = canvas.transform.Find("Background").GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAllAssetsAtPath(TitleBackgroundPath).OfType<Sprite>().FirstOrDefault();
            background.color = Color.white;
            background.type = Image.Type.Simple;

            Image shade = CreateImage(canvas.transform, "BottomShade", new Color(0.08f, 0.035f, 0.015f, 0.14f));
            Stretch(shade.rectTransform);

            Image menuBar = CreateImage(canvas.transform, "MenuBar", new Color(0.12f, 0.055f, 0.025f, 0.9f));
            SetRect(menuBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 32f), new Vector2(1120f, 126f));
            menuBar.gameObject.AddComponent<Outline>().effectColor = new Color32(215, 154, 59, 210);
            menuBar.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.65f);

            Button startButton = CreateButton(menuBar.transform, "NewGameButton", "새 게임", bold, Hex("E7BE70"), DarkBrown);
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(-350f, 0f), new Vector2(292f, 76f));
            UnityEventTools.AddPersistentListener(startButton.onClick, loader.LoadGame);

            Button continueButton = CreateButton(menuBar.transform, "ContinueButton", "이어하기", bold, Hex("B26F37"), Cream);
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                Vector2.zero, new Vector2(292f, 76f));
            UnityEventTools.AddPersistentListener(continueButton.onClick, loader.ContinueGame);
            continueButton.interactable = SaveGameManager.HasSave;

            Button quitButton = CreateButton(menuBar.transform, "QuitButton", "게임 종료", regular, Hex("5B2D1E"), Cream);
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * 0.5f,
                new Vector2(350f, 0f), new Vector2(292f, 76f));
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
            Image background = canvas.transform.Find("Background").GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAllAssetsAtPath(LoadingBackgroundPath).OfType<Sprite>().FirstOrDefault();
            background.color = Color.white;
            background.type = Image.Type.Simple;

            Image shade = CreateImage(canvas.transform, "LoadingShade", new Color(0.04f, 0.015f, 0.005f, 0.12f));
            Stretch(shade.rectTransform);

            Image panel = CreateImage(canvas.transform, "LoadingPanel", new Color(0.1f, 0.04f, 0.015f, 0.9f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 34f), new Vector2(1040f, 210f));
            panel.gameObject.AddComponent<Outline>().effectColor = new Color32(215, 154, 59, 210);
            panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.65f);

            Text title = CreateText(panel.transform, "LoadingTitle", "주막을 준비하는 중...", 36,
                new Color32(255, 236, 190, 255), bold, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -45f), new Vector2(760f, 52f));

            Slider slider = CreateProgressBar(panel.transform);
            SetRect(slider.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(-42f, -4f), new Vector2(820f, 34f));

            Text percent = CreateText(panel.transform, "ProgressText", "0%", 25, Cream, regular, FontStyle.Bold);
            SetRect(percent.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(425f, -4f), new Vector2(100f, 44f));

            Text hint = CreateText(panel.transform, "LoadingHint", "맛있는 음식과 따뜻한 자리를 마련하고 있습니다", 20,
                new Color32(230, 190, 120, 255), regular);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(850f, 38f));

            SerializedObject loaderData = new(loader);
            loaderData.FindProperty("progressBar").objectReferenceValue = slider;
            loaderData.FindProperty("progressText").objectReferenceValue = percent;
            loaderData.FindProperty("minimumDisplaySeconds").floatValue = 1.5f;
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
