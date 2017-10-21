using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Thor.Tasks
{
    public class RedisQueue
    {
        private ConnectionMultiplexer Redis { get; }

        public RedisQueue(ConnectionMultiplexer redis)
        {
            Redis = redis;
        }
        
        public void Push(RedisKey stackName, RedisValue value)
        {
            Retry.OnException(() => Redis.GetDatabase().ListRightPush(stackName, value),
                new[] {typeof(RedisTimeoutException)});
        }

        public RedisValue Pop(RedisKey stackName)
        {
            return Retry.OnException(() => Redis.GetDatabase().ListLeftPop(stackName),
                new[] {typeof(RedisTimeoutException)});
        }

        public async Task<RedisValue[]> PopAll(RedisKey stackName)
        {
            var db = Redis.GetDatabase();

            var lockName = "PopAllLock::" + (string) stackName;
            var listLength = db.ListLength(lockName);
            
            var transaction = db.CreateTransaction();
            var listValuesTask = transaction.ListRangeAsync(stackName, stop: listLength - 1);
            var listTrimTask = transaction.ListTrimAsync(stackName, start: listLength, stop: -1);
            transaction.Execute();

            var listValues = await listValuesTask;
            await listTrimTask;

            return listValues;
        }
    }
}