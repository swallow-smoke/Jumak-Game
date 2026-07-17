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

        // 플레이어가 바라보는 방향으로 손에 든 아이템을 회전시킨다. 기본 바라보는 방향(아래)일 때 회전이 없도록 down을 기준으로 잡는다.
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Vector2.SignedAngle(Vector2.down, direction);
            handPoint.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
