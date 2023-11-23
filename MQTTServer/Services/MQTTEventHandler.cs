using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTServer.Backend;
using PubSubServer.Client;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTServer.Services
{
    public class MqttEventHandler
    {
        private readonly IPubSubClient _pubSub;
        private readonly IQueueClient _queue;
        private readonly IMqttUserStore _userStore;
        private readonly bool _publishRedisPubSub;
        private readonly bool _publishRedisQueue;

        public MqttEventHandler(IServiceProvider serviceProvider)
        {
            _pubSub = serviceProvider.GetService<IPubSubClient>();
            _userStore = serviceProvider.GetRequiredService<IMqttUserStore>();
            //_userProvider = serviceProvider.GetRequiredService<UserProvider>();
            bool.TryParse(Environment.GetEnvironmentVariable("USE_REDIS_QUEUE"), out var useRedisQueue);
            bool.TryParse(Environment.GetEnvironmentVariable("PUBLISH_REDIS_PUBSUB"), out var publishRedisPubSub);
            bool.TryParse(Environment.GetEnvironmentVariable("PUBLISH_REDIS_QUEUE"), out var publishRedisQueue);
            if (useRedisQueue)
            {
                _queue = serviceProvider.GetRequiredService<IQueueClient>();
            }

            _publishRedisPubSub = publishRedisPubSub;
            _publishRedisQueue = publishRedisQueue;
        }

        #region MqttEventHandler

        public async Task OnClientConnectedAsync(ClientConnectedEventArgs eventArgs)
        {
            if (_pubSub != null)
            {
                await _pubSub.PublishAsync("mqtt/connected", new
                {
                    eventArgs.ClientId,
                    eventArgs.Endpoint,
                    eventArgs.AuthenticationMethod,
                    ProtocolVersion = eventArgs.ProtocolVersion.ToString(),
                    eventArgs.UserName,
                    eventArgs.SessionItems
                });
            }
        }

        public async Task OnClientDisonnectedAsync(ClientDisconnectedEventArgs eventArgs)
        {
            if (_pubSub != null)
            {
                await _pubSub.PublishAsync("mqtt/disconnected", new
                {
                    eventArgs.ClientId,
                    eventArgs.Endpoint,
                    eventArgs.ReasonString,
                    eventArgs.DisconnectType,
                    eventArgs.ReasonCode,
                    eventArgs.SessionItems
                });
            }
        }

        public async Task OnClientAcknowledgedPublishPacketAsync(ClientAcknowledgedPublishPacketEventArgs eventArgs)
        {
            //var qos1AcknowledgePacket = eventArgs.AcknowledgePacket as MqttPubAckPacket;

            //var qos2AcknowledgePacket = eventArgs.AcknowledgePacket as MqttPubCompPacket;
        }

        public async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs eventArgs)
        {
            var func = async (InterceptingPublishEventArgs e) =>
            {
                if (_pubSub != null && e.ApplicationMessage.PayloadSegment.Count <= 8388608 && _publishRedisPubSub)
                {
                    await _pubSub?.PublishAsync("mqtt/publish", new
                    {
                        e.ClientId,
                        e.ApplicationMessage.Topic,
                        Payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()),
                        e.ApplicationMessage.QualityOfServiceLevel,
                        e.SessionItems
                    });
                }

                if (_queue != null && _publishRedisQueue)
                {
                    await _queue.PushAsync("mqtt/publish", new
                    {
                        e.ClientId,
                        e.ApplicationMessage.Topic,
                        Payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray()),
                        e.ApplicationMessage.QualityOfServiceLevel,
                        e.SessionItems
                    });
                }
            };

            var user = SessionUser.FromSession(eventArgs.SessionItems);
            if (user.PublishTopics.Contains(eventArgs.ApplicationMessage.Topic))
            {
                eventArgs.ProcessPublish = true;
                await func(eventArgs);
            }

            if (user.PublishTopics.Any(x => TopicChecker.Regex(x, eventArgs.ApplicationMessage.Topic)))
            {
                eventArgs.ProcessPublish = true;
                await func(eventArgs);
            }

            eventArgs.ProcessPublish = false;
        }

        public async Task OnLoadingRetainedMessageAsync(LoadingRetainedMessagesEventArgs eventArgs)
        {
        }

        public async Task OnRetainedMessageChangedAsync(RetainedMessageChangedEventArgs eventArgs)
        {
        }

        public async Task OnRetainedMessagesClearedAsync(EventArgs eventArgs)
        {
        }

        public async Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs eventArgs)
        {
            if (_pubSub != null)
            {
                await _pubSub?.PublishAsync("mqtt/subscribed", new
                {
                    eventArgs.ClientId,
                    eventArgs.TopicFilter.Topic,
                    eventArgs.TopicFilter.QualityOfServiceLevel,
                    eventArgs.TopicFilter.RetainHandling,
                    eventArgs.SessionItems
                });
            }
        }

        public async Task OnClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs eventArgs)
        {
            if (_pubSub != null)
            {
                await _pubSub?.PublishAsync("mqtt/unsubscribed", new
                {
                    eventArgs.ClientId,
                    eventArgs.TopicFilter,
                    eventArgs.SessionItems
                });

            }
        }

        public Task OnInterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs eventArgs)
        {
            var user = SessionUser.FromSession(eventArgs.SessionItems);
            if (user.SubscribeTopics.Select(x => x).Contains(eventArgs.TopicFilter.Topic))
            {
                eventArgs.ProcessSubscription = true;
                return Task.CompletedTask;
            }

            if (user.SubscribeTopics.Any(x => TopicChecker.Regex(x, eventArgs.TopicFilter.Topic)))
            {
                eventArgs.ProcessSubscription = true;
                return Task.CompletedTask;
            }

            eventArgs.ProcessSubscription = false;
            return Task.CompletedTask;
        }

        public async Task ValidateConnectionAsync(ValidatingConnectionEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(eventArgs.Password))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            var user = await _userStore.FindByUsernameAsync(eventArgs.UserName);
            if (user == null)
            {
                if (_pubSub != null)
                {
                    await _pubSub?.PublishAsync("mqtt/invaliduser", new
                    {
                        eventArgs.ClientId,
                        eventArgs.UserName,
                        eventArgs.SessionItems
                    });

                }
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            var authenticted = _userStore.CanAuthenticate(user, eventArgs.Password);
            if (!authenticted)
            {
                await _pubSub?.PublishAsync("mqtt/invalidpassword", new
                {
                    eventArgs.ClientId,
                    eventArgs.UserName,
                    eventArgs.SessionItems
                });
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            eventArgs.SessionItems.Add("user_id", user.Id);
            eventArgs.SessionItems.Add("username", user.UserName);
            if (user.TenantId.HasValue)
            {
                eventArgs.SessionItems.Add("tenant_id", user.TenantId.Value);
            }
            eventArgs.SessionItems.Add("subscribe_topics", user.SubscribeTopics.Select(x => x.Topic).ToArray());
            eventArgs.SessionItems.Add("publish_topics", user.PublishTopics.Select(x => x.Topic).ToArray());

            Console.WriteLine($"Client '{eventArgs.ClientId}' wants to connect. Accepting!");
        }

        #endregion
    }

    public class SessionUser
    {
        public long? UserId { get; set; }
        public long? TenantId { get; set; }
        public string Username { get; set; }
        public string[] PublishTopics { get; set; }
        public string[] SubscribeTopics { get; set; }

        public static SessionUser FromSession(IDictionary sessionItems)
        {
            long? userId = null;
            long? tenantId = null;
            if (long.TryParse(sessionItems["user_id"]?.ToString(), out var _userId))
            {
                userId = _userId;
            }

            if (long.TryParse(sessionItems["tenant_id"]?.ToString(), out var _tenantId))
            {
                tenantId = _tenantId;
            }

            return new SessionUser()
            {
                UserId = userId,
                TenantId = tenantId,
                Username = sessionItems["username"]?.ToString(),
                SubscribeTopics = sessionItems["subscribe_topics"] as string[] ?? new string[0],
                PublishTopics = sessionItems["publish_topics"] as string[] ?? new string[0]
            };
        }
    }

    internal class _MqttEventHandler
    {
        private static _MqttEventHandler instance = new _MqttEventHandler();
        public static _MqttEventHandler Instance { get { return instance; } }
        private _MqttEventHandler() { }
        public IServiceProvider ServiceProvider { get; set; }


        #region Events

        public async Task OnClientConnectedAsync(ClientConnectedEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnClientConnectedAsync(eventArgs)
            , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnClientDisconnectedAsync(ClientDisconnectedEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnClientDisonnectedAsync(eventArgs)
            , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnClientAcknowledgedPublishPacketAsync(ClientAcknowledgedPublishPacketEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnClientAcknowledgedPublishPacketAsync(eventArgs)
            , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnInterceptingPublishAsync(InterceptingPublishEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnInterceptingPublishAsync(eventArgs)
            , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnLoadingRetainedMessageAsync(LoadingRetainedMessagesEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnLoadingRetainedMessageAsync(eventArgs)
            );
        }

        public async Task OnRetainedMessageChangedAsync(RetainedMessageChangedEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                await handler.OnRetainedMessageChangedAsync(eventArgs)
            );
        }

        public async Task OnRetainedMessagesClearedAsync(EventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                 await handler.OnRetainedMessagesClearedAsync(eventArgs)
             );
        }

        public async Task OnInterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                 await handler.OnInterceptingSubscriptionAsync(eventArgs)
             , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                 await handler.OnClientSubscribedTopicAsync(eventArgs)
             , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task OnClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs eventArgs)
        {
            await _handleAsync(async handler =>
                 await handler.OnClientUnsubscribedTopicAsync(eventArgs)
             , _getUserId(eventArgs.SessionItems), _getTenantId(eventArgs.SessionItems));
        }

        public async Task ValidateConnectionAsync(ValidatingConnectionEventArgs eventArgs)
        {

            await _handleAsync(async handler =>
                await handler.ValidateConnectionAsync(eventArgs)
            );
        }

        #endregion

        #region Helper

        private long? _getUserId(IDictionary obj)
        {
            if (obj["user_id"] is long)
            {
                return (long)obj["user_id"];
            }
            return null;
        }

        private long? _getTenantId(IDictionary obj)
        {
            if (obj["tenant_id"] is long)
            {
                return (long)obj["tenant_id"];
            }
            return null;
        }

        private async Task _handleAsync(Func<MqttEventHandler, Task> action, long? userId = null, long? tenantId = null)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                if (userId.HasValue)
                {
                    var userProvider = scope.ServiceProvider.GetRequiredService<UserProvider>();
                    userProvider.UserId = userId;
                }

                if (userId.HasValue)
                {
                    var tenantProvider = scope.ServiceProvider.GetRequiredService<TenantProvider>();
                    tenantProvider.TenantId = tenantId;
                }

                var handler = scope.ServiceProvider.GetRequiredService<MqttEventHandler>();
                await action(handler);
            }
        }

        #endregion
    }
}
