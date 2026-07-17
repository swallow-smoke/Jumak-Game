using _001_Scripts._003_Object.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._001_Entity.Item.Interface
{
    public interface IPickable : IInteractable
    {
        void Pick(GameObject picker);
    }
}
