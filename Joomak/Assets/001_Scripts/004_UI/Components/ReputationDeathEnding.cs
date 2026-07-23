using System.Collections;
using System.Linq;
using _001_Scripts._000_Core;
using _001_Scripts._001_Manager;
using _001_Scripts._005_Data.Config;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public sealed class ReputationDeathEnding : MonoBehaviour
    {
        private static ReputationDeathEnding instance;
        private float previousTimeScale = 1f;
        private CanvasGroup canvasGroup;

        public static bool IsActive => instance != null;

        public static void Show()
        {
            if (instance != null)
            {
                return;
            }

            instance = new GameObject("Reputation Death Ending").AddComponent<ReputationDeathEnding>();
            instance.Build();
        }

        private void Build()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (GameBalance.Current.reputation.deleteSaveOnDeath)
            {
                SaveGameManager.DeleteSave();
            }

            Font font = CreateKoreanFont();
            GameObject canvasObject = new("DeathEndingCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            Image background = CreateImage(canvasObject.transform, "Blackout", new Color32(15, 7, 7, 252));
            Stretch(background.rectTransform);

            Image redGlow = CreateImage(canvasObject.transform, "RedGlow", new Color(0.35f, 0.025f, 0.02f, 0.32f));
            SetRect(redGlow.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(1450f, 720f));

            Text title = CreateText(canvasObject.transform, "EndingTitle", "주막의 마지막 밤", 72,
                new Color32(210, 74, 58, 255), font, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -180f), new Vector2(1250f, 110f));

            Text ending = CreateText(canvasObject.transform, "EndingText",
                "명성을 모두 잃은 주막에는 더 이상 손님이 찾아오지 않았다.\n" +
                "빚과 과로에 지친 주인은 끝내 쓰러졌고,\n" +
                "주막의 불은 다시 켜지지 않았다.", 36,
                new Color32(232, 214, 190, 255), font);
            ending.lineSpacing = 1.35f;
            SetRect(ending.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, 40f), new Vector2(1300f, 260f));

            Text cause = CreateText(canvasObject.transform, "Cause", "DEATH ENDING · 명성 0", 24,
                new Color32(160, 112, 92, 255), font, FontStyle.Bold);
            SetRect(cause.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -135f), new Vector2(700f, 48f));

            Button restart = CreateButton(canvasObject.transform, "Restart", "처음부터", font,
                new Color32(159, 53, 42, 255));
            SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-330f, 120f), new Vector2(270f, 76f));
            restart.onClick.AddListener(RestartRun);

            Button titleButton = CreateButton(canvasObject.transform, "Title", "타이틀로", font,
                new Color32(105, 70, 55, 255));
            SetRect(titleButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(270f, 76f));
            titleButton.onClick.AddListener(ReturnToTitle);

            Button quit = CreateButton(canvasObject.transform, "Quit", "게임 종료", font,
                new Color32(63, 45, 41, 255));
            SetRect(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(330f, 120f), new Vector2(270f, 76f));
            quit.onClick.AddListener(QuitGame);

            EnsureEventSystem();
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float duration = Mathf.Max(0.01f, GameBalance.Current.reputation.endingFadeSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private void RestartRun()
        {
            SaveGameManager.DeleteSave();
            DayCycleManager.ResetRun();
            UpgradeApi.ResetRun();
            RunState.Instance?.ResetRun();
            TutorialOverlay.RequestForNewGame();
            LeaveEnding();
            SceneLoader.LoadThroughLoading("InGame");
        }

        private void ReturnToTitle()
        {
            SaveGameManager.DeleteSave();
            LeaveEnding();
            SceneLoader.LoadThroughLoading("Title");
        }

        private void QuitGame()
        {
            LeaveEnding();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LeaveEnding()
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            instance = null;
            Destroy(gameObject);
        }

        private static Font CreateKoreanFont()
        {
            Font cookie = Resources.FindObjectsOfTypeAll<Font>()
                .FirstOrDefault(candidate => candidate != null && candidate.name.Contains("CookieRun"));
            return cookie != null
                ? cookie
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 32);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Color color,
            Font font, FontStyle style = FontStyle.Normal)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Shadow));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Shadow shadow = root.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(5f, -5f);

            Text text = CreateText(root.transform, "Label", label, 28, new Color32(255, 238, 211, 255), font,
                FontStyle.Bold);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding;
            rect.offsetMax = -Vector2.one * padding;
        }
    }
}
