using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MQTTServer.Backend.Entities;
using System;

namespace MQTTServer.Backend
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }

        public DbSet<UserEntity> MqttUsers { get; set; }
        public DbSet<PublishTopicEntity> PublishTopics { get; set; }
        public DbSet<SubscribeTopicEntity> SubscribeTopics { get; set; }
    }
}
