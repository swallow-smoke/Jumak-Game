using System;
using System.Collections.Generic;
using System.Linq;
using _001_Scripts._001_Manager;
using _001_Scripts._003_Object._000_Structure.Hall;
using _001_Scripts._003_Object._000_Structure.Inventory;
using _001_Scripts._003_Object._001_Entity;
using _001_Scripts._005_Data.Upgrade;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public sealed class CheatConsole : MonoBehaviour
    {
        private const string GameSceneName = "InGame";
        private const string UpgradeSceneName = "Upgrade";

        private GameObject panelRoot;
        private Text statusText;
        private float previousTimeScale = 1f;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            IsOpen = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (scene.name is not (GameSceneName or UpgradeSceneName) || FindAnyObjectByType<CheatConsole>() != null)
            {
                return;
            }

            new GameObject("CheatConsole").AddComponent<CheatConsole>();
        }

        private void Update()
        {
            if (Keyboard.current?.f10Key.wasPressedThisFrame != true || ReputationDeathEnding.IsActive)
            {
                return;
            }

            Toggle();
        }

        private void OnDestroy()
        {
            if (IsOpen)
            {
                IsOpen = false;
                RestoreTimeScale();
            }
        }

        private void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            if (panelRoot == null)
            {
                BuildUi();
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            IsOpen = true;
            panelRoot.SetActive(true);
            statusText.text = "치트를 선택하세요. 변경 사항은 즉시 적용됩니다.";
        }

        private void Close()
        {
            IsOpen = false;
            panelRoot?.SetActive(false);
            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = previousTimeScale <= 0f && !PauseSettingsMenu.IsPaused && !TutorialOverlay.IsOpen
                ? 1f
                : previousTimeScale;
        }

        private void BuildUi()
        {
            Font font = FindFont();
            panelRoot = new GameObject("CheatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            panelRoot.transform.SetParent(transform, false);

            Canvas canvas = panelRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000;

            CanvasScaler scaler = panelRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image dim = CreateImage(panelRoot.transform, "Dim", new Color(0.02f, 0.015f, 0.012f, 0.72f));
            Stretch(dim.rectTransform);

            Image window = CreateImage(dim.transform, "Window", new Color32(45, 31, 25, 252));
            SetRect(window.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(1120f, 760f));
            Outline outline = window.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(224, 157, 57, 230);
            outline.effectDistance = new Vector2(3f, -3f);
            Shadow shadow = window.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(10f, -10f);

            Text title = CreateText(window.transform, "Title", "개발자 치트 콘솔", 40,
                new Color32(255, 214, 126, 255), font, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(-120f, 64f));

            Text closeHint = CreateText(window.transform, "CloseHint", "F10 닫기", 19,
                new Color32(205, 181, 150, 255), font);
            SetRect(closeHint.rectTransform, new Vector2(0.78f, 1f), Vector2.one, new Vector2(1f, 1f),
                new Vector2(-28f, -48f), new Vector2(0f, 40f));
            closeHint.alignment = TextAnchor.MiddleRight;

            List<CheatButtonData> cheats = new()
            {
                new("돈 +1,000전", AddMoney, new Color32(168, 112, 42, 255)),
                new("명성 +20", AddReputation, new Color32(157, 64, 54, 255)),
                new("재료 전체 충전", FillIngredients, new Color32(55, 124, 75, 255)),
                new("재료 즉시 배달", DeliverIngredients, new Color32(73, 112, 151, 255)),
                new("손님 즉시 생성", SpawnCustomer, new Color32(124, 77, 143, 255)),
                new("쓰레기 이벤트", SpawnTrash, new Color32(112, 89, 63, 255)),
                new("촛불 끄기 이벤트", ExtinguishCandle, new Color32(83, 91, 118, 255)),
                new("문제 전부 정리", ClearProblems, new Color32(55, 125, 119, 255)),
                new("오늘 즉시 정산", EndDay, new Color32(139, 73, 48, 255))
            };

            const float startX = -350f;
            const float startY = 210f;
            const float gapX = 350f;
            const float gapY = 145f;
            for (int i = 0; i < cheats.Count; i++)
            {
                CheatButtonData cheat = cheats[i];
                int column = i % 3;
                int row = i / 3;
                Button button = CreateButton(window.transform, $"Cheat_{i}", cheat.Label, font, cheat.Color);
                SetRect(button.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
                    Vector2.one * 0.5f, new Vector2(startX + column * gapX, startY - row * gapY),
                    new Vector2(300f, 100f));
                Action action = cheat.Action;
                button.onClick.AddListener(() => action());
            }

            statusText = CreateText(window.transform, "Status", string.Empty, 20,
                new Color32(239, 218, 182, 255), font);
            SetRect(statusText.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 68f), new Vector2(0f, 48f));

            Button close = CreateButton(window.transform, "Close", "닫기", font, new Color32(89, 62, 50, 255));
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(150f, 54f));
            close.onClick.AddListener(Close);
        }

        private void AddMoney()
        {
            int current = UpgradeApi.AddMoney(1000);
            SetStatus($"돈 +1,000전  ·  현재 {current}전");
        }

        private void AddReputation()
        {
            if (ReputationManager.Instance == null)
            {
                SetStatus("ReputationManager를 찾을 수 없습니다.", true);
                return;
            }

            ReputationManager.Instance.Restore(20);
            SetStatus($"명성 +20  ·  현재 {ReputationManager.Instance.Current}/{ReputationManager.Instance.MaxValue}");
        }

        private void FillIngredients()
        {
            SupplyBox[] boxes = FindObjectsByType<SupplyBox>(FindObjectsInactive.Exclude);
            int added = boxes.Sum(box => box.AddStock(box.MaxStack));
            SetStatus($"보급함 {boxes.Length}개 충전 완료  ·  재고 +{added}");
        }

        private void DeliverIngredients()
        {
            int delivered = EventManager.Instance != null ? EventManager.Instance.TryDeliverIngredients() : 0;
            SetStatus(delivered > 0 ? $"재료 상자 {delivered}개를 즉시 배달했습니다." : "배달에 실패했습니다.", delivered <= 0);
        }

        private void SpawnCustomer()
        {
            bool spawned = HallManager.Instance != null && HallManager.Instance.TrySpawnCustomer();
            SetStatus(spawned ? "손님을 즉시 생성했습니다." : "입구 대기 공간이 꽉 찼거나 생성 설정이 없습니다.", !spawned);
        }

        private void SpawnTrash()
        {
            bool spawned = EventManager.Instance != null && EventManager.Instance.TrySpawnTrash();
            SetStatus(spawned ? "홀에 쓰레기를 생성했습니다." : "쓰레기 생성에 실패했습니다.", !spawned);
        }

        private void ExtinguishCandle()
        {
            bool changed = EventManager.Instance != null && EventManager.Instance.TryExtinguishCandle();
            SetStatus(changed ? "촛불 하나를 껐습니다." : "끌 수 있는 촛불이 없습니다.", !changed);
        }

        private void ClearProblems()
        {
            Trash[] trash = FindObjectsByType<Trash>(FindObjectsInactive.Exclude);
            foreach (Trash target in trash)
            {
                Destroy(target.gameObject);
            }

            int relit = 0;
            foreach (Candle candle in FindObjectsByType<Candle>(FindObjectsInactive.Exclude))
            {
                if (!candle.IsLit)
                {
                    candle.Interact(null);
                    relit++;
                }
            }

            SetStatus($"문제 정리 완료  ·  쓰레기 {trash.Length}개 제거 / 촛불 {relit}개 점화");
        }

        private void EndDay()
        {
            DayCycleManager day = FindAnyObjectByType<DayCycleManager>();
            if (day == null)
            {
                SetStatus("DayCycleManager를 찾을 수 없습니다.", true);
                return;
            }

            Close();
            day.CheatEndDay();
        }

        private void SetStatus(string message, bool error = false)
        {
            statusText.text = message;
            statusText.color = error ? new Color32(255, 126, 105, 255) : new Color32(239, 218, 182, 255);
            Debug.Log($"[Cheat] {message}");
        }

        private static Font FindFont()
        {
            Font cookie = Resources.FindObjectsOfTypeAll<Font>()
                .FirstOrDefault(candidate => candidate != null && candidate.name.Contains("CookieRun"));
            return cookie != null
                ? cookie
                : Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 28);
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
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(5f, -5f);
            Text text = CreateText(root.transform, "Label", label, 22, new Color32(255, 240, 211, 255), font,
                FontStyle.Bold);
            Stretch(text.rectTransform, 8f);
            return button;
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

        private readonly struct CheatButtonData
        {
            public readonly string Label;
            public readonly Action Action;
            public readonly Color Color;

            public CheatButtonData(string label, Action action, Color color)
            {
                Label = label;
                Action = action;
                Color = color;
            }
        }
    }
}
