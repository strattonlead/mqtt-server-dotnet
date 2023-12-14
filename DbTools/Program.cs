using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTServer.Backend;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddDbContext<MqttDbContext>(options =>
    {
        options.UseNpgsql("pg");

    });
});
builder.Build().Run();
