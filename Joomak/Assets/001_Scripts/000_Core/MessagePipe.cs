using System;
using System.Collections.Generic;
using _001_Scripts._000_Core.MessageData;
using UnityEngine;

namespace _001_Scripts._000_Core
{
    public static class MessagePipe
    {
        private static readonly Dictionary<Type, List<Subscription>> Subscriptions = new();

        public static bool EnableLogging { get; set; }

        public static int SubscriptionCount
        {
            get
            {
                int count = 0;
                foreach (List<Subscription> subscriptions in Subscriptions.Values)
                {
                    count += subscriptions.Count;
                }

                return count;
            }
        }

        public static IDisposable Subscribe<T>(
            Action<BaseMessage> handler,
            MessageEndpoint receiver = MessageEndpoint.Any)
            where T : BaseMsgData
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return AddSubscription(typeof(T), handler, handler, receiver);
        }

        public static IDisposable SubscribeData<T>(
            Action<T> handler,
            MessageEndpoint receiver = MessageEndpoint.Any)
            where T : BaseMsgData
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Action<BaseMessage> wrapper = message => handler(message.GetData<T>());
            return AddSubscription(typeof(T), wrapper, handler, receiver);
        }

        public static void Unsubscribe<T>(Action<BaseMessage> handler)
            where T : BaseMsgData
        {
            RemoveMatchingSubscriptions(typeof(T), handler);
        }

        public static void UnsubscribeData<T>(Action<T> handler)
            where T : BaseMsgData
        {
            RemoveMatchingSubscriptions(typeof(T), handler);
        }

        public static BaseMessage Publish<T>(
            T data,
            MessageEndpoint source,
            MessageEndpoint destination,
            GameObject sender = null,
            Guid correlationId = default)
            where T : BaseMsgData
        {
            BaseMessage message = new(
                data,
                source,
                destination,
                sender != null ? sender.name : null,
                correlationId);

            Publish(message);
            return message;
        }

        public static BaseMessage Reply<T>(
            in BaseMessage request,
            T data,
            MessageEndpoint source,
            MessageEndpoint destination,
            GameObject sender = null)
            where T : BaseMsgData
        {
            return Publish(data, source, destination, sender, request.Id);
        }

        public static void Request(BaseMessage message, GameObject sender = null)
        {
            Publish(message);
        }

        public static void Publish(in BaseMessage message)
        {
            if (message.Data == null)
            {
                Debug.LogError("[MessagePipe] Cannot publish a message with null data.");
                return;
            }

            if (EnableLogging)
            {
                Debug.Log(
                    $"[MessagePipe] {message.Source} -> {message.Destination} | " +
                    $"{message.Data.GetType().Name} | {message.Id}");
            }

            Type messageType = message.Data.GetType();
            if (!Subscriptions.TryGetValue(messageType, out List<Subscription> subscriptions))
            {
                return;
            }

            Subscription[] snapshot = subscriptions.ToArray();
            foreach (Subscription subscription in snapshot)
            {
                if (subscription.IsDisposed || !message.IsFor(subscription.Receiver))
                {
                    continue;
                }

                if (subscription.Handler.Target is UnityEngine.Object unityTarget && unityTarget == null)
                {
                    subscription.Dispose();
                    continue;
                }

                try
                {
                    subscription.Handler(message);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static void Clear()
        {
            foreach (List<Subscription> subscriptions in Subscriptions.Values)
            {
                foreach (Subscription subscription in subscriptions)
                {
                    subscription.MarkDisposed();
                }
            }

            Subscriptions.Clear();
        }

        private static IDisposable AddSubscription(
            Type messageType,
            Action<BaseMessage> handler,
            Delegate originalHandler,
            MessageEndpoint receiver)
        {
            if (!Subscriptions.TryGetValue(messageType, out List<Subscription> subscriptions))
            {
                subscriptions = new List<Subscription>();
                Subscriptions.Add(messageType, subscriptions);
            }

            Subscription subscription = new(messageType, handler, originalHandler, receiver);
            subscriptions.Add(subscription);
            return subscription;
        }

        private static void RemoveMatchingSubscriptions(Type messageType, Delegate handler)
        {
            if (handler == null || !Subscriptions.TryGetValue(messageType, out List<Subscription> subscriptions))
            {
                return;
            }

            for (int i = subscriptions.Count - 1; i >= 0; i--)
            {
                if (subscriptions[i].OriginalHandler == handler)
                {
                    subscriptions[i].Dispose();
                }
            }
        }

        private static void Remove(Subscription subscription)
        {
            if (!Subscriptions.TryGetValue(subscription.MessageType, out List<Subscription> subscriptions))
            {
                return;
            }

            subscriptions.Remove(subscription);
            if (subscriptions.Count == 0)
            {
                Subscriptions.Remove(subscription.MessageType);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            Clear();
            EnableLogging = false;
        }

        private sealed class Subscription : IDisposable
        {
            public readonly Type MessageType;
            public readonly Action<BaseMessage> Handler;
            public readonly Delegate OriginalHandler;
            public readonly MessageEndpoint Receiver;

            public bool IsDisposed { get; private set; }

            public Subscription(
                Type messageType,
                Action<BaseMessage> handler,
                Delegate originalHandler,
                MessageEndpoint receiver)
            {
                MessageType = messageType;
                Handler = handler;
                OriginalHandler = originalHandler;
                Receiver = receiver;
            }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                Remove(this);
            }

            public void MarkDisposed()
            {
                IsDisposed = true;
            }
        }
    }
}
