using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTServer.Backend.Entities;
using System;

namespace MQTTServer.Backend
{
    public class MqttDbContext : DbContext
    {
        public MqttDbContext(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<DbContextOptions<MqttDbContext>>()) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MqttDbContext).Assembly);
        }

        public DbSet<MqttUserEntity> MqttUsers { get; set; }
        public DbSet<PublishTopicEntity> PublishTopics { get; set; }
        public DbSet<SubscribeTopicEntity> SubscribeTopics { get; set; }
    }
}
