using _001_Scripts._003_Object.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Cooker.Interface
{
    public interface ICookerable : IInteractable
    {
        void Cook(GameObject cooker);
    }
}
