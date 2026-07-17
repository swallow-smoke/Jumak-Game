using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    // 게임 수치에는 손대지 않고 성공한 동작의 위치만 잠깐 강조하는 공용 연출기.
    public sealed class GameplayFeedback : MonoBehaviour
    {
        private const float Lifetime = 0.65f;

        private static GameplayFeedback instance;
        private static Sprite squareSprite;
        private static Font koreanFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() => EnsureInstance();

        public static void Burst(
            Vector3 position,
            Color color,
            string label = null,
            int particleCount = 9)
        {
            EnsureInstance().StartCoroutine(
                EnsureInstance().AnimateBurst(position, color, label, Mathf.Clamp(particleCount, 0, 20)));
        }

        private static GameplayFeedback EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new("GameplayFeedback");
            instance = root.AddComponent<GameplayFeedback>();
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
            squareSprite ??= Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            koreanFont ??= FindKoreanFont();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private IEnumerator AnimateBurst(Vector3 position, Color color, string labelValue, int particleCount)
        {
            GameObject effectRoot = new("ActionFeedback");
            effectRoot.transform.position = position;

            List<Spark> sparks = new(particleCount);
            for (int i = 0; i < particleCount; i++)
            {
                GameObject sparkObject = new("Spark", typeof(SpriteRenderer));
                sparkObject.transform.SetParent(effectRoot.transform, false);
                sparkObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

                float angle = (360f / Mathf.Max(1, particleCount)) * i + Random.Range(-16f, 16f);
                float speed = Random.Range(0.8f, 1.65f);
                Vector2 velocity = Quaternion.Euler(0f, 0f, angle) * Vector2.up * speed;
                float size = Random.Range(0.07f, 0.14f);
                sparkObject.transform.localScale = Vector3.one * size;

                SpriteRenderer renderer = sparkObject.GetComponent<SpriteRenderer>();
                renderer.sprite = squareSprite;
                renderer.color = color;
                renderer.sortingOrder = 2500;
                sparks.Add(new Spark(sparkObject.transform, renderer, velocity, size));
            }

            CanvasGroup labelGroup = null;
            RectTransform labelRect = null;
            if (!string.IsNullOrWhiteSpace(labelValue))
            {
                GameObject labelRoot = new("FeedbackLabel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                labelRoot.transform.SetParent(effectRoot.transform, false);
                labelRoot.transform.localPosition = new Vector3(0f, 0.3f, -0.2f);
                labelRoot.transform.localScale = Vector3.one * 0.012f;

                Canvas canvas = labelRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 2600;
                labelGroup = labelRoot.GetComponent<CanvasGroup>();

                labelRect = labelRoot.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(180f, 48f);

                GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(labelRoot.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                Text text = textObject.GetComponent<Text>();
                text.text = labelValue;
                text.font = koreanFont;
                text.fontSize = 30;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = color;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            float elapsed = 0f;
            while (elapsed < Lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Lifetime);
                float alpha = 1f - t * t;

                foreach (Spark spark in sparks)
                {
                    if (spark.Transform == null)
                    {
                        continue;
                    }

                    spark.Transform.localPosition += (Vector3)(spark.Velocity * Time.unscaledDeltaTime);
                    spark.Transform.localScale = Vector3.one * (spark.StartSize * (1f - t));
                    Color faded = spark.Renderer.color;
                    faded.a = alpha;
                    spark.Renderer.color = faded;
                }

                if (labelRect != null)
                {
                    labelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 32f, t));
                    labelGroup.alpha = alpha;
                }

                yield return null;
            }

            Destroy(effectRoot);
        }

        private static Font FindKoreanFont()
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include))
            {
                if (text != null && text.font != null && text.font.name.Contains("CookieRun"))
                {
                    return text.font;
                }
            }

            return Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 30)
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private sealed class Spark
        {
            public readonly Transform Transform;
            public readonly SpriteRenderer Renderer;
            public readonly Vector2 Velocity;
            public readonly float StartSize;

            public Spark(Transform transform, SpriteRenderer renderer, Vector2 velocity, float startSize)
            {
                Transform = transform;
                Renderer = renderer;
                Velocity = velocity;
                StartSize = startSize;
            }
        }
    }
}
