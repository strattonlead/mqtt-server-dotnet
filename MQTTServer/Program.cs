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
builder.Services.AddSingleton<_MqttEventHandler>();
builder.Services.AddScoped<MqttEventHandler>();
builder.Services.AddScoped<UserProvider>();
builder.Services.AddScoped<TenantProvider>();
builder.Services.AddScoped<IMqttUserStore, MqttUserStore>();
builder.Services.AddDbContext<MqttDbContext>(options =>
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
builder.Services.AddHostedService<RedisAckBackgroundService>();

var app = builder.Build();

if (usePostgres)
{
    using (var scope = app.Services.CreateScope())
    using (var dbContext = scope.ServiceProvider.GetRequiredService<MqttDbContext>())
    {
        try
        {
            dbContext.Database.Migrate();
        }
        catch { }
    }
}

app.UseStaticFiles();
var eventHandler = app.Services.GetRequiredService<_MqttEventHandler>();

app.UseRouting();
if (useUi || Debugger.IsAttached)
{
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");
}

app.UseMqttServer(
    server =>
    {
        server.ValidatingConnectionAsync += eventHandler.ValidateConnectionAsync;
        server.ClientConnectedAsync += eventHandler.OnClientConnectedAsync;
        server.ClientDisconnectedAsync += eventHandler.OnClientDisconnectedAsync;
        server.ClientAcknowledgedPublishPacketAsync += eventHandler.OnClientAcknowledgedPublishPacketAsync;
        server.InterceptingPublishAsync += eventHandler.OnInterceptingPublishAsync;
        server.LoadingRetainedMessageAsync += eventHandler.OnLoadingRetainedMessageAsync;
        server.RetainedMessageChangedAsync += eventHandler.OnRetainedMessageChangedAsync;
        server.RetainedMessagesClearedAsync += eventHandler.OnRetainedMessagesClearedAsync;
        server.ClientSubscribedTopicAsync += eventHandler.OnClientSubscribedTopicAsync;
        server.ClientUnsubscribedTopicAsync += eventHandler.OnClientUnsubscribedTopicAsync;
        server.InterceptingSubscriptionAsync += eventHandler.OnInterceptingSubscriptionAsync;
    });

app.Run();