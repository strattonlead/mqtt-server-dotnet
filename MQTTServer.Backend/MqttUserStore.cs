using Microsoft.EntityFrameworkCore;
using MQTTServer.Backend.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTServer.Backend
{
    public class MqttUserStore : IMqttUserStore
    {
        private readonly MqttDbContext _dbContext;
        public MqttUserStore(MqttDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region IMqttUserStore

        public bool CanAuthenticate(MqttUserEntity mqttUser, string password)
        {
            return mqttUser.Password == _hash(password);
        }

        public async Task<bool> CanAuthenticateAsync(string username, string password)
        {
            var hash = _hash(password);
            return await _dbContext.MqttUsers.AnyAsync(x => x.UserName == username && x.Password == hash);
        }

        public async Task<MqttUserEntity> FindByUsernameAsync(string username)
        {
            var user = await _dbContext.MqttUsers.FirstOrDefaultAsync(x => x.UserName == username);
            return _loadTopics(user);
        }

        public MqttUserEntity FindById(long? userId)
        {
            var user = _dbContext.MqttUsers.Find(userId);
            return _loadTopics(user);
        }

        public async Task<MqttUserEntity> CreateAsync(string username, string password, IList<string> publishTopics, IList<string> subscribeTopics, long? tenantId = null, IDictionary<string, string> customProperties = null, CancellationToken cancellationToken = default)
        {
            var user = new MqttUserEntity()
            {
                UserName = username,
                PublishTopics = publishTopics?.Select(x => new PublishTopicEntity() { Topic = x }).ToList(),
                SubscribeTopics = subscribeTopics?.Select(x => new SubscribeTopicEntity() { Topic = x }).ToList(),
                Password = _hash(password),
                TenantId = tenantId,
                CustomProperties = customProperties.ToDictionary(x => x.Key, x => x.Value)
            };

            _dbContext.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var user = FindById(id);
            if (user != null)
            {
                _dbContext.Remove(user);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        #endregion

        #region Helpers

        private MqttUserEntity _loadTopics(MqttUserEntity user)
        {
            if (user != null)
            {
                if (user.PublishTopics == null)
                {
                    _dbContext.Entry(user).Collection(x => x.PublishTopics).Load();
                }

                if (user.SubscribeTopics == null)
                {
                    _dbContext.Entry(user).Collection(x => x.SubscribeTopics).Load();
                }
            }
            return user;
        }

        private string _hash(string s)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(s);
            var hasher = SHA1.Create();
            var hashBytes = hasher.ComputeHash(plainTextBytes);
            var hashValue = BitConverter.ToString(hashBytes);
            return hashValue.Replace("-", "").ToLower();
        }

        #endregion
    }

    public interface IMqttUserStore
    {
        MqttUserEntity FindById(long? userId);
        Task<MqttUserEntity> FindByUsernameAsync(string username);
        bool CanAuthenticate(MqttUserEntity mqttUser, string password);
        Task<bool> CanAuthenticateAsync(string username, string password);
        Task<MqttUserEntity> CreateAsync(string username, string password, IList<string> publishTopics, IList<string> subscribeTopics, long? tenantId = null, IDictionary<string, string> customProperties = null, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
