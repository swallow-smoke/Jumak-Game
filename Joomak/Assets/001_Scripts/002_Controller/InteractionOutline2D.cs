using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._002_Controller
{
    [DisallowMultipleComponent]
    public sealed class InteractionOutline2D : MonoBehaviour
    {
        private static readonly Vector2[] Directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        [SerializeField, Min(0f)] private float outlineThickness = 0.04f;
        [SerializeField, ColorUsage(true, true)] private Color outlineColor = new(2f, 1.4f, 0.15f, 0.9f);

        private readonly List<OutlineGroup> groups = new();
        private bool isHighlighted;

        public static InteractionOutline2D GetOrAdd(GameObject target)
        {
            if (!target.TryGetComponent(out InteractionOutline2D outline))
            {
                outline = target.AddComponent<InteractionOutline2D>();
            }

            return outline;
        }

        private void Awake()
        {
            BuildOutlineRenderers();
            SetHighlighted(false);
        }

        private void LateUpdate()
        {
            if (!isHighlighted)
            {
                return;
            }

            foreach (OutlineGroup group in groups)
            {
                SyncGroup(group);
            }
        }

        private void OnDisable()
        {
            SetHighlighted(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;

            foreach (OutlineGroup group in groups)
            {
                foreach (SpriteRenderer outline in group.Outlines)
                {
                    outline.enabled = highlighted && group.Source != null && group.Source.enabled;
                }
            }

            if (highlighted)
            {
                foreach (OutlineGroup group in groups)
                {
                    SyncGroup(group);
                }
            }
        }

        private void BuildOutlineRenderers()
        {
            SpriteRenderer[] sources = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer source in sources)
            {
                if (source.transform.name.StartsWith("__InteractionOutline"))
                {
                    continue;
                }

                SpriteRenderer[] outlines = new SpriteRenderer[Directions.Length];
                for (int i = 0; i < Directions.Length; i++)
                {
                    GameObject outlineObject = new($"__InteractionOutline_{i}");
                    outlineObject.transform.SetParent(source.transform, false);
                    outlines[i] = outlineObject.AddComponent<SpriteRenderer>();
                }

                OutlineGroup group = new(source, outlines);
                groups.Add(group);
                SyncGroup(group);
            }
        }

        private void SyncGroup(OutlineGroup group)
        {
            SpriteRenderer source = group.Source;
            if (source == null)
            {
                return;
            }

            Vector3 sourceScale = source.transform.lossyScale;
            float xScale = Mathf.Max(Mathf.Abs(sourceScale.x), 0.0001f);
            float yScale = Mathf.Max(Mathf.Abs(sourceScale.y), 0.0001f);

            for (int i = 0; i < group.Outlines.Length; i++)
            {
                SpriteRenderer outline = group.Outlines[i];
                Vector2 direction = Directions[i];

                outline.transform.localPosition = new Vector3(
                    direction.x * outlineThickness / xScale,
                    direction.y * outlineThickness / yScale,
                    0f);
                outline.sprite = source.sprite;
                outline.color = outlineColor;
                outline.flipX = source.flipX;
                outline.flipY = source.flipY;
                outline.drawMode = source.drawMode;
                outline.size = source.size;
                outline.tileMode = source.tileMode;
                outline.maskInteraction = source.maskInteraction;
                outline.spriteSortPoint = source.spriteSortPoint;
                outline.sortingLayerID = source.sortingLayerID;
                outline.sortingOrder = source.sortingOrder - 1;
                outline.sharedMaterial = source.sharedMaterial;
                outline.enabled = isHighlighted && source.enabled;
            }
        }

        private sealed class OutlineGroup
        {
            public readonly SpriteRenderer Source;
            public readonly SpriteRenderer[] Outlines;

            public OutlineGroup(SpriteRenderer source, SpriteRenderer[] outlines)
            {
                Source = source;
                Outlines = outlines;
            }
        }
    }
}
