using _001_Scripts._001_Manager;
using _001_Scripts._004_UI.Interface;
using UnityEngine;

namespace _001_Scripts._004_UI
{
    // 모든 패널의 부모. 상속받은 패널은 Start에 UIManager로 자기 자신을 등록한다.
    //
    // 열고 닫을 때 GameObject를 끄지 않고 alpha/raycast만 조절한다.
    // 꺼버리면 퇴장 연출이 재생되기도 전에 오브젝트가 죽어서 트윈이 잘린다.
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PanelBase : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float transitionSeconds = 0.2f;
        [SerializeField] private bool openOnStart;

        [Tooltip("HUD처럼 항상 떠 있어야 하는 패널. UIManager.CloseAll()이 건너뛴다.")]
        [SerializeField] private bool alwaysVisible;

        private CanvasGroup canvasGroup;
        private IUIAnimator animator;

        public bool IsOpen { get; private set; }
        public bool AlwaysVisible => alwaysVisible;
        protected float TransitionSeconds => transitionSeconds;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            // 구현체(UIComponent)가 없으면 null이고, 그 경우 즉시 전환된다.
            animator = GetComponent<IUIAnimator>();
        }

        protected virtual void Start()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterPanel(this);
            }

            ApplyImmediate(openOnStart || alwaysVisible);
        }

        protected virtual void OnDestroy()
        {
            animator?.StopAll();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UnregisterPanel(this);
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            SetInputBlocking(true);

            if (animator != null)
            {
                animator.StopAll();
                animator.FadeIn(transitionSeconds);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }

            OnOpened();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            // 연출이 끝나기 전에도 클릭이 통과하면 안 되므로 raycast는 즉시 끊는다.
            SetInputBlocking(false);

            if (animator != null)
            {
                animator.StopAll();
                animator.FadeOut(transitionSeconds);
            }
            else
            {
                canvasGroup.alpha = 0f;
            }

            OnClosed();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Open();
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void OnClosed()
        {
        }

        private void ApplyImmediate(bool open)
        {
            IsOpen = open;
            canvasGroup.alpha = open ? 1f : 0f;
            SetInputBlocking(open);

            if (open)
            {
                OnOpened();
            }
        }

        private void SetInputBlocking(bool enabled)
        {
            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }
    }
}
