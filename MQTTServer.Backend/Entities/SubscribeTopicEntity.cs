using Microsoft.EntityFrameworkCore;

namespace MQTTServer.Backend.Entities
{
    [Index(nameof(Topic))]
    public class SubscribeTopicEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserEntity User { get; set; }
        public string Topic { get; set; }
    }
}