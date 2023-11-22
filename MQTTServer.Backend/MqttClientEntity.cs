using System.ComponentModel.DataAnnotations;

namespace MQTTServer.Backend
{
    public class MqttClientEntity
    {
        [Key]
        public string ClientId { get; set; }
    }
}
