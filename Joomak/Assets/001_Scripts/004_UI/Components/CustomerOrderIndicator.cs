using _001_Scripts._003_Object._001_Entity.NPC;
using _001_Scripts._001_Manager;
using UnityEngine;

namespace _001_Scripts._004_UI.Components
{
    [DisallowMultipleComponent]
    public sealed class CustomerOrderIndicator : MonoBehaviour
    {
        private static readonly Color ThinkingColor = new(1f, 0.75f, 0.16f, 1f);
        private static readonly Color ReadyColor = new(1f, 0.25f, 0.12f, 1f);

        [SerializeField] private Vector3 worldOffset = new(0f, 2.05f, -0.2f);
        [SerializeField, Min(0.1f)] private float bubbleScale = 1.45f;

        private GameObject indicatorRoot;
        private UnityEngine.UI.Text symbolText;
        private CustomerState visibleState = (CustomerState)(-1);
        private AudioClip questionSfx;
        private AudioClip exclamationSfx;

        private void Awake()
        {
            BuildIndicator();
            SetCustomerState(CustomerState.WaitingForSeat);
        }

        private void LateUpdate()
        {
            if (indicatorRoot == null || !indicatorRoot.activeSelf)
            {
                return;
            }

            indicatorRoot.transform.position = transform.position + worldOffset;
        }

        private void OnDestroy()
        {
            if (indicatorRoot != null)
            {
                Destroy(indicatorRoot);
            }
        }

        public void SetCustomerState(CustomerState state)
        {
            if (visibleState == state)
            {
                return;
            }

            visibleState = state;
            switch (state)
            {
                case CustomerState.Deciding:
                    if (indicatorRoot != null)
                    {
                        Show("?", ThinkingColor);
                    }

                    AudioManager.Instance?.PlaySfx(questionSfx);
                    break;

                case CustomerState.ReadyToOrder:
                    if (indicatorRoot != null)
                    {
                        Show("!", ReadyColor);
                    }

                    AudioManager.Instance?.PlaySfx(exclamationSfx);
                    break;

                default:
                    if (indicatorRoot != null)
                    {
                        indicatorRoot.SetActive(false);
                    }
                    break;
            }
        }

        public void ConfigureAudio(AudioClip questionClip, AudioClip exclamationClip)
        {
            questionSfx = questionClip;
            exclamationSfx = exclamationClip;
        }

        private void Show(string symbol, Color color)
        {
            string htmlColor = ColorUtility.ToHtmlStringRGB(color);
            symbolText.text = $"<color=#{htmlColor}><b>{symbol}</b></color>";
            symbolText.fontSize = 58;
            symbolText.resizeTextForBestFit = true;
            symbolText.resizeTextMinSize = 44;
            symbolText.resizeTextMaxSize = 72;
            symbolText.alignment = TextAnchor.MiddleCenter;
            symbolText.horizontalOverflow = HorizontalWrapMode.Overflow;
            symbolText.verticalOverflow = VerticalWrapMode.Overflow;

            indicatorRoot.transform.position = transform.position + worldOffset;
            indicatorRoot.transform.localScale = Vector3.one * bubbleScale;
            indicatorRoot.SetActive(true);
        }

        private void BuildIndicator()
        {
            InteractionPromptBubble prompt = GetComponent<InteractionPromptBubble>();
            if (prompt == null || !prompt.TryCreateVisualClone(out indicatorRoot, out symbolText))
            {
                Debug.LogWarning($"{name}: 주문 상태에 사용할 상호작용 버블을 찾지 못했습니다.", this);
                return;
            }

            indicatorRoot.name = $"{name}_OrderStatusBubble";
            indicatorRoot.transform.position = transform.position + worldOffset;
            indicatorRoot.transform.rotation = Quaternion.identity;
            indicatorRoot.transform.localScale = Vector3.one * bubbleScale;
            indicatorRoot.SetActive(false);
        }
    }
}
