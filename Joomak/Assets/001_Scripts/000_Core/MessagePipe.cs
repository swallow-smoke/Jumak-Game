using System;
using System.Collections.Generic;
using _001_Scripts._000_Core.MessageData;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public static class MessagePipe
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<BaseMessage> handler) where T : BaseMsgData
        {
            var key = typeof(T);
            Handlers[key] = Handlers.TryGetValue(key, out var existing)
                ? (Action<BaseMessage>)existing + handler
                : handler;
        }

        public static void Unsubscribe<T>(Action<BaseMessage> handler) where T : BaseMsgData
        {
            var key = typeof(T);
            if (!Handlers.TryGetValue(key, out var existing)) return;

            var remaining = (Action<BaseMessage>)existing - handler;
            if (remaining == null) Handlers.Remove(key);
            else Handlers[key] = remaining;
        }

        public static void Request(BaseMessage msg, GameObject sender)
        {
            if (msg.Data == null)
            {
                Debug.LogError($"[MessagePipe] Data is null. Sender: {sender.name}");
                return;
            }

            Debug.Log($"[MessagePipe] {sender.name} -> {msg.Data.GetType().Name} | id: {msg.Id}");

            if (Handlers.TryGetValue(msg.Data.GetType(), out var handler))
                ((Action<BaseMessage>)handler).Invoke(msg);
        }

        // Enter Play Mode Options에서 도메인 리로드를 끄면 static 필드가 플레이 세션 사이에 남는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear() => Handlers.Clear();
    }
}
