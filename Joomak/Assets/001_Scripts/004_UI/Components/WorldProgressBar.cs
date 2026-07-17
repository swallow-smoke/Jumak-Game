using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    // 월드 오브젝트를 따라다니는 공용 진행 바. 런타임에만 UI를 만들어 프리팹 수정 없이 사용할 수 있다.
    [DisallowMultipleComponent]
    public sealed class WorldProgressBar : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new(0.12f, 0.08f, 0.05f, 0.92f);
        private static readonly Color TrackColor = new(0.27f, 0.20f, 0.14f, 0.96f);
        private static Font cachedKoreanFont;

        private GameObject uiRoot;
        private RectTransform fillRect;
        private Text label;
        private Vector3 worldOffset;
        private bool requestedVisible;

        private void Awake()
        {
            BuildVisual();
        }

        private void LateUpdate()
        {
            if (uiRoot != null && uiRoot.activeSelf)
            {
                uiRoot.transform.position = transform.position + worldOffset;
            }
        }

        private void OnEnable()
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(requestedVisible);
            }
        }

        private void OnDisable()
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (uiRoot != null)
            {
                Destroy(uiRoot);
            }
        }

        public void Configure(Vector3 offset, float worldScale, Color fillColor)
        {
            worldOffset = offset;
            if (uiRoot == null)
            {
                BuildVisual();
            }

            uiRoot.transform.localScale = Vector3.one * Mathf.Max(0.001f, worldScale);
            fillRect.GetComponent<Image>().color = fillColor;
            uiRoot.transform.position = transform.position + worldOffset;
        }

        public void SetProgress(float normalized, string text, bool visible)
        {
            requestedVisible = visible;
            if (uiRoot == null)
            {
                BuildVisual();
            }

            float progress = Mathf.Clamp01(normalized);
            fillRect.anchorMax = new Vector2(progress, 1f);
            label.text = text ?? string.Empty;
            uiRoot.SetActive(visible && isActiveAndEnabled);
        }

        private void BuildVisual()
        {
            if (uiRoot != null)
            {
                return;
            }

            uiRoot = new GameObject($"{name}_WorldProgress", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1150;

            RectTransform rootRect = uiRoot.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(180f, 34f);
            uiRoot.transform.localScale = Vector3.one * 0.007f;

            RectTransform background = CreateImage(uiRoot.transform, "Background", BackgroundColor);
            Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform track = CreateImage(background, "Track", TrackColor);
            Stretch(track, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            fillRect = CreateImage(track, "Fill", new Color(0.92f, 0.56f, 0.18f, 1f));
            Stretch(fillRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(background, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            label = labelObject.GetComponent<Text>();
            label.font = CreateKoreanFont();
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.supportRichText = true;

            uiRoot.SetActive(false);
        }

        private static RectTransform CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Font CreateKoreanFont()
        {
            cachedKoreanFont ??= Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 24)
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedKoreanFont;
        }
    }
}
