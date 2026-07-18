using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    // 새 게임으로 InGame에 처음 들어올 때 조작과 핵심 루프를 안내한다.
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private const string GameSceneName = "InGame";

        private static readonly TutorialPage[] Pages =
        {
            new("주막 운영 시작",
                "두 사람이 역할을 나눠 주막을 운영합니다.\n\n" +
                "홀 담당  ·  WASD 이동 / E 상호작용\n" +
                "주방 담당  ·  방향키 이동 / SPACE 상호작용"),
            new("손님 맞이와 주문",
                "홀 담당이 손님을 바라보고 E를 누르면 따라옵니다.\n" +
                "빈 식탁으로 안내한 뒤 식탁에 E를 눌러 앉히세요.\n\n" +
                "손님 위에 ! 말풍선이 뜨면 다시 E를 눌러 주문을 받습니다."),
            new("조리와 서빙",
                "주방 주문판에서 들어온 메뉴를 확인하세요.\n" +
                "조리대에 필요한 재료를 넣고 레시피를 선택해 조리합니다.\n\n" +
                "완성된 요리는 서빙대에 놓고, 홀 담당이 받아 해당 손님에게 전달합니다."),
            new("재료 보급",
                "재료 배달 알림이 오면 홀 입구의 상자를 서빙대로 옮기세요.\n" +
                "주방 담당이 상자를 받아 해체대에서 포장을 풀면 보급함 재고가 채워집니다.\n\n" +
                "보급함 위 숫자로 현재 재고를 확인할 수 있습니다."),
            new("하루 운영 팁",
                "쓰레기·먹튀·손놈은 빗자루로 해결하고, 꺼진 촛불은 상호작용으로 켭니다.\n" +
                "손님이 식사를 마치면 접시를 치워 주방으로 반납하세요.\n\n" +
                "하루가 끝나면 정산 후 업그레이드를 구매할 수 있습니다.\n" +
                "ESC  ·  설정 / 일시정지")
        };

        private static bool requestedForNewGame;
        private static int inputConsumedFrame = -1;

        private Text stepText;
        private Text titleText;
        private Text bodyText;
        private Text nextButtonText;
        private Button previousButton;
        private Button nextButton;
        private int pageIndex;
        private float previousTimeScale = 1f;
        private bool timeScaleCaptured;

        public static bool IsOpen { get; private set; }
        public static bool ShouldBlockGameplayInput => IsOpen || inputConsumedFrame == Time.frameCount;

        public static void RequestForNewGame()
        {
            requestedForNewGame = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            requestedForNewGame = false;
            inputConsumedFrame = -1;
            IsOpen = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name != GameSceneName || !requestedForNewGame || FindAnyObjectByType<TutorialOverlay>() != null)
            {
                return;
            }

            new GameObject("TutorialOverlay").AddComponent<TutorialOverlay>();
        }

        private void Awake()
        {
            IsOpen = true;
            BuildUi();
            ShowPage(0);
        }

        private IEnumerator Start()
        {
            // DayCycleManager.Start가 Time.timeScale을 복구한 뒤 확실히 일시정지한다.
            yield return null;
            previousTimeScale = Time.timeScale;
            timeScaleCaptured = true;
            Time.timeScale = 0f;
            nextButton?.Select();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame && pageIndex > 0)
            {
                ShowPage(pageIndex - 1);
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                Next();
            }
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                RestoreTimeScale();
            }
        }

        private void Next()
        {
            if (pageIndex >= Pages.Length - 1)
            {
                Close();
                return;
            }

            ShowPage(pageIndex + 1);
        }

        private void Close()
        {
            requestedForNewGame = false;
            inputConsumedFrame = Time.frameCount;
            IsOpen = false;
            RestoreTimeScale();
            Destroy(gameObject);
        }

        private void RestoreTimeScale()
        {
            if (timeScaleCaptured)
            {
                Time.timeScale = previousTimeScale;
                timeScaleCaptured = false;
            }
        }

        private void ShowPage(int index)
        {
            pageIndex = Mathf.Clamp(index, 0, Pages.Length - 1);
            TutorialPage page = Pages[pageIndex];
            stepText.text = $"초보 주모 안내  {pageIndex + 1}/{Pages.Length}";
            titleText.text = page.Title;
            bodyText.text = page.Body;
            previousButton.interactable = pageIndex > 0;
            nextButtonText.text = pageIndex == Pages.Length - 1 ? "영업 시작" : "다음";
        }

        private void BuildUi()
        {
            Font font = FindFont();
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9500;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            Image dim = CreateImage(transform, "Dim", new Color(0.025f, 0.018f, 0.012f, 0.78f));
            Stretch(dim.rectTransform);

            Image panel = CreateImage(dim.transform, "TutorialPanel", new Color32(250, 235, 200, 255));
            SetRect(panel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(940f, 600f));
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(10f, -10f);

            Image header = CreateImage(panel.transform, "Header", new Color32(92, 48, 24, 255));
            SetRect(header.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 92f));

            stepText = CreateText(header.transform, "Step", string.Empty, 22, new Color32(240, 190, 92, 255), font);
            SetRect(stepText.rectTransform, new Vector2(0f, 0f), new Vector2(0.35f, 1f), new Vector2(0f, 0.5f),
                new Vector2(32f, 0f), Vector2.zero);
            stepText.alignment = TextAnchor.MiddleLeft;

            Text skipText = CreateText(header.transform, "SkipHint", "ESC 건너뛰기", 19,
                new Color32(242, 222, 187, 255), font);
            SetRect(skipText.rectTransform, new Vector2(0.7f, 0f), Vector2.one, new Vector2(1f, 0.5f),
                new Vector2(-30f, 0f), Vector2.zero);
            skipText.alignment = TextAnchor.MiddleRight;

            titleText = CreateText(panel.transform, "Title", string.Empty, 42, new Color32(74, 36, 20, 255), font,
                FontStyle.Bold);
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -135f), new Vector2(-90f, 70f));

            bodyText = CreateText(panel.transform, "Body", string.Empty, 28, new Color32(72, 59, 45, 255), font);
            SetRect(bodyText.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -35f), new Vector2(760f, 250f));
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.lineSpacing = 1.2f;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

            previousButton = CreateButton(panel.transform, "Previous", "이전", font, new Color32(117, 86, 57, 255));
            SetRect(previousButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(42f, 34f), new Vector2(190f, 66f));
            previousButton.onClick.AddListener(() => ShowPage(pageIndex - 1));

            nextButton = CreateButton(panel.transform, "Next", "다음", font, new Color32(218, 157, 55, 255));
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), Vector2.one * new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-42f, 34f), new Vector2(230f, 66f));
            nextButton.onClick.AddListener(Next);
            nextButtonText = nextButton.GetComponentInChildren<Text>();

            Text keyHint = CreateText(panel.transform, "KeyHint", "E / SPACE / →  다음", 20,
                new Color32(126, 92, 58, 255), font);
            SetRect(keyHint.rectTransform, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 52f), new Vector2(0f, 42f));
        }

        private static Font FindFont()
        {
            foreach (Font loaded in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (loaded != null && loaded.name.Contains("CookieRun"))
                {
                    return loaded;
                }
            }

            return Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 28)
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string objectName, string value, int size, Color color,
            Font font, FontStyle style = FontStyle.Normal)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Text));
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

        private static Button CreateButton(Transform parent, string objectName, string label, Font font, Color color)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(root.transform, "Label", label, 25, new Color32(44, 28, 18, 255), font,
                FontStyle.Bold);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
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

        private readonly struct TutorialPage
        {
            public readonly string Title;
            public readonly string Body;

            public TutorialPage(string title, string body)
            {
                Title = title;
                Body = body;
            }
        }
    }
}
