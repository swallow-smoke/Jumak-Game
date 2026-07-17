using System;
using _001_Scripts._004_UI.Interface;
using DG.Tweening;
using UnityEngine;

namespace _001_Scripts._004_UI
{
    // IUIAnimator의 DOTween 구현체. PanelBase와 같은 오브젝트에 붙이면
    // PanelBase.Open()/Close()가 GetComponent<IUIAnimator>()로 이걸 찾아 쓴다.
    // 없으면 PanelBase가 알아서 즉시 전환(alpha 0/1)으로 대체한다.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIComponent : MonoBehaviour, IUIAnimator
    {
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Tween fadeTween;
        private Tween moveTween;

        public bool IsAnimating =>
            (fadeTween != null && fadeTween.IsActive() && fadeTween.IsPlaying()) ||
            (moveTween != null && moveTween.IsActive() && moveTween.IsPlaying());

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void FadeIn(float duration, Action onComplete = null)
        {
            fadeTween?.Kill();

            // SettingPanel이 여는 동안 Time.timeScale을 0으로 만들기 때문에,
            // 스케일드 타임으로 돌면 열리는 연출 도중에 얼어붙는다. 그래서 언스케일드로 돈다.
            fadeTween = canvasGroup.DOFade(1f, duration)
                .SetEase(fadeEase)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void FadeOut(float duration, Action onComplete = null)
        {
            fadeTween?.Kill();

            fadeTween = canvasGroup.DOFade(0f, duration)
                .SetEase(fadeEase)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void MoveTo(Vector2 anchoredPosition, float duration, Action onComplete = null)
        {
            if (rectTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            moveTween?.Kill();

            moveTween = rectTransform.DOAnchorPos(anchoredPosition, duration)
                .SetEase(moveEase)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void StopAll()
        {
            fadeTween?.Kill();
            moveTween?.Kill();
            fadeTween = null;
            moveTween = null;
        }

        private void OnDestroy()
        {
            StopAll();
        }
    }
}
