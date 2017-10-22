using System.Collections;
using StackExchange.Redis;

namespace Thor.Tasks
{
    public class RedisTaskQueueBackend : ITaskQueueBackend
    {
        private ConnectionMultiplexer Redis { get; }
        private RedisQueue RedisQueue { get; }
        private string QueueName { get; }
        
        public RedisTaskQueueBackend(string redisConnectionString, string queueName)
        {
            Redis = ConnectionMultiplexer.Connect(redisConnectionString);
            RedisQueue = new RedisQueue(Redis);
            QueueName = queueName;
        }

        public void Enqueue(string jsonString)
        {
            RedisQueue.Push(QueueName, jsonString);
        }

        public string Dequeue()
        {
            var jsonString = RedisQueue.Pop(QueueName);
            return jsonString;
        }

        public IBackendLock LockBlocking(string lockKey)
        {
            return Redis.LockBlocking(lockKey);
        }

        public void SetString(string key, string value)
        {
            Redis.GetDatabase().StringSet(key, value);
        }

        public string GetString(string key)
        {
            return Redis.GetDatabase().StringGet(key);
        }
    }
}