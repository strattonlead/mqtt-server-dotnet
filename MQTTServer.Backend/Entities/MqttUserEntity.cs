using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MQTTServer.Backend.Entities
{
    public class MqttUserEntity
    {
        [Key]
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public long? TenantId { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; }
        public List<PublishTopicEntity> PublishTopics { get; set; }
        public List<SubscribeTopicEntity> SubscribeTopics { get; set; }
    }

    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<MqttUserEntity>
    {
        public void Configure(EntityTypeBuilder<MqttUserEntity> builder)
        {
            builder.HasMany(x => x.PublishTopics)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.SubscribeTopics)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.CustomProperties)
                .HasColumnType("text")
                .HasConversion(x => JsonConvert.SerializeObject(x), x => _deserialize(x));
        }

        private Dictionary<string, string> _deserialize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
            }
            catch { }
            return null;
        }
    }

}