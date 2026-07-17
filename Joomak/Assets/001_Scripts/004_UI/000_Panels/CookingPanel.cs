using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // 조리 패널. 여기서는 '뜨고 닫히는 것'까지만 담당한다.
    // 조리 UI 내용(재료 선택, 레시피 표시 등)은 주방 담당이 이 클래스에 이어서 구현한다.
    //
    // 여는 쪽에서: UIManager.Instance.OpenPanel<CookingPanel>()
    public sealed class CookingPanel : PanelBase
    {
        [SerializeField] private Key closeKey = Key.Escape;
        [SerializeField] private Button closeButton;

        // 어느 조리대에서 열었는지. 주방 담당이 내용을 채울 때 필요할 것 같아 남겨둔다.
        public GameObject Source { get; private set; }

        protected override void Start()
        {
            base.Start();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        protected override void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[closeKey].wasPressedThisFrame)
            {
                Close();
            }
        }

        public void OpenFrom(GameObject source)
        {
            Source = source;
            Open();
        }

        protected override void OnClosed()
        {
            Source = null;
        }
    }
}
