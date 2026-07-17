using _001_Scripts._003_Object._001_Entity.Item.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public abstract class CarryableItem : BaseEntity, IPickable
    {
        [SerializeField] private Vector3 carriedScale = Vector3.one;

        private Collider2D[] itemColliders;
        private Rigidbody2D[] itemBodies;
        private Vector3 groundScale;

        public bool IsCarried { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            itemColliders = GetComponentsInChildren<Collider2D>(true);
            itemBodies = GetComponentsInChildren<Rigidbody2D>(true);
            groundScale = transform.localScale;
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
            transform.localScale = carriedScale;

            SetPhysicsEnabled(false);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.SetParent(null, true);
            transform.position = worldPosition;
            transform.localScale = groundScale;
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
