using UnityEngine;

namespace _001_Scripts._001_Manager
{
    public abstract class SinManagerBase<T> : MonoBehaviour
    {
        public T Instance;

        public abstract void Initialize();

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = (T)(object)this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}