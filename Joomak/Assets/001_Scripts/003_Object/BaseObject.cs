using System;
using UnityEngine;

namespace _001_Scripts._003_Object
{
    public abstract class BaseObject : MonoBehaviour
    {
        [SerializeField] private string objectId;
        [SerializeField] private string displayName;

        public string ObjectId => objectId;
        public string DisplayName => displayName;
        public Vector2 Position => transform.position;

        protected virtual void Reset()
        {
            EnsureObjectId();
            displayName = gameObject.name;
        }

        protected virtual void OnValidate()
        {
            EnsureObjectId();
        }

        private void EnsureObjectId()
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                objectId = Guid.NewGuid().ToString("N");
            }
        }
    }
}
