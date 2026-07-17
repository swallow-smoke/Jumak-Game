using _001_Scripts._003_Object.Interface;
using _001_Scripts._003_Object;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Interface
{
    public abstract class BaseStructure : BaseObject, IInteractable
    {
        public abstract void Interact(GameObject interactor);
    }
}
