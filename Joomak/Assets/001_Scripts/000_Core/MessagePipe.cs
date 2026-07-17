using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public static class MessagePipe
    {
        public static List<BaseMessage> messages = new List<BaseMessage>();

        public static void Request(BaseMessage msg, GameObject sender)
        {
            messages.Add(msg);
            Debug.Log($"Sender: {sender.name} | id: {msg.id} | MessageName: {msg.MessageName}");
        }
    }
}