using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Server;
using MQTTServer.Client;
using PubSubServer.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTServer.Services
{
    public class RedisAckBackgroundService : BackgroundService
    {
        #region Properties

        private MqttServer _mqttServer { get; set; }
        private readonly bool _useRedisAck;
        private readonly IPubSubService _pubSub;

        #endregion

        #region Constructor

        public RedisAckBackgroundService(IServiceProvider serviceProvider)
        {
            _mqttServer = serviceProvider.GetRequiredService<MqttServer>();
            if (bool.TryParse(Environment.GetEnvironmentVariable("USE_REDIS_ACK"), out var useRedisAck) && useRedisAck)
            {
                _useRedisAck = true;
                _pubSub = serviceProvider.GetRequiredService<IPubSubService>();
                _initAck();
            }
        }

        #endregion

        #region 

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_useRedisAck)
            {
                return;
            }

            try
            {
                await _pubSub.SubscribeAsync<AckMessage>("mqtt/ack", async ackMessage =>
                 {
                     var message = new MqttApplicationMessageBuilder()
                         .WithTopic(ackMessage.AckTopic)
                         .WithPayload(ackMessage.AckId)
                         .Build();

                     await _mqttServer.InjectApplicationMessage(
                         new InjectedMqttApplicationMessage(message)
                         {
                             SenderClientId = ackMessage.ClientId
                         });

                 }, stoppingToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        private void _initAck()
        {
            //var connection = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING"));
            //var subscriber = connection.GetSubscriber();
            //var channel = RedisChannel.Literal("mqtt/ack");
            //subscriber.SubscribeAsync(channel, async (channel, value) =>
            //{
            //    var ackMessage = JsonConvert.DeserializeObject<AckMessage>(value.ToString());

            //    var mqttServer = _serviceProvider.GetRequiredService<MqttServer>();
            //    var message = new MqttApplicationMessageBuilder()
            //       .WithTopic(ackMessage.AckTopic)
            //       .WithPayload(ackMessage.AckId)
            //       .Build();

            //    await mqttServer.InjectApplicationMessage(
            //        new InjectedMqttApplicationMessage(message)
            //        {
            //            SenderClientId = ackMessage.ClientId
            //        });

            //}).Wait();

            //_pubSub.SubscribeAsync<AckMessage>("mqtt/ack", async ackMessage =>
            //{
            //    var mqttServer = _serviceProvider.GetRequiredService<MqttServer>();

            //    var message = new MqttApplicationMessageBuilder()
            //        .WithTopic(ackMessage.AckTopic)
            //        .WithPayload(ackMessage.AckId)
            //        .Build();

            //    await mqttServer.InjectApplicationMessage(
            //        new InjectedMqttApplicationMessage(message)
            //        {
            //            SenderClientId = ackMessage.ClientId
            //        });

            //}, default).Wait(TimeSpan.FromSeconds(10));
        }
    }
}
