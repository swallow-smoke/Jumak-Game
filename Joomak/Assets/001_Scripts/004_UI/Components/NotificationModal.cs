using System.Collections;
using System.Collections.Generic;
using _001_Scripts._000_Core;
using _001_Scripts._000_Core.MessageData;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public enum NotificationKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    // 화면 좌측 상단에 잠시 쌓였다가 투명해지며 사라지는 전역 알림 UI.
    // 씬 배치 없이 자동 생성되며 NotificationModal.Show(...)로 어디서든 사용할 수 있다.
    public sealed class NotificationModal : MonoBehaviour
    {
        private const int MaxVisibleCount = 4;
        private const float FadeInDuration = 0.18f;
        private const float FadeOutDuration = 0.65f;
        private const float SlideDistance = 36f;

        private static NotificationModal instance;

        private readonly List<ActiveNotification> activeNotifications = new();
        private readonly MessageSubscriptionBag subscriptions = new();

        private RectTransform listRoot;
        private Font koreanFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void Show(
            string message,
            NotificationKind kind = NotificationKind.Info,
            float visibleSeconds = 3f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnsureInstance().CreateNotification(message.Trim(), kind, Mathf.Max(0.5f, visibleSeconds));
        }

        public static void Important(string message, float visibleSeconds = 4f)
        {
            Show(message, NotificationKind.Warning, visibleSeconds);
        }

        private static NotificationModal EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new("NotificationModal");
            instance = root.AddComponent<NotificationModal>();
            DontDestroyOnLoad(root);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            BuildCanvas();
            SubscribeToImportantMessages();
        }

        private void OnDestroy()
        {
            subscriptions.Dispose();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void BuildCanvas()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            koreanFont = FindKoreanFont();

            GameObject listObject = new("NotificationList", typeof(RectTransform));
            listObject.transform.SetParent(transform, false);
            listRoot = (RectTransform)listObject.transform;
            listRoot.anchorMin = new Vector2(0f, 1f);
            listRoot.anchorMax = new Vector2(0f, 1f);
            listRoot.pivot = new Vector2(0f, 1f);
            listRoot.anchoredPosition = new Vector2(32f, -32f);
            listRoot.sizeDelta = new Vector2(500f, 0f);

            VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = listObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static Font FindKoreanFont()
        {
            Text[] sceneTexts = FindObjectsByType<Text>(FindObjectsInactive.Include);

            foreach (Text text in sceneTexts)
            {
                if (text != null && text.font != null && text.font.name.Contains("CookieRun"))
                {
                    return text.font;
                }
            }

            Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font font in loadedFonts)
            {
                if (font != null && font.name.Contains("CookieRun Regular"))
                {
                    return font;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void SubscribeToImportantMessages()
        {
            subscriptions.Add(KitchenMessagePort.OnOrderRequested(OnOrderRequested));
            subscriptions.Add(HallMessagePort.OnDishReady(OnDishReady));
            subscriptions.Add(HallMessagePort.OnIngredientSupplyRequested(OnIngredientSupplyRequested));
            subscriptions.Add(HallMessagePort.OnOrderStatusChanged(OnOrderStatusChanged));
            subscriptions.Add(KitchenMessagePort.OnBundleDelivered(OnBundleDelivered));
        }

        private void OnOrderRequested(BaseMessage message)
        {
            if (message.GetData<OrderRequestedMsgData>() != null)
            {
                Show("새 주문이 들어왔습니다.\n주문서를 확인하세요.", NotificationKind.Info);
            }
        }

        private void OnDishReady(DishReadyMsgData data)
        {
            Show("요리가 완성됐습니다.\n서빙대를 확인하세요.", NotificationKind.Success);
        }

        private void OnIngredientSupplyRequested(IngredientSupplyRequestedMsgData data)
        {
            string ingredient = string.IsNullOrWhiteSpace(data.IngredientId) ? "재료" : data.IngredientId;
            Important($"재료 보급 요청\n{ingredient} x{data.RequestedAmount}");
        }

        private void OnBundleDelivered(IngredientBundleDeliveredMsgData data)
        {
            Show("재료 상자가 주방으로 전달됐습니다.\n해체대에서 포장을 풀어주세요.", NotificationKind.Success, 4f);
        }

        private void OnOrderStatusChanged(OrderStatusChangedMsgData data)
        {
            if (data.Status is not (KitchenOrderStatus.Rejected or KitchenOrderStatus.Cancelled))
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(data.Reason) ? "주방 상태를 확인하세요." : data.Reason;
            Show($"주문이 취소되었습니다.\n{reason}", NotificationKind.Error, 4f);
        }

        private void CreateNotification(string message, NotificationKind kind, float visibleSeconds)
        {
            if (listRoot == null)
            {
                return;
            }

            if (activeNotifications.Count >= MaxVisibleCount)
            {
                RemoveImmediately(activeNotifications[0]);
            }

            GameObject item = new($"Notification_{kind}", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            item.transform.SetParent(listRoot, false);

            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 500f;
            layoutElement.preferredHeight = 104f;

            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject visual = CreateImage("Visual", item.transform, new Color(0.12f, 0.085f, 0.055f, 0.96f));
            RectTransform visualRect = (RectTransform)visual.transform;
            Stretch(visualRect);
            visualRect.anchoredPosition = new Vector2(-SlideDistance, 0f);

            Color accentColor = GetAccentColor(kind);
            GameObject accent = CreateImage("Accent", visual.transform, accentColor);
            RectTransform accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.sizeDelta = new Vector2(7f, 0f);
            accentRect.anchoredPosition = Vector2.zero;

            Text icon = CreateText("Icon", visual.transform, GetIcon(kind), 36, FontStyle.Bold);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(25f, 0f);
            iconRect.sizeDelta = new Vector2(48f, 0f);
            icon.alignment = TextAnchor.MiddleCenter;
            icon.color = accentColor;

            Text title = CreateText("Title", visual.transform, GetTitle(kind), 22, FontStyle.Bold);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(82f, -43f);
            titleRect.offsetMax = new Vector2(-22f, -10f);
            title.alignment = TextAnchor.MiddleLeft;
            title.color = accentColor;

            Text body = CreateText("Message", visual.transform, message, 18, FontStyle.Normal);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(82f, 12f);
            bodyRect.offsetMax = new Vector2(-22f, -43f);
            body.alignment = TextAnchor.UpperLeft;
            body.color = new Color(1f, 0.96f, 0.87f, 1f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            ActiveNotification notification = new(item, canvasGroup, visualRect);
            activeNotifications.Add(notification);
            notification.Routine = StartCoroutine(AnimateNotification(notification, visibleSeconds));
        }

        private IEnumerator AnimateNotification(ActiveNotification notification, float visibleSeconds)
        {
            float elapsed = 0f;
            while (elapsed < FadeInDuration && notification.Object != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                notification.Group.alpha = t;
                notification.Visual.anchoredPosition = new Vector2(Mathf.Lerp(-SlideDistance, 0f, eased), 0f);
                yield return null;
            }

            if (notification.Object == null)
            {
                yield break;
            }

            notification.Group.alpha = 1f;
            notification.Visual.anchoredPosition = Vector2.zero;

            elapsed = 0f;
            while (elapsed < visibleSeconds && notification.Object != null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < FadeOutDuration && notification.Object != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                notification.Group.alpha = 1f - t * t;
                yield return null;
            }

            RemoveImmediately(notification, false);
        }

        private void RemoveImmediately(ActiveNotification notification, bool stopRoutine = true)
        {
            if (notification == null)
            {
                return;
            }

            activeNotifications.Remove(notification);
            if (stopRoutine && notification.Routine != null)
            {
                StopCoroutine(notification.Routine);
            }

            if (notification.Object != null)
            {
                Destroy(notification.Object);
            }
        }

        private Text CreateText(string objectName, Transform parent, string value, int size, FontStyle style)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.supportRichText = true;
            text.raycastTarget = false;
            if (koreanFont != null)
            {
                text.font = koreanFont;
            }

            return text;
        }

        private static GameObject CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return imageObject;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string GetTitle(NotificationKind kind) => kind switch
        {
            NotificationKind.Success => "완료",
            NotificationKind.Warning => "중요 알림",
            NotificationKind.Error => "경고",
            _ => "알림"
        };

        private static string GetIcon(NotificationKind kind) => kind switch
        {
            NotificationKind.Success => "✓",
            NotificationKind.Warning => "!",
            NotificationKind.Error => "!",
            _ => "i"
        };

        private static Color GetAccentColor(NotificationKind kind) => kind switch
        {
            NotificationKind.Success => new Color(0.4f, 0.84f, 0.48f, 1f),
            NotificationKind.Warning => new Color(1f, 0.7f, 0.2f, 1f),
            NotificationKind.Error => new Color(0.95f, 0.32f, 0.25f, 1f),
            _ => new Color(0.42f, 0.72f, 1f, 1f)
        };

        private sealed class ActiveNotification
        {
            public readonly GameObject Object;
            public readonly CanvasGroup Group;
            public readonly RectTransform Visual;
            public Coroutine Routine;

            public ActiveNotification(GameObject gameObject, CanvasGroup group, RectTransform visual)
            {
                Object = gameObject;
                Group = group;
                Visual = visual;
            }
        }
    }
}
