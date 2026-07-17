using System;
using UnityEngine;

namespace _001_Scripts._003_Object
{
    public abstract class BaseObject : MonoBehaviour
    {
        private Guid _objectId;

        public Guid ObjectId => _objectId;
        public string ObjectName => name;
        public Vector2 Position => transform.position;

        protected virtual void Awake()
        {
            _objectId = Guid.NewGuid();
        }
    }
}
