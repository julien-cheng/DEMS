using Documents.API.Common.Models;

namespace Documents.API.Common.Websocket.Messages
{
    public class QueueControlMessage
    {
        public enum QueueControlMessageTypes
        {
            Connected,
            Incoming,
            Ping,
            Ack,
            Nak
        }
        public QueueControlMessageTypes Type { get; set; }

        public string Queue { get; set; }
        public QueueMessage Message { get; set; }
    }
}
