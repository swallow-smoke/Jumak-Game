using System;
using _001_Scripts._000_Core.MessageData;

namespace _001_Scripts._000_Core
{
    public readonly struct BaseMessage
    {
        public Guid Id { get; }
        public Guid CorrelationId { get; }
        public BaseMsgData Data { get; }
        public MessageEndpoint Source { get; }
        public MessageEndpoint Destination { get; }
        public string SenderName { get; }
        public long CreatedAtUtcTicks { get; }

        public DateTime CreatedAtUtc => new(CreatedAtUtcTicks, DateTimeKind.Utc);

        public BaseMessage(
            BaseMsgData data,
            MessageEndpoint source = MessageEndpoint.System,
            MessageEndpoint destination = MessageEndpoint.Broadcast,
            string senderName = null,
            Guid correlationId = default)
        {
            Id = Guid.NewGuid();
            CorrelationId = correlationId;
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Source = source;
            Destination = destination;
            SenderName = string.IsNullOrWhiteSpace(senderName) ? source.ToString() : senderName;
            CreatedAtUtcTicks = DateTime.UtcNow.Ticks;
        }

        public T GetData<T>() where T : BaseMsgData => Data as T;

        public bool IsFor(MessageEndpoint endpoint)
        {
            return endpoint == MessageEndpoint.Any ||
                   Destination == MessageEndpoint.Broadcast ||
                   Destination == endpoint;
        }
    }
}
