using System.Collections.Generic;
using _001_Scripts._003_Object;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    public class ObjectManager : SinManagerBase<ObjectManager>
    {
        private List<BaseObject> objects = new List<BaseObject>();

        public override void Initialize()
        {
            Debug.Log("ObjectManager Initialized");
        }
    }
}