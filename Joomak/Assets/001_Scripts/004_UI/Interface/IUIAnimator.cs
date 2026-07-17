using System;
using UnityEngine;

namespace _001_Scripts._004_UI.Interface
{
    // 패널의 등장/퇴장 연출. 구현체(UIComponent)는 DOTween으로 만든다.
    // PanelBase는 이 인터페이스로만 붙으므로, 구현체가 없으면 즉시 전환으로 동작한다.
    public interface IUIAnimator
    {
        bool IsAnimating { get; }

        void FadeIn(float duration, Action onComplete = null);
        void FadeOut(float duration, Action onComplete = null);
        void MoveTo(Vector2 anchoredPosition, float duration, Action onComplete = null);

        // 패널이 파괴되거나 연출이 겹칠 때 이전 트윈을 끊는다.
        void StopAll();
    }
}
