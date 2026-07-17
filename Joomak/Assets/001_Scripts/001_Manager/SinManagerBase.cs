using _001_Scripts._001_Manager.Interface;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    public abstract class SinManagerBase<T> : MonoBehaviour, IManager where T : SinManagerBase<T>
    {
        public static T Instance { get; private set; }

        public abstract void Initialize();

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
