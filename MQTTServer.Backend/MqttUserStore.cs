using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IDbContextFactory<MqttDbContext> _dbContextFactory;
        private readonly MqttDbContext _dbContext;
        private readonly MqttUserStoreOptions _options;
        public MqttUserStore(IServiceProvider serviceProvider)
        {
            _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<MqttDbContext>>();
            _dbContext = serviceProvider.GetRequiredService<MqttDbContext>();
            _options = serviceProvider.GetService<MqttUserStoreOptions>();
        }

        #region IMqttUserStore

        public bool CanAuthenticate(MqttUserEntity mqttUser, string password)
        {
            return mqttUser.Password == _hash(password);
        }

        public async Task<bool> CanAuthenticateAsync(string username, string password)
        {
            var hash = _hash(password);
            return await UseDbContextAsync(async dbContext =>
            {
                return await dbContext.MqttUsers.AnyAsync(x => x.UserName == username && x.Password == hash);
            });
        }

        public async Task<MqttUserEntity> FindByUsernameAsync(string username)
        {
            return await UseDbContextAsync(async dbContext =>
            {
                var user = await _dbContext.MqttUsers.FirstOrDefaultAsync(x => x.UserName == username);
                return _loadTopics(dbContext, user);
            });

        }

        public MqttUserEntity FindById(long? userId)
        {
            return UseDbContext(dbContext =>
            {
                var user = dbContext.MqttUsers.Find(userId);
                return _loadTopics(dbContext, user);
            });
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
            await UseDbContextAsync(async dbContext =>
           {
               var user = _dbContext.MqttUsers.Find(id);
               if (user != null)
               {
                   _dbContext.Remove(user);
                   await _dbContext.SaveChangesAsync(cancellationToken);
               }
           });
        }

        #endregion

        #region Helpers

        protected void UseDbContext(Action<MqttDbContext> action)
        {
            using (var dbContext = _dbContext)
            {
                action(dbContext);
            }
        }

        protected TResult UseDbContext<TResult>(Func<MqttDbContext, TResult> func)
        {
            using (var dbContext = _dbContext)
            {
                return func(dbContext);
            }
        }

        protected async Task UseDbContextAsync(Func<MqttDbContext, Task> func)
        {
            if (_options != null && _options.UseDbContextFactory)
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    await func(dbContext);
                }
            }
            await func(_dbContext);
        }

        protected async Task<TResult> UseDbContextAsync<TResult>(Func<MqttDbContext, Task<TResult>> func)
        {
            if (_options != null && _options.UseDbContextFactory)
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    return await func(dbContext);
                }
            }
            return await func(_dbContext);
        }

        private MqttUserEntity _loadTopics(MqttDbContext dbContext, MqttUserEntity user)
        {
            if (user != null)
            {
                if (user.PublishTopics == null)
                {
                    dbContext.Entry(user).Collection(x => x.PublishTopics).Load();
                }

                if (user.SubscribeTopics == null)
                {
                    dbContext.Entry(user).Collection(x => x.SubscribeTopics).Load();
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

    public class MqttUserStoreOptions
    {
        public bool UseDbContextFactory { get; set; }
    }

    public class MqttUserStoreOptionsBuilder
    {
        internal MqttUserStoreOptions Options { get; set; } = new MqttUserStoreOptions();

        public MqttUserStoreOptionsBuilder UseDbContextFactory()
        {
            Options.UseDbContextFactory = true;
            return this;
        }
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
