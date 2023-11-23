using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTServer.Backend;
using MQTTServer.Services;
using PubSubServer.Client;
using PubSubServer.Redis;
using System;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

if ((bool.TryParse(Environment.GetEnvironmentVariable("USE_UI"), out var useUi) && useUi) || Debugger.IsAttached)
{
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
}
bool.TryParse(Environment.GetEnvironmentVariable("USE_POSTGRES"), out var usePostgres);

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(1883, o => o.UseMqtt());
    if (useUi)
    {
        if (builder.Environment.IsProduction())
        {
            options.ListenAnyIP(80);
        }
        if (Debugger.IsAttached)
        {
            options.ListenAnyIP(5001, o => o.UseHttps());
        }
    }
});
builder.Services.AddHostedMqttServer(
    optionsBuilder =>
    {
        optionsBuilder.WithDefaultEndpoint();
        if (bool.TryParse(Environment.GetEnvironmentVariable("USE_CERT"), out var useCert) && useCert)
        {
            optionsBuilder.WithEncryptionCertificate(new CertificateProvider());
            optionsBuilder.WithEncryptedEndpoint();
        }
    });
builder.Services.AddMqttConnectionHandler();
builder.Services.AddConnections();
builder.Services.AddSingleton(_MqttEventHandler.Instance);
builder.Services.AddScoped<MqttEventHandler>();
builder.Services.AddScoped<UserProvider>();
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<IMqttUserStore, UserStore>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"));
    }
    else
    {
        options.UseInMemoryDatabase("mqtt");
    }
});

if (bool.TryParse(Environment.GetEnvironmentVariable("USE_REDIS"), out var useRedis) && useRedis)
{
    builder.Services.AddRedisServices(options =>
    {
        options.UseIsActive(true);

        var channel = Environment.GetEnvironmentVariable("REDIS_DEFAULT_CHANNEL");
        if (!string.IsNullOrWhiteSpace(channel))
        {
            options.UseDefaultChannel("");
        }

        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        options.UseConnectionString(redisConnectionString);
    });
    builder.Services.AddPubSubClient();

    if (bool.TryParse(Environment.GetEnvironmentVariable("USE_REDIS_QUEUE"), out var useRedisQueue) && useRedisQueue)
    {
        builder.Services.AddQueueClient();
    }
}


var app = builder.Build();
_MqttEventHandler.Instance.ServiceProvider = app.Services;

if (usePostgres)
{
    using (var scope = app.Services.CreateScope())
    using (var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
    {
        dbContext.Database.Migrate();
    }
}

app.UseStaticFiles();

app.UseRouting();
if (useUi || Debugger.IsAttached)
{
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");
}

app.UseMqttServer(
    server =>
    {
        server.ValidatingConnectionAsync += _MqttEventHandler.Instance.ValidateConnectionAsync;
        server.ClientConnectedAsync += _MqttEventHandler.Instance.OnClientConnectedAsync;
        server.ClientDisconnectedAsync += _MqttEventHandler.Instance.OnClientDisconnectedAsync;
        server.ClientAcknowledgedPublishPacketAsync += _MqttEventHandler.Instance.OnClientAcknowledgedPublishPacketAsync;
        server.InterceptingPublishAsync += _MqttEventHandler.Instance.OnInterceptingPublishAsync;
        server.LoadingRetainedMessageAsync += _MqttEventHandler.Instance.OnLoadingRetainedMessageAsync;
        server.RetainedMessageChangedAsync += _MqttEventHandler.Instance.OnRetainedMessageChangedAsync;
        server.RetainedMessagesClearedAsync += _MqttEventHandler.Instance.OnRetainedMessagesClearedAsync;
        server.ClientSubscribedTopicAsync += _MqttEventHandler.Instance.OnClientSubscribedTopicAsync;
        server.ClientUnsubscribedTopicAsync += _MqttEventHandler.Instance.OnClientUnsubscribedTopicAsync;
        server.InterceptingSubscriptionAsync += _MqttEventHandler.Instance.OnInterceptingSubscriptionAsync;
    });

app.Run();