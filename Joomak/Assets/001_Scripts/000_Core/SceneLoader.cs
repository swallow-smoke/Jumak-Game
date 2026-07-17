using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._000_Core
{
    // Title -> Loading -> 목표 씬의 흐름을 담당한다.
    // Title 버튼에서는 LoadGame/LoadScene을 호출하고 Loading 씬에서는 자동으로 비동기 로드를 시작한다.
    public sealed class SceneLoader : MonoBehaviour
    {
        private const string LoadingSceneName = "Loading";
        private const string DefaultGameSceneName = "InGame";

        private static string pendingSceneName;

        [Header("Loading UI")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private Text progressText;
        [SerializeField, Min(0f)] private float minimumDisplaySeconds = 0.6f;

        private bool isLoading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapSceneFlow()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName is not ("Title" or LoadingSceneName) || FindAnyObjectByType<SceneLoader>() != null)
            {
                return;
            }

            SceneLoader loader = new GameObject("SceneLoader").AddComponent<SceneLoader>();
            if (sceneName == "Title")
            {
                loader.BuildRuntimeTitleUi();
            }
            else
            {
                loader.BuildRuntimeLoadingUi();
            }
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == LoadingSceneName)
            {
                StartCoroutine(LoadPendingSceneAsync());
            }
        }

        public void LoadGame()
        {
            LoadScene(DefaultGameSceneName);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Build Settings에서 '{sceneName}' 씬을 찾을 수 없습니다.", this);
                return;
            }

            isLoading = true;
            pendingSceneName = sceneName;
            SceneManager.LoadScene(LoadingSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadPendingSceneAsync()
        {
            if (isLoading)
            {
                yield break;
            }

            isLoading = true;
            string targetScene = string.IsNullOrWhiteSpace(pendingSceneName)
                ? DefaultGameSceneName
                : pendingSceneName;

            pendingSceneName = null;
            SetProgress(0f);

            if (!Application.CanStreamedLevelBeLoaded(targetScene))
            {
                Debug.LogError($"[SceneLoader] Build Settings에서 '{targetScene}' 씬을 찾을 수 없습니다.", this);
                SetProgress(0f, "씬을 불러올 수 없습니다");
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
            if (operation == null)
            {
                SetProgress(0f, "로딩을 시작할 수 없습니다");
                yield break;
            }

            operation.allowSceneActivation = false;
            float startedAt = Time.realtimeSinceStartup;

            while (!operation.isDone)
            {
                // Unity 비동기 로드는 활성화 직전 상태를 0.9로 보고하므로 UI에서는 0~100%로 환산한다.
                float normalized = Mathf.Clamp01(operation.progress / 0.9f);
                SetProgress(normalized);

                bool loaded = operation.progress >= 0.9f;
                bool shownLongEnough = Time.realtimeSinceStartup - startedAt >= minimumDisplaySeconds;
                if (loaded && shownLongEnough)
                {
                    SetProgress(1f);
                    yield return null;
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void SetProgress(float value, string overrideText = null)
        {
            value = Mathf.Clamp01(value);
            if (progressBar != null)
            {
                progressBar.SetValueWithoutNotify(value);
            }

            if (progressText != null)
            {
                progressText.text = overrideText ?? $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void BuildRuntimeTitleUi()
        {
            Font font = CreateKoreanFont();
            Canvas canvas = CreateCanvas("TitleCanvas", new Color32(232, 201, 142, 255));
            Image panel = CreateImage(canvas.transform, "TitlePanel", new Color32(255, 246, 223, 255));
            SetRect(panel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(660f, 570f));
            panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.02f, 0.45f);

            Text title = CreateText(panel.transform, "GameTitle", "주  막", 88, new Color32(108, 53, 34, 255), font, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -95f), new Vector2(560f, 120f));

            Text subtitle = CreateText(panel.transform, "Subtitle", "오늘도 따뜻한 한 상을 준비합니다", 27,
                new Color32(61, 39, 29, 255), font);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -188f), new Vector2(560f, 50f));

            Button startButton = CreateButton(panel.transform, "StartButton", "영업 시작", font,
                new Color32(215, 154, 59, 255), new Color32(39, 24, 17, 255));
            SetRect(startButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -55f), new Vector2(390f, 82f));
            startButton.onClick.AddListener(LoadGame);

            Button quitButton = CreateButton(panel.transform, "QuitButton", "게임 종료", font,
                new Color32(108, 53, 34, 255), new Color32(255, 246, 223, 255));
            SetRect(quitButton.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -160f), new Vector2(390f, 68f));
            quitButton.onClick.AddListener(QuitGame);

            EnsureEventSystem();
        }

        private void BuildRuntimeLoadingUi()
        {
            Font font = CreateKoreanFont();
            Canvas canvas = CreateCanvas("LoadingCanvas", new Color32(39, 24, 17, 255));

            Text title = CreateText(canvas.transform, "LoadingTitle", "주막을 준비하는 중...", 48,
                new Color32(255, 246, 223, 255), font, FontStyle.Bold);
            SetRect(title.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, 105f), new Vector2(760f, 80f));

            progressBar = CreateProgressBar(canvas.transform);
            SetRect(progressBar.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(760f, 42f));

            progressText = CreateText(canvas.transform, "ProgressText", "0%", 30,
                new Color32(255, 246, 223, 255), font, FontStyle.Bold);
            SetRect(progressText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -70f), new Vector2(300f, 55f));

            Text hint = CreateText(canvas.transform, "LoadingHint", "맛있는 음식과 따뜻한 자리를 마련하고 있습니다", 23,
                new Color(1f, 0.84f, 0.55f, 1f), font);
            SetRect(hint.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -145f), new Vector2(850f, 50f));
        }

        private static Font CreateKoreanFont()
        {
            foreach (Font loadedFont in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (loadedFont != null && loadedFont.name.Contains("CookieRun"))
                {
                    return loadedFont;
                }
            }

            return Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" },
                32);
        }

        private static Canvas CreateCanvas(string objectName, Color backgroundColor)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(root.transform, "Background", backgroundColor);
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

            Image fill = CreateImage(root.transform, "Fill", new Color32(215, 154, 59, 255));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(6f, 6f);
            fill.rectTransform.offsetMax = new Vector2(-6f, -6f);

            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(0f);
            slider.transition = Selectable.Transition.None;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;
            return slider;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Font font, Color background, Color textColor)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = background;
            root.AddComponent<Shadow>().effectColor = new Color(0.1f, 0.05f, 0.02f, 0.35f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(root.transform, "Label", label, 31, textColor, font, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string value,
            int size,
            Color color,
            Font font,
            FontStyle style = FontStyle.Normal)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
    }
}
