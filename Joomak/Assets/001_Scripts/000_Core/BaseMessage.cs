using _001_Scripts._000_Core.MessageData;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public readonly struct BaseMessage
    {
        public readonly string MessageName;
        public readonly GUID id;
        public readonly BaseMsgData data;
        
        public BaseMessage(string messageName, GUID id, BaseMsgData data)
        {
            MessageName = messageName;
            this.id = id;
            this.data = data;
        }
    }
}