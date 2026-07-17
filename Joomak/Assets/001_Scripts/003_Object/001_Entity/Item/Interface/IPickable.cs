using _001_Scripts._003_Object.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item.Interface
{
    public interface IPickable : IInteractable
    {
        public void Pick(GameObject picker);
    }
}
