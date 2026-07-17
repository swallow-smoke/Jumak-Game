using _001_Scripts._001_Manager;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts._004_UI.Components
{
    public sealed class ReputationSliderView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Text valueText;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color lowColor = new(0.72f, 0.2f, 0.15f, 1f);
        [SerializeField] private Color highColor = new(0.95f, 0.67f, 0.18f, 1f);

        private ReputationManager reputation;
        private float bindRetryTimer;

        private void Start() => TryBind();

        private void Update()
        {
            if (reputation != null)
            {
                return;
            }

            bindRetryTimer -= Time.unscaledDeltaTime;
            if (bindRetryTimer <= 0f)
            {
                bindRetryTimer = 0.5f;
                TryBind();
            }
        }

        private void OnDisable() => Unbind();

        private void TryBind()
        {
            ReputationManager manager = ReputationManager.Instance;
            if (manager == null || manager == reputation)
            {
                return;
            }

            Unbind();
            reputation = manager;
            reputation.Changed += Refresh;
            Refresh(reputation.Current);
        }

        private void Unbind()
        {
            if (reputation != null)
            {
                reputation.Changed -= Refresh;
                reputation = null;
            }
        }

        private void Refresh(int value)
        {
            int max = reputation != null ? Mathf.Max(1, reputation.MaxValue) : 100;
            float normalized = Mathf.Clamp01(value / (float)max);

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = max;
                slider.SetValueWithoutNotify(value);
            }

            if (valueText != null)
            {
                valueText.text = $"명성  {value} / {max}";
            }

            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(lowColor, highColor, normalized);
            }
        }
    }
}
