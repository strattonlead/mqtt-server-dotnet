using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTServer.Backend;
using MQTTServer.Services;
using PubSubServer.Redis;
using System;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

if ((bool.TryParse(Environment.GetEnvironmentVariable("USE_UI"), out var useUi) && useUi) || Debugger.IsAttached)
{
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
}

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(1883, o => o.UseMqtt());
    if (useUi)
    {
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
        //optionsBuilder.WithEncryptionCertificate(new CertificateProvider());
        //optionsBuilder.WithEncryptedEndpoint();
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
    if (bool.TryParse(Environment.GetEnvironmentVariable("USE_MSSQL"), out var useMsSql) && useMsSql)
    {
        options.UseSqlServer(Environment.GetEnvironmentVariable("MSSQL_CONNECTION_STRING"));
    }
    else
    {
        options.UseInMemoryDatabase("mqtt");
    }
});

if (bool.TryParse(Environment.GetEnvironmentVariable("USE_REDIS"), out var useRedis) && useRedis)
{
    builder.Services.AddRedisPubSubService(options =>
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
}


var app = builder.Build();
_MqttEventHandler.Instance.ServiceProvider = app.Services;

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