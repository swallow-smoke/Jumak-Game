using _001.Scripts._003_Object._000_Structure.Interface;
using UnityEngine;

namespace _001_Scripts._003_Object._000_Structure.Cooker.Interface
{
    public interface ICookerable : IInteractable
    {
        public void Cook(GameObject cooker);
    }
}