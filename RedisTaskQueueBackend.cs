using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gofer.NET;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class RedisTaskQueueBackend : ITaskQueueBackend
    {
        private ConnectionMultiplexer Redis { get; }
        private RedisQueue RedisQueue { get; }
        private string QueueName { get; }
        public string BackupQueueName { get; }

        public RedisTaskQueueBackend(string redisConnectionString, string queueName, string backupQueueName)
        {
            Redis = ConnectionMultiplexer.Connect(redisConnectionString);
            RedisQueue = new RedisQueue(Redis);
            QueueName = queueName;
            BackupQueueName = backupQueueName;
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

        public string DequeueAndBackup()
        {
            var jsonString = RedisQueue.PopPush(QueueName, BackupQueueName);
            return jsonString;
        }

        public string PeekBackup()
        {
            var jsonString = RedisQueue.Peek(BackupQueueName);
            return jsonString;
        }

        public string RestoreTopBackup()
        {
            var jsonString = RedisQueue.PopPush(BackupQueueName, QueueName);
            return jsonString;
        }

        public void RemoveBackup(string jsonString)
        {
            if (jsonString == null)
                return;
            
            RedisQueue.Remove(BackupQueueName, jsonString);
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

        public long AddToList(string key, string value)
        {
            return Redis.GetDatabase().ListLeftPush(key, value);
        }

        public long RemoveFromList(string key, string value)
        {
            return Redis.GetDatabase().ListRemove(key, value);
        }

        public IEnumerable<string> GetList(string key)
        {
            return Redis.GetDatabase().ListRange(key).Select(v => (string)v);
        }
    }
}