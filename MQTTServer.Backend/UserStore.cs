using MQTTServer.Backend.Entities;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MQTTServer.Backend
{
    public class UserStore : IMqttUserStore
    {
        //private readonly ApplicationDbContext _dbContext;
        //public UserStore(ApplicationDbContext dbContext)
        //{
        //    _dbContext = dbContext;
        //}

        #region IMqttUserStore

        public bool CanAuthenticate(UserEntity mqttUser, string password)
        {
            return true;
            //return mqttUser.Password == _hash(password);
        }

        public async Task<bool> CanAuthenticateAsync(string username, string password)
        {
            return true;
            //var hash = _hash(password);
            //return await _dbContext.MqttUsers.AnyAsync(x => x.UserName == username && x.Password == hash);
        }

        public async Task<UserEntity> FindByUsernameAsync(string username)
        {
            return User;
            //return await _dbContext.MqttUsers.FirstOrDefaultAsync(x => x.UserName == username);
        }

        public UserEntity FindById(long? userId)
        {
            return User;
            //return _dbContext.MqttUsers.Find(userId);
        }

        #endregion

        #region Helpers

        UserEntity User => new UserEntity()
        {
            Id = 1,
            UserName = "user",
            Password = _hash("pass"),
            TenantId = 1,
            PublishTopics = new System.Collections.Generic.List<PublishTopicEntity>()
            {
                new PublishTopicEntity(){
                    Id = 1, UserId = 1, Topic = "mytopic"
                }
            },
            SubscribeTopics = new System.Collections.Generic.List<SubscribeTopicEntity>()
            {
                new SubscribeTopicEntity(){
                    Id = 1, UserId = 1, Topic = "mytopic"
                }
            }
        };

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
