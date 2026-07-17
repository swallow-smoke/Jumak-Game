using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.NPC
{
    // 홀 플레이어가 손님을 한 명씩 테이블로 안내한다. SingleItemCarrier의 손님 버전.
    [DisallowMultipleComponent]
    public sealed class CustomerEscort : MonoBehaviour
    {
        public Customer Escorted { get; private set; }

        public bool TryEscort(Customer customer)
        {
            if (customer == null || Escorted != null)
            {
                return false;
            }

            Escorted = customer;
            return true;
        }

        public void Release(Customer customer)
        {
            if (Escorted == customer)
            {
                Escorted = null;
            }
        }
    }
}
