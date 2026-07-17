using _001_Scripts._001_Manager;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _001_Scripts._004_UI._000_Panels
{
    // ESC로 여닫는 설정 패널.
    public sealed class SettingPanel : PanelBase
    {
        [SerializeField] private Key toggleKey = Key.Escape;
        [SerializeField] private Button closeButton;

        [Tooltip("설정을 여는 동안 게임을 멈춘다. 2인 로컬 협동이라 한 명이 열면 둘 다 멈춘다.")]
        [SerializeField] private bool pauseWhileOpen = true;

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

            // 설정을 연 채로 씬이 바뀌면 timeScale이 0인 채 남는다.
            if (pauseWhileOpen && IsOpen)
            {
                Time.timeScale = 1f;
            }

            base.OnDestroy();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                Toggle();
            }
        }

        protected override void OnOpened()
        {
            if (pauseWhileOpen)
            {
                Time.timeScale = 0f;
            }
        }

        protected override void OnClosed()
        {
            if (pauseWhileOpen)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
