using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gofer.NET;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class RedisTaskQueueBackend : ITaskQueueBackend
    {
        private ConnectionMultiplexer Redis { get; }
        private RedisQueue RedisQueue { get; }
        private string QueueName { get; }
//        public string BackupQueueName { get; }

        public RedisTaskQueueBackend(string redisConnectionString, string queueName, string backupQueueName)
        {
            Redis = ConnectionMultiplexer.Connect(redisConnectionString);
            RedisQueue = new RedisQueue(Redis);
            QueueName = queueName;
//            BackupQueueName = backupQueueName;
        }

        public async Task Enqueue(string jsonString)
        {
            await RedisQueue.Push(QueueName, jsonString);
        }

        public async Task<string> Dequeue()
        {
            var jsonString = await RedisQueue.Pop(QueueName);
            return jsonString;
        }

//        public string DequeueAndBackup()
//        {
//            var jsonString = RedisQueue.PopPush(QueueName, BackupQueueName);
//            return jsonString;
//        }
//
//        public string PeekBackup()
//        {
//            var jsonString = RedisQueue.Peek(BackupQueueName);
//            return jsonString;
//        }
//
//        public string RestoreTopBackup()
//        {
//            var jsonString = RedisQueue.PopPush(BackupQueueName, QueueName);
//            return jsonString;
//        }
//
//        public void RemoveBackup(string jsonString)
//        {
//            if (jsonString == null)
//                return;
//            
//            RedisQueue.Remove(BackupQueueName, jsonString);
//        }

        public async Task<IBackendLock> LockBlocking(string lockKey)
        {
            return await Redis.LockBlockingAsync(lockKey);
        }
        
        public async Task<IBackendLock> LockNonBlocking(string lockKey)
        {
            return await Redis.LockNonBlockingAsync(lockKey);
        }

        public async Task SetString(string key, string value)
        {
            await Redis.GetDatabase().StringSetAsync(key, value);
        }

        public async Task<string> GetString(string key)
        {
            return await Redis.GetDatabase().StringGetAsync(key);
        }

        public async Task<long> AddToList(string key, string value)
        {
            return await Redis.GetDatabase().ListLeftPushAsync(key, value);
        }

        public async Task<long> RemoveFromList(string key, string value)
        {
            return await Redis.GetDatabase().ListRemoveAsync(key, value);
        }

        public async Task<IEnumerable<string>> GetList(string key)
        {
            var list = await Redis.GetDatabase().ListRangeAsync(key);
            var strList = list.Select(v => (string)v);

            return strList;
        }

        public async Task DeleteKey(string scheduleBackupKey)
        {
            await Redis.GetDatabase().KeyDeleteAsync(scheduleBackupKey);
        }
    }
}