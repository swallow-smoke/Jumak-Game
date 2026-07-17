using DG.Tweening;
using UnityEngine;

namespace _001_Scripts._004_UI.Components
{
    // 활성화될 때(OnEnable) Scale을 0에서 원래 크기로 천천히 키우며 나타난다.
    // 프리팹에 붙여두면 CookingStation 같은 쪽에서는 그냥 SetActive(true)만 하면 된다.
    public sealed class ScaleRevealAnimator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.25f;
        [SerializeField] private Ease ease = Ease.OutBack;

        private Tween scaleTween;
        private Vector3 targetScale;

        private void Awake()
        {
            targetScale = transform.localScale;
        }

        private void OnEnable()
        {
            scaleTween?.Kill();
            transform.localScale = Vector3.zero;
            scaleTween = transform.DOScale(targetScale, duration).SetEase(ease);
        }

        private void OnDisable()
        {
            scaleTween?.Kill();
        }
    }
}
