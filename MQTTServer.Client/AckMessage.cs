namespace MQTTServer.Client
{
    public class AckMessage
    {
        public string ClientId { get; set; }
        public string Topic { get; set; }
        public string PackageId { get; set; }
    }
}
