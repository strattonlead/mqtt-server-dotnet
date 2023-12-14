using Microsoft.Extensions.DependencyInjection;

namespace MQTTServer.Backend
{
    public static class DI
    {
        public static void AddMqttUserStore(this IServiceCollection services)
        {
            services.AddScoped<IMqttUserStore, MqttUserStore>();
        }
    }
}
