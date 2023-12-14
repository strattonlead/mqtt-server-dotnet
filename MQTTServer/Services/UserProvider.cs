using Microsoft.Extensions.DependencyInjection;
using MQTTServer.Backend;
using MQTTServer.Backend.Entities;
using System;

namespace MQTTServer.Services
{
    public class UserProvider
    {
        private readonly IMqttUserStore _userStore;
        public UserProvider(IServiceProvider serviceProvider)
        {
            _userStore = serviceProvider.GetRequiredService<IMqttUserStore>();
        }
        private MqttUserEntity _user;
        public MqttUserEntity User
        {
            get
            {
                if (_user != null || !UserId.HasValue)
                {
                    return _user;
                }

                _user = _userStore.FindById(UserId);
                return _user;
            }
        }
        public long? UserId { get; set; }
    }
}
