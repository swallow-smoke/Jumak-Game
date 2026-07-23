using System;
using System.Collections;
using System.Collections.Generic;
using _001_Scripts._002_Controller;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public enum TutorialAction
    {
        HallMoved,
        KitchenMoved,
        CustomerEscorted,
        CustomerSeated,
        OrderTaken,
        BundlePickedUp,
        BundleUnpacked,
        RecipeSelected,
        CookingStarted,
        DishCompleted,
        DishServed
    }

    public static class TutorialProgress
    {
        private static readonly HashSet<TutorialAction> Completed = new();
        public static event Action<TutorialAction> Progressed;

        public static void Reset()
        {
            Completed.Clear();
        }

        public static void Report(TutorialAction action)
        {
            if (Completed.Add(action))
            {
                Progressed?.Invoke(action);
            }
        }

        public static bool HasCompleted(TutorialAction action) => Completed.Contains(action);
    }

    // 새 게임에서는 짧은 안내 뒤 실제 행동을 확인하는 목표 추적기로 전환된다.
    // F1을 누르면 언제든 처음부터 다시 볼 수 있다.
    public sealed class TutorialOverlay : MonoBehaviour
    {
        private const string GameSceneName = "InGame";

        private static readonly TutorialStep[] Steps =
        {
            new(TutorialAction.HallMoved, "홀 담당 이동",
                "홀 담당 캐릭터를 움직여 보세요.", PlayerControlProfile.Hall, PlayerControlAction.MoveUp),
            new(TutorialAction.KitchenMoved, "주방 담당 이동",
                "주방 담당 캐릭터도 움직여 보세요.", PlayerControlProfile.Kitchen, PlayerControlAction.MoveUp),
            new(TutorialAction.CustomerEscorted, "손님 맞이",
                "입구의 손님을 바라보고 상호작용해서 따라오게 하세요.", PlayerControlProfile.Hall, PlayerControlAction.Interact),
            new(TutorialAction.CustomerSeated, "식탁 안내",
                "손님이 따라오는 상태에서 빈 식탁을 바라보고 상호작용하세요.", PlayerControlProfile.Hall, PlayerControlAction.Interact),
            new(TutorialAction.OrderTaken, "주문 받기",
                "손님 위에 느낌표가 뜨면 손님에게 상호작용해서 주문을 받으세요.", PlayerControlProfile.Hall, PlayerControlAction.Interact),
            new(TutorialAction.BundlePickedUp, "재료 상자 운반",
                "홀 입구에 배달된 재료 상자 하나를 집으세요. 서빙대를 통해 주방으로 넘길 수 있습니다.",
                PlayerControlProfile.Hall, PlayerControlAction.Interact),
            new(TutorialAction.BundleUnpacked, "재료 해체",
                "주방 담당이 재료 상자를 해체대에 가져가 포장을 푸세요.", PlayerControlProfile.Kitchen, PlayerControlAction.Interact),
            new(TutorialAction.RecipeSelected, "레시피 선택",
                "주방 조리대에서 상호작용한 뒤 선택 키로 레시피를 고르고 확정하세요.",
                PlayerControlProfile.Kitchen, PlayerControlAction.Interact),
            new(TutorialAction.CookingStarted, "재료 투입과 조리",
                "보급함에서 필요한 재료를 하나씩 꺼내 선택한 조리대에 넣으세요.",
                PlayerControlProfile.Kitchen, PlayerControlAction.Interact),
            new(TutorialAction.DishCompleted, "요리 완성",
                "조리가 끝날 때까지 기다리거나 조리대에 상호작용해서 시간을 단축하세요.",
                PlayerControlProfile.Kitchen, PlayerControlAction.Interact),
            new(TutorialAction.DishServed, "손님에게 서빙",
                "완성 요리를 홀로 넘긴 뒤 주문한 손님에게 전달하세요.",
                PlayerControlProfile.Hall, PlayerControlAction.Interact)
        };

        private static bool requestedForNewGame;
        private static int inputConsumedFrame = -1;

        private GameObject introRoot;
        private GameObject trackerRoot;
        private Text trackerStepText;
        private Text trackerTitleText;
        private Text trackerBodyText;
        private Text trackerKeyText;
        private Slider progressBar;
        private int stepIndex;
        private float previousTimeScale = 1f;
        private bool timeScaleCaptured;
        private bool completing;

        public static bool IsOpen { get; private set; }
        public static bool IsRunning { get; private set; }
        public static bool ShouldBlockGameplayInput => IsOpen || inputConsumedFrame == Time.frameCount;

        public static void RequestForNewGame()
        {
            requestedForNewGame = true;
            TutorialProgress.Reset();
        }

        public static void OpenManual()
        {
            if (FindAnyObjectByType<TutorialOverlay>() != null)
            {
                return;
            }

            TutorialProgress.Reset();
            new GameObject("TutorialOverlay").AddComponent<TutorialOverlay>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            requestedForNewGame = false;
            inputConsumedFrame = -1;
            IsOpen = false;
            IsRunning = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name != GameSceneName)
            {
                return;
            }

            if (FindAnyObjectByType<TutorialLauncher>() == null)
            {
                new GameObject("TutorialLauncher").AddComponent<TutorialLauncher>();
            }

            if (requestedForNewGame && FindAnyObjectByType<TutorialOverlay>() == null)
            {
                new GameObject("TutorialOverlay").AddComponent<TutorialOverlay>();
            }
        }

        private void Awake()
        {
            IsOpen = true;
            IsRunning = true;
            TutorialProgress.Progressed += OnProgressed;
            BuildUi();
            RefreshTracker();
        }

        private IEnumerator Start()
        {
            yield return null;
            previousTimeScale = Time.timeScale;
            timeScaleCaptured = true;
            Time.timeScale = 0f;
            if (!IsOpen)
            {
                RestoreTimeScale();
            }
        }

        private void Update()
        {
            if (!IsOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SkipTutorial();
            }
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                BeginPractice();
            }
        }

        private void OnDestroy()
        {
            TutorialProgress.Progressed -= OnProgressed;
            RestoreTimeScale();
            IsOpen = false;
            IsRunning = false;
        }

        private void BeginPractice()
        {
            IsOpen = false;
            inputConsumedFrame = Time.frameCount;
            RestoreTimeScale();
            introRoot.SetActive(false);
            trackerRoot.SetActive(true);
            AdvancePastAlreadyCompletedSteps();
            RefreshTracker();
        }

        private void SkipTutorial()
        {
            requestedForNewGame = false;
            inputConsumedFrame = Time.frameCount;
            IsOpen = false;
            RestoreTimeScale();
            Destroy(gameObject);
        }

        private void OnProgressed(TutorialAction action)
        {
            if (completing || stepIndex >= Steps.Length || Steps[stepIndex].Action != action)
            {
                return;
            }

            NotificationModal.Show($"튜토리얼 완료\n{Steps[stepIndex].Title}", NotificationKind.Success, 2f);
            stepIndex++;
            AdvancePastAlreadyCompletedSteps();
            RefreshTracker();
        }

        private void AdvancePastAlreadyCompletedSteps()
        {
            while (stepIndex < Steps.Length && TutorialProgress.HasCompleted(Steps[stepIndex].Action))
            {
                stepIndex++;
            }
        }

        private void RefreshTracker()
        {
            if (stepIndex >= Steps.Length)
            {
                if (!completing)
                {
                    completing = true;
                    StartCoroutine(CompleteTutorial());
                }

                return;
            }

            TutorialStep step = Steps[stepIndex];
            trackerStepText.text = $"실전 교육  {stepIndex + 1}/{Steps.Length}";
            trackerTitleText.text = step.Title;
            trackerBodyText.text = step.Instruction;
            trackerKeyText.text = BuildKeyHint(step);
            progressBar.SetValueWithoutNotify((float)stepIndex / Steps.Length);
        }

        private IEnumerator CompleteTutorial()
        {
            requestedForNewGame = false;
            trackerStepText.text = "실전 교육 완료";
            trackerTitleText.text = "주막 운영 준비 완료!";
            trackerBodyText.text = "쓰레기·진상·먹튀는 빗자루로 해결하고, 아이템은 내려놓기 키로 바닥에 둘 수 있습니다.";
            trackerKeyText.text = "F1  튜토리얼 다시 보기   ·   ESC  설정";
            progressBar.SetValueWithoutNotify(1f);
            yield return new WaitForSecondsRealtime(5f);
            Destroy(gameObject);
        }

        private static string BuildKeyHint(TutorialStep step)
        {
            string key = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(step.Profile, step.KeyAction));
            return $"{PlayerControlBindings.GetProfileLabel(step.Profile)}  ·  {key}";
        }

        private void RestoreTimeScale()
        {
            if (!timeScaleCaptured)
            {
                return;
            }

            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            timeScaleCaptured = false;
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

            BuildIntro(font);
            BuildTracker(font);
            trackerRoot.SetActive(false);
        }

        private void BuildIntro(Font font)
        {
            introRoot = new GameObject("Intro", typeof(RectTransform));
            introRoot.transform.SetParent(transform, false);
            Stretch((RectTransform)introRoot.transform);

            Image dim = CreateImage(introRoot.transform, "Dim", new Color(0.025f, 0.018f, 0.012f, 0.82f));
            Stretch(dim.rectTransform);

            Image panel = CreateImage(dim.transform, "TutorialPanel", new Color32(250, 235, 200, 255));
            SetRect(panel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(1050f, 650f));
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(10f, -10f);

            Image header = CreateImage(panel.transform, "Header", new Color32(92, 48, 24, 255));
            SetRect(header.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 98f));

            Text title = CreateText(header.transform, "Title", "초보 주모 실전 교육", 35,
                new Color32(255, 221, 145, 255), font, FontStyle.Bold);
            Stretch(title.rectTransform, 12f);

            string hallKeys = BuildRoleKeys(PlayerControlProfile.Hall);
            string kitchenKeys = BuildRoleKeys(PlayerControlProfile.Kitchen);
            Text body = CreateText(panel.transform, "Body",
                "설명만 넘기는 튜토리얼이 아니라 실제 운영을 한 단계씩 연습합니다.\n\n" +
                $"홀 담당   {hallKeys}\n" +
                $"주방 담당   {kitchenKeys}\n\n" +
                "화면 왼쪽 목표를 따라 손님 맞이부터 조리와 서빙까지 완료하세요.\n" +
                "설정에서 키를 변경하면 튜토리얼 안내도 자동으로 바뀝니다.",
                28, new Color32(72, 53, 40, 255), font);
            body.alignment = TextAnchor.UpperLeft;
            body.lineSpacing = 1.25f;
            SetRect(body.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -20f), new Vector2(850f, 350f));

            Button start = CreateButton(panel.transform, "StartPractice", "실습 시작", font,
                new Color32(218, 157, 55, 255));
            SetRect(start.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-48f, 38f), new Vector2(250f, 72f));
            start.onClick.AddListener(BeginPractice);

            Button skip = CreateButton(panel.transform, "Skip", "건너뛰기", font,
                new Color32(117, 86, 57, 255));
            SetRect(skip.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(48f, 38f), new Vector2(210f, 72f));
            skip.onClick.AddListener(SkipTutorial);

            Text hint = CreateText(panel.transform, "Hint", "ENTER / SPACE  실습 시작   ·   ESC  건너뛰기", 19,
                new Color32(126, 92, 58, 255), font);
            SetRect(hint.rectTransform, new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 52f), new Vector2(0f, 38f));
        }

        private void BuildTracker(Font font)
        {
            trackerRoot = new GameObject("ObjectiveTracker", typeof(RectTransform), typeof(Image), typeof(Shadow));
            trackerRoot.transform.SetParent(transform, false);
            RectTransform rootRect = trackerRoot.GetComponent<RectTransform>();
            SetRect(rootRect, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(28f, 28f), new Vector2(590f, 245f));

            trackerRoot.GetComponent<Image>().color = new Color32(53, 31, 21, 238);
            Shadow shadow = trackerRoot.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(7f, -7f);

            trackerStepText = CreateText(trackerRoot.transform, "Step", string.Empty, 18,
                new Color32(232, 170, 72, 255), font, FontStyle.Bold);
            SetRect(trackerStepText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                new Vector2(24f, -16f), new Vector2(-48f, 30f));
            trackerStepText.alignment = TextAnchor.MiddleLeft;

            trackerTitleText = CreateText(trackerRoot.transform, "Objective", string.Empty, 28,
                new Color32(255, 235, 193, 255), font, FontStyle.Bold);
            SetRect(trackerTitleText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                new Vector2(24f, -50f), new Vector2(-48f, 42f));
            trackerTitleText.alignment = TextAnchor.MiddleLeft;

            trackerBodyText = CreateText(trackerRoot.transform, "Description", string.Empty, 19,
                new Color32(235, 217, 187, 255), font);
            SetRect(trackerBodyText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                new Vector2(24f, -94f), new Vector2(-48f, 76f));
            trackerBodyText.alignment = TextAnchor.UpperLeft;

            trackerKeyText = CreateText(trackerRoot.transform, "Key", string.Empty, 18,
                new Color32(244, 181, 80, 255), font, FontStyle.Bold);
            SetRect(trackerKeyText.rectTransform, new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 50f), new Vector2(0f, 32f));
            trackerKeyText.alignment = TextAnchor.MiddleLeft;

            Button stop = CreateButton(trackerRoot.transform, "StopTutorial", "종료", font,
                new Color32(108, 70, 52, 255), 17);
            SetRect(stop.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-18f, 43f), new Vector2(110f, 40f));
            stop.onClick.AddListener(SkipTutorial);

            progressBar = CreateProgressBar(trackerRoot.transform);
            SetRect(progressBar.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-38f, 18f));
        }

        private static string BuildRoleKeys(PlayerControlProfile profile)
        {
            string up = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.MoveUp));
            string down = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.MoveDown));
            string left = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.MoveLeft));
            string right = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.MoveRight));
            string interact = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.Interact));
            string drop = PlayerControlBindings.GetKeyLabel(PlayerControlBindings.Get(profile, PlayerControlAction.Drop));
            return $"이동 {up}/{down}/{left}/{right}  ·  상호작용 {interact}  ·  내려놓기 {drop}";
        }

        private static Slider CreateProgressBar(Transform parent)
        {
            GameObject root = new("Progress", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(parent, false);
            Image background = root.GetComponent<Image>();
            background.color = new Color(1f, 0.92f, 0.75f, 0.18f);
            Image fill = CreateImage(root.transform, "Fill", new Color32(222, 155, 47, 255));
            Stretch(fill.rectTransform, 3f);
            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = background;
            slider.transition = Selectable.Transition.None;
            return slider;
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Font font, Color color,
            int fontSize = 25)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(root.transform, "Label", label, fontSize, new Color32(255, 238, 205, 255), font,
                FontStyle.Bold);
            Stretch(text.rectTransform, 6f);
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

        private readonly struct TutorialStep
        {
            public readonly TutorialAction Action;
            public readonly string Title;
            public readonly string Instruction;
            public readonly PlayerControlProfile Profile;
            public readonly PlayerControlAction KeyAction;

            public TutorialStep(TutorialAction action, string title, string instruction,
                PlayerControlProfile profile, PlayerControlAction keyAction)
            {
                Action = action;
                Title = title;
                Instruction = instruction;
                Profile = profile;
                KeyAction = keyAction;
            }
        }
    }

    public sealed class TutorialLauncher : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current?.f1Key.wasPressedThisFrame == true &&
                !PauseSettingsMenu.IsPaused && !ReputationDeathEnding.IsActive && !CheatConsole.IsOpen)
            {
                TutorialOverlay.OpenManual();
            }
        }
    }
}
