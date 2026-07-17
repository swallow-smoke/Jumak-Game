using _001_Scripts._003_Object._001_Entity.NPC;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Hall
{
    [DisallowMultipleComponent]
    public sealed class Seat : MonoBehaviour
    {
        public DiningTable Table { get; private set; }
        public Customer Occupant { get; private set; }
        public bool HasDirtyPlate { get; private set; }

        public bool IsFree => Occupant == null && !HasDirtyPlate;
        public Vector3 SitPosition => transform.position;

        internal void BindTable(DiningTable table) => Table = table;

        public bool TryReserve(Customer customer)
        {
            if (customer == null || !IsFree)
            {
                return false;
            }

            Occupant = customer;
            return true;
        }

        public void Release(Customer customer)
        {
            if (Occupant == customer)
            {
                Occupant = null;
            }
        }

        // 손님이 식사를 마치면 자리에 빈 그릇이 남는다. 치우기 전까지 이 자리는 다시 못 쓴다.
        public void MarkDirty() => HasDirtyPlate = true;

        public void ClearPlate() => HasDirtyPlate = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = IsFree ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.55f);
        }
    }
}
