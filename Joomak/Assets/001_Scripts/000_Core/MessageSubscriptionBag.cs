using System;
using System.Collections.Generic;

namespace _001_Scripts._000_Core
{
    public sealed class MessageSubscriptionBag : IDisposable
    {
        private readonly List<IDisposable> subscriptions = new();

        public void Add(IDisposable subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            subscriptions.Add(subscription);
        }

        public void Clear()
        {
            for (int i = subscriptions.Count - 1; i >= 0; i--)
            {
                subscriptions[i].Dispose();
            }

            subscriptions.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
