using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    // 기획서 3번의 홀 입구. 손님이 여기서 생성되어 대기 지점에 줄을 서고, 퇴장도 여기로 한다.
    public sealed class CustomerEntrance : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;
        [SerializeField] private List<Transform> waitingSpots = new();

        public int WaitingCapacity => waitingSpots.Count;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public Vector3 ExitPosition => exitPoint != null ? exitPoint.position : SpawnPosition;

        public bool TryGetWaitingSpot(int index, out Vector3 position)
        {
            if (index < 0 || index >= waitingSpots.Count || waitingSpots[index] == null)
            {
                position = SpawnPosition;
                return false;
            }

            position = waitingSpots[index].position;
            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SpawnPosition, 0.66f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ExitPosition, 0.66f);

            Gizmos.color = Color.yellow;
            foreach (Transform spot in waitingSpots)
            {
                if (spot != null)
                {
                    Gizmos.DrawWireCube(spot.position, Vector3.one * 0.9f);
                }
            }
        }
    }
}
