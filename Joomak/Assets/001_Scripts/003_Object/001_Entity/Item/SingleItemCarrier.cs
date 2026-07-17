using _001_Scripts._003_Object._001_Entity.Item.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    [DisallowMultipleComponent]
    public class SingleItemCarrier : MonoBehaviour, ISingleItemCarrier
    {
        [SerializeField] private Transform handPoint;
        [SerializeField] private Vector3 defaultHandPosition = new(0f, 0.6f, 0f);

        public CarryableItem HeldItem { get; private set; }

        protected virtual void Awake()
        {
            if (handPoint == null)
            {
                GameObject handObject = new("HandPoint");
                handPoint = handObject.transform;
                handPoint.SetParent(transform, false);
                handPoint.localPosition = defaultHandPosition;
            }
        }

        public bool TryCarry(CarryableItem item)
        {
            if (item == null || HeldItem != null || item.IsCarried)
            {
                return false;
            }

            HeldItem = item;
            item.SetCarried(handPoint);
            return true;
        }

        public bool TryConsumeHeldItem(CarryableItem expectedItem)
        {
            if (HeldItem == null || HeldItem != expectedItem)
            {
                return false;
            }

            CarryableItem consumedItem = HeldItem;
            HeldItem = null;
            Destroy(consumedItem.gameObject);
            return true;
        }

        public bool TryDropHeldItem(Vector3 worldPosition)
        {
            if (HeldItem == null)
            {
                return false;
            }

            CarryableItem droppedItem = HeldItem;
            HeldItem = null;
            droppedItem.SetWorldPosition(worldPosition);
            return true;
        }
    }
}
