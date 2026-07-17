using _001_Scripts._001_Manager;
using _001_Scripts._003_Object.Interface;
using UnityEngine;
using _001_Scripts._004_UI.Components;

namespace _001_Scripts._003_Object._001_Entity
{
    // 기획서 8번 청소 이벤트: 바닥에 생긴 쓰레기. 빗자루로 연타해야 치워진다.
    public sealed class Trash : BaseEntity, IBroomTarget
    {
        private const int TimeoutPenalty = 5;

        private int remainingHits;
        private float remainingSeconds;
        private bool isResolved;

        // 쓰레기는 언제나 빗자루가 있어야 치울 수 있다.
        public bool RequiresBroom => true;
        public int RemainingHits => remainingHits;

        public void Initialize(int hits, float timeLimitSeconds)
        {
            remainingHits = Mathf.Max(1, hits);
            remainingSeconds = timeLimitSeconds;
        }

        private void Update()
        {
            if (isResolved || remainingHits <= 0)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds > 0f)
            {
                return;
            }

            Penalize();

            // 페널티를 한 번 물린 뒤에는 치워준다. 남겨두면 같은 쓰레기로 계속 깎이게 된다.
            Resolve();
        }

        public void Interact(GameObject interactor)
        {
            if (isResolved)
            {
                return;
            }

            remainingHits--;
            GameplayFeedback.Burst(transform.position, new Color(0.65f, 0.53f, 0.36f),
                remainingHits > 0 ? $"남은 횟수 {remainingHits}" : null, 6);
            if (remainingHits <= 0)
            {
                Resolve();
            }
        }

        private void Penalize()
        {
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.Penalize(TimeoutPenalty, "쓰레기를 치우지 않음");
            }
        }

        private void Resolve()
        {
            isResolved = true;
            GameplayFeedback.Burst(transform.position, new Color(0.45f, 0.9f, 0.55f), "청소 완료!", 14);
            Destroy(gameObject);
        }
    }
}
