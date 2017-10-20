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
            Redis.GetDatabase().ListRightPush(stackName, value);
        }

        public RedisValue Pop(RedisKey stackName)
        {
            return Redis.GetDatabase().ListLeftPop(stackName);
        }

        public async Task<RedisValue[]> PopAll(RedisKey stackName)
        {
            var db = Redis.GetDatabase();
            var listLength = db.ListLength(stackName);
            
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