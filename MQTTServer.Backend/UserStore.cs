using Microsoft.EntityFrameworkCore;
using MQTTServer.Backend.Entities;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MQTTServer.Backend
{
    public class UserStore : IMqttUserStore
    {
        private readonly MqttDbContext _dbContext;
        public UserStore(MqttDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region IMqttUserStore

        public bool CanAuthenticate(UserEntity mqttUser, string password)
        {
            return mqttUser.Password == _hash(password);
        }

        public async Task<bool> CanAuthenticateAsync(string username, string password)
        {
            var hash = _hash(password);
            return await _dbContext.MqttUsers.AnyAsync(x => x.UserName == username && x.Password == hash);
        }

        public async Task<UserEntity> FindByUsernameAsync(string username)
        {
            var user = await _dbContext.MqttUsers.FirstOrDefaultAsync(x => x.UserName == username);
            return _loadTopics(user);
        }

        public UserEntity FindById(long? userId)
        {
            var user = _dbContext.MqttUsers.Find(userId);
            return _loadTopics(user);
        }

        #endregion

        #region Helpers

        private UserEntity _loadTopics(UserEntity user)
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
        UserEntity FindById(long? userId);
        Task<UserEntity> FindByUsernameAsync(string username);
        bool CanAuthenticate(UserEntity mqttUser, string password);
        Task<bool> CanAuthenticateAsync(string username, string password);
    }
}
