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
            Retry.OnException(() => Redis.GetDatabase().ListLeftPush(stackName, value),
                new[] {typeof(RedisTimeoutException)});
        }

        public RedisValue Pop(RedisKey stackName)
        {
            return Retry.OnException(() => Redis.GetDatabase().ListRightPop(stackName),
                new[] {typeof(RedisTimeoutException)});
        }

        public RedisValue PopPush(RedisKey popFromStackName, RedisKey pushToStackName)
        {
            return Retry.OnException(() => Redis.GetDatabase().ListRightPopLeftPush(popFromStackName, pushToStackName), 
                new[] {typeof(RedisTimeoutException)});
        }

        public RedisValue Peek(RedisKey stackName)
        {
            return Retry.OnException(() => Redis.GetDatabase().ListGetByIndex(stackName, -1), 
                new[] {typeof(RedisTimeoutException)});
        }
        
        public RedisValue PeekTail(RedisKey stackName)
        {
            return Retry.OnException(() => Redis.GetDatabase().ListGetByIndex(stackName, 0), 
                new[] {typeof(RedisTimeoutException)});
        }

        public bool Remove(RedisKey stackName, RedisValue value)
        {
            var removedCount = Retry.OnException(() => Redis.GetDatabase().ListRemove(stackName, value, -1), 
                new[] {typeof(RedisTimeoutException)});

            return Math.Abs(removedCount) == 1;
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