using System;
using _001_Scripts._000_Core.MessageData;

namespace _001_Scripts._000_Core
{
    public readonly struct BaseMessage
    {
        public readonly Guid Id;
        public readonly BaseMsgData Data;

        public BaseMessage(BaseMsgData data)
        {
            Id = Guid.NewGuid();
            Data = data;
        }

        public T GetData<T>() where T : BaseMsgData => Data as T;
    }
}
