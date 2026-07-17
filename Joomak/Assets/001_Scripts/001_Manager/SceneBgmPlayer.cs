using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 씬 안의 아무 GameObject에 붙이고 bgmClip만 지정하면 전역 AudioManager로 BGM을 재생한다.
    public sealed class SceneBgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool stopOnDisable;

        private void Start()
        {
            AudioManager.Instance?.PlayBgm(bgmClip, loop);
        }

        private void OnDisable()
        {
            if (stopOnDisable)
            {
                AudioManager.Instance?.StopBgm();
            }
        }
    }
}
