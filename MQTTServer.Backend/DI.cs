using Microsoft.Extensions.DependencyInjection;
using System;

namespace MQTTServer.Backend
{
    public static class DI
    {
        public static void AddMqttUserStore(this IServiceCollection services)
        {
            services.AddScoped<IMqttUserStore, MqttUserStore>();
        }

        public static void AddMqttUserStore(this IServiceCollection services, Action<MqttUserStoreOptionsBuilder> builder)
        {
            var optionsBuilder = new MqttUserStoreOptionsBuilder();
            builder?.Invoke(optionsBuilder);
            services.AddSingleton(optionsBuilder.Options);
            services.AddScoped<IMqttUserStore, MqttUserStore>();
        }
    }
}
