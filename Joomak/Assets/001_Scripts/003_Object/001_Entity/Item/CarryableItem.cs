using _001_Scripts._003_Object._001_Entity.Item.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    public abstract class CarryableItem : BaseEntity, IPickable
    {
        private Collider2D[] itemColliders;
        private Rigidbody2D[] itemBodies;

        public bool IsCarried { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            itemColliders = GetComponentsInChildren<Collider2D>(true);
            itemBodies = GetComponentsInChildren<Rigidbody2D>(true);
        }

        public void Interact(GameObject interactor)
        {
            Pick(interactor);
        }

        public void Pick(GameObject picker)
        {
            if (!IsCarried && picker.TryGetComponent(out ISingleItemCarrier carrier))
            {
                carrier.TryCarry(this);
            }
        }

        public void SetCarried(Transform handPoint)
        {
            IsCarried = true;
            transform.SetParent(handPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            SetPhysicsEnabled(false);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.SetParent(null, true);
            transform.position = worldPosition;
            IsCarried = false;

            SetPhysicsEnabled(true);
        }

        private void SetPhysicsEnabled(bool enabled)
        {
            foreach (Collider2D itemCollider in itemColliders)
            {
                if (itemCollider != null)
                {
                    itemCollider.enabled = enabled;
                }
            }

            foreach (Rigidbody2D itemBody in itemBodies)
            {
                if (itemBody == null)
                {
                    continue;
                }

                itemBody.linearVelocity = Vector2.zero;
                itemBody.angularVelocity = 0f;
                itemBody.simulated = enabled;
            }
        }
    }
}
