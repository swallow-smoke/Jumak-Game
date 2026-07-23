using System.Collections;
using _001_Scripts._000_Core;
using _001_Scripts._004_UI.Components;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._001_Manager
{
    // InGame의 하루 타이머, 영업 종료 정산, Upgrade 씬의 다음 날 시작 흐름을 관리한다.
    public sealed class DayCycleManager : MonoBehaviour
    {
        private const string GameSceneName = "InGame";
        private const string UpgradeSceneName = "Upgrade";

        private static int currentDay = 1;
        private static int lastDayRevenue;
        private static int lastDayTotalMoney;
        private static bool resumePending;
        private static bool resumeDayInProgress;
        private static float resumeRemainingSeconds;
        private static int resumeRevenue;

        [Header("Day Settings")]
        [SerializeField, Min(10f)] private float dayDurationSeconds = 180f;
        [SerializeField, Min(0f)] private float settlementDisplaySeconds = 3f;

        private float remainingSeconds;
        private int revenueThisDay;
        private int lastKnownMoney;
        private bool dayEnding;
        private Text dayText;
        private Canvas runtimeCanvas;
        private float autoSaveTimer;

        public static int CurrentDay => currentDay;
        public static int LastDayRevenue => lastDayRevenue;

        public void CheatEndDay()
        {
            if (!dayEnding && SceneManager.GetActiveScene().name == GameSceneName)
            {
                remainingSeconds = 0f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneLoadedHook()
        {
            // Title -> Loading -> InGame처럼 실행 중 씬을 바꾸는 경우에도 매번 일차 관리자를 보장한다.
            // 에디터에서 Domain Reload를 꺼도 중복 구독되지 않도록 먼저 제거한다.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureManagerForScene(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            EnsureManagerForScene(scene.name);
        }

        private static void EnsureManagerForScene(string sceneName)
        {
            if (sceneName is not (GameSceneName or UpgradeSceneName) ||
                FindAnyObjectByType<DayCycleManager>() != null)
            {
                return;
            }

            new GameObject("DayCycleManager").AddComponent<DayCycleManager>();
        }

        public static void ResetRun()
        {
            currentDay = 1;
            lastDayRevenue = 0;
            lastDayTotalMoney = 0;
            resumePending = false;
            resumeDayInProgress = false;
            resumeRemainingSeconds = 0f;
            resumeRevenue = 0;
        }

        public static void RestoreProgress(SaveGameData data)
        {
            if (data == null)
            {
                return;
            }

            currentDay = Mathf.Max(1, data.currentDay);
            lastDayRevenue = Mathf.Max(0, data.lastDayRevenue);
            lastDayTotalMoney = Mathf.Max(0, data.money);
            resumePending = true;
            resumeDayInProgress = data.dayInProgress;
            resumeRemainingSeconds = Mathf.Max(0f, data.remainingDaySeconds);
            resumeRevenue = Mathf.Max(0, data.revenueThisDay);
        }

        public static void SaveCurrentProgress(string sceneName = null)
        {
            DayCycleManager manager = FindAnyObjectByType<DayCycleManager>();
            if (manager != null)
            {
                manager.SaveProgress(sceneName);
                return;
            }

            SaveGameManager.Save(CreateSaveData(
                sceneName ?? SceneManager.GetActiveScene().name,
                false,
                0f,
                0));
        }

        private void Start()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == GameSceneName)
            {
                StartDay();
            }
            else if (sceneName == UpgradeSceneName)
            {
                BuildUpgradeContinueUi();
            }
        }

        private void OnDestroy()
        {
            UpgradeApi.MoneyChanged -= OnMoneyChanged;
        }

        private void Update()
        {
            if (dayEnding || SceneManager.GetActiveScene().name != GameSceneName)
            {
                return;
            }

            // 실습형 튜토리얼 중에는 게임은 움직이되 하루 제한시간만 멈춘다.
            // 배우는 도중 정산 씬으로 넘어가 튜토리얼이 끊기는 것을 막는다.
            if (!TutorialOverlay.IsRunning)
            {
                remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            }
            autoSaveTimer += Time.unscaledDeltaTime;
            if (autoSaveTimer >= 10f)
            {
                autoSaveTimer = 0f;
                SaveProgress(GameSceneName);
            }

            RefreshDayText();
            if (remainingSeconds <= 0f)
            {
                StartCoroutine(EndDay());
            }
        }

        private void StartDay()
        {
            Time.timeScale = 1f;
            if (resumePending && resumeDayInProgress)
            {
                remainingSeconds = Mathf.Clamp(resumeRemainingSeconds, 1f, dayDurationSeconds);
                revenueThisDay = resumeRevenue;
            }
            else
            {
                remainingSeconds = dayDurationSeconds;
                revenueThisDay = 0;
            }

            resumePending = false;
            resumeDayInProgress = false;
            lastKnownMoney = UpgradeApi.Money;
            UpgradeApi.MoneyChanged += OnMoneyChanged;
            BuildDayHud();
            RefreshDayText();
            NotificationModal.Show($"{currentDay}일차 영업을 시작합니다.", NotificationKind.Info, 3f);
            SaveProgress(GameSceneName);
        }

        private void OnMoneyChanged(int value)
        {
            if (value > lastKnownMoney)
            {
                revenueThisDay += value - lastKnownMoney;
            }

            lastKnownMoney = value;
        }

        private IEnumerator EndDay()
        {
            if (dayEnding)
            {
                yield break;
            }

            dayEnding = true;
            UpgradeApi.MoneyChanged -= OnMoneyChanged;
            lastDayRevenue = revenueThisDay;
            lastDayTotalMoney = UpgradeApi.Money;
            Time.timeScale = 0f;

            SaveGameManager.Save(CreateSaveData(UpgradeSceneName, false, 0f, 0));

            BuildSettlementPanel();

            float elapsed = 0f;
            while (elapsed < settlementDisplaySeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Time.timeScale = 1f;
            SceneLoader.LoadThroughLoading(UpgradeSceneName);
        }

        private void StartNextDay()
        {
            currentDay++;
            resumePending = false;
            SaveGameManager.Save(CreateSaveData(GameSceneName, false, 0f, 0));
            SceneLoader.LoadThroughLoading(GameSceneName);
        }

        private void BuildDayHud()
        {
            runtimeCanvas = CreateCanvas("DayCycleCanvas", 4500);
            Image badge = CreateImage(runtimeCanvas.transform, "DayBadge", new Color(0.12f, 0.075f, 0.045f, 0.92f));
            SetRect(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(390f, 86f));

            dayText = CreateText(badge.transform, "DayText", string.Empty, 30, new Color32(255, 239, 199, 255), FontStyle.Bold);
            Stretch(dayText.rectTransform, 14f);
        }

        private void RefreshDayText()
        {
            if (dayText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(remainingSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            dayText.text = $"{currentDay}일차  ·  {minutes:00}:{seconds:00}";
        }

        private void BuildSettlementPanel()
        {
            if (runtimeCanvas == null)
            {
                runtimeCanvas = CreateCanvas("DayCycleCanvas", 4500);
            }

            Image dim = CreateImage(runtimeCanvas.transform, "SettlementDim", new Color(0f, 0f, 0f, 0.72f));
            Stretch(dim.rectTransform);

            Image panel = CreateImage(dim.transform, "SettlementPanel", new Color32(255, 246, 223, 255));
            SetRect(panel.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(680f, 430f));
            panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);

            Text title = CreateText(panel.transform, "Title", $"{currentDay}일차 영업 종료", 46,
                new Color32(108, 53, 34, 255), FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -66f), new Vector2(580f, 80f));

            Text revenue = CreateText(panel.transform, "Revenue", $"오늘 매출  +{lastDayRevenue}전", 34,
                new Color32(62, 125, 76, 255), FontStyle.Bold);
            SetRect(revenue.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, 10f), new Vector2(560f, 65f));

            Text total = CreateText(panel.transform, "Total", $"보유 금액  {lastDayTotalMoney}전", 29,
                new Color32(61, 39, 29, 255));
            SetRect(total.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                new Vector2(0f, -62f), new Vector2(560f, 55f));

            Text hint = CreateText(panel.transform, "Hint", "잠시 후 업그레이드 화면으로 이동합니다", 22,
                new Color32(111, 98, 86, 255));
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 45f), new Vector2(580f, 50f));
        }

        private void BuildUpgradeContinueUi()
        {
            SaveGameManager.Save(CreateSaveData(UpgradeSceneName, false, 0f, 0));
            Canvas canvas = CreateCanvas("NextDayCanvas", 6000);

            Image summary = CreateImage(canvas.transform, "SettlementSummary", new Color(0.12f, 0.075f, 0.045f, 0.94f));
            SetRect(summary.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -28f), new Vector2(430f, 100f));
            Text summaryText = CreateText(summary.transform, "SummaryText",
                $"{currentDay}일차 정산 완료\n매출 +{lastDayRevenue}전  ·  보유 {UpgradeApi.Money}전", 22,
                new Color32(255, 239, 199, 255), FontStyle.Bold);
            Stretch(summaryText.rectTransform, 14f);

            Button nextButton = CreateButton(canvas.transform, "NextDayButton", $"{currentDay + 1}일차 시작");
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-28f, -28f), new Vector2(300f, 76f));
            nextButton.onClick.AddListener(StartNextDay);
            EnsureEventSystem();
        }

        private void SaveProgress(string sceneName)
        {
            bool inProgress = SceneManager.GetActiveScene().name == GameSceneName && !dayEnding;
            SaveGameManager.Save(CreateSaveData(
                sceneName ?? SceneManager.GetActiveScene().name,
                inProgress,
                inProgress ? remainingSeconds : 0f,
                inProgress ? revenueThisDay : 0));
        }

        private static SaveGameData CreateSaveData(string sceneName, bool inProgress, float remaining, int revenue)
        {
            return new SaveGameData
            {
                currentDay = currentDay,
                money = UpgradeApi.Money,
                reputation = UpgradeApi.Reputation,
                maxReputation = UpgradeApi.MaxReputation,
                upgrades = UpgradeApi.CaptureUpgradeLevels(),
                remainingDaySeconds = remaining,
                revenueThisDay = revenue,
                lastDayRevenue = lastDayRevenue,
                dayInProgress = inProgress,
                sceneName = sceneName
            };
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveProgress(null);
            }
        }

        private void OnApplicationQuit()
        {
            SaveProgress(null);
        }

        private static Canvas CreateCanvas(string objectName, int sortingOrder)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Font FindFont()
        {
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font != null && font.name.Contains("CookieRun"))
                {
                    return font;
                }
            }

            return Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 28);
        }

        private static Text CreateText(Transform parent, string objectName, string value, int size, Color color,
            FontStyle style = FontStyle.Normal)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.font = FindFont();
            text.text = value;
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

        private static Button CreateButton(Transform parent, string objectName, string label)
        {
            GameObject root = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = new Color32(215, 154, 59, 255);
            root.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.4f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(root.transform, "Label", label, 27, new Color32(39, 24, 17, 255), FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
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
    }
}
