namespace MQTTServer.Client
{
    public class AckMessage
    {
        public string ClientId { get; set; }
        public string AckTopic { get; set; }
        public string AckId { get; set; }
    }
}
