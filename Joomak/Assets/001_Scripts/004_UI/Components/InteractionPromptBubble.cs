using UnityEngine;
using _001_Scripts._002_Controller;

namespace _001_Scripts._004_UI.Components
{
    public sealed class InteractionPromptBubble : MonoBehaviour
    {
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private UnityEngine.UI.Text promptText;
        [SerializeField] private string title = "상호작용";
        [SerializeField] private string actionHint = "E 상호작용";
        [SerializeField] private Vector3 worldOffset = new(0f, 1.2f, 0f);
        [SerializeField, Min(0.1f)] private float worldScale = 1.15f;
        private PlayerController focusedPlayer;

        private void Awake()
        {
            if (bubbleRoot == null)
            {
                return;
            }

            bubbleRoot.transform.position = transform.position + worldOffset;
            bubbleRoot.transform.localScale = Vector3.one * worldScale;
            RefreshText(null);
            bubbleRoot.SetActive(false);
        }

        public void SetFocused(PlayerController player, bool focused)
        {
            if (bubbleRoot == null)
            {
                return;
            }

            if (focused)
            {
                focusedPlayer = player;
                bubbleRoot.transform.position = transform.position + worldOffset;
                RefreshText(player);
                bubbleRoot.SetActive(true);
                return;
            }

            if (focusedPlayer != null && focusedPlayer != player)
            {
                return;
            }

            focusedPlayer = null;
            bubbleRoot.SetActive(false);
        }

        private void OnDisable()
        {
            focusedPlayer = null;
            if (bubbleRoot != null)
            {
                bubbleRoot.SetActive(false);
            }
        }

        public bool TryCreateVisualClone(out GameObject clone, out UnityEngine.UI.Text text)
        {
            clone = null;
            text = null;
            if (bubbleRoot == null)
            {
                return false;
            }

            clone = Instantiate(bubbleRoot);
            clone.name = "OrderStatusBubble";
            text = clone.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (text == null)
            {
                Destroy(clone);
                clone = null;
                return false;
            }

            clone.SetActive(false);
            return true;
        }

        private void RefreshText(PlayerController player)
        {
            if (promptText == null)
            {
                return;
            }

            string hint = actionHint.StartsWith("E ") ? actionHint[2..] : actionHint;
            string keyLabel = player != null ? player.InteractKeyLabel : "E";
            promptText.text = $"<color=#9B5A2E><b>{title}</b></color>\n<size=70%><color=#6F6256>{keyLabel} {hint}</color></size>";
        }
    }
}
