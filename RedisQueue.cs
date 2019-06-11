using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Gofer.NET;

namespace Gofer.NET
{
    public class RedisQueue
    {
        private ConnectionMultiplexer Redis { get; }

        /// <summary>
        /// Redis Queue implements a FIFO data structure backed by a redis instance.
        /// 
        /// The "Left" of the list is considered to be the tail of the queue,
        /// and the "Right" the head. Note this is opposite of the StackExchange.Redis
        /// documentation. This is necessary to properly use the `ListRightPopLeftPushAsync`
        /// method to transfer a value from the head of a queue to the tail of another
        /// (used to assure that a message is consumed at-least once).
        /// </summary>
        /// <param name="redis"></param>
        public RedisQueue(ConnectionMultiplexer redis)
        {
            Redis = redis;
        }
        
        public async Task Push(RedisKey queueName, RedisValue value)
        {
            await Redis.GetDatabase().ListLeftPushAsync(queueName, value);
        }

        public async Task<RedisValue> Pop(RedisKey queueName)
        {
            return await Redis.GetDatabase().ListRightPopAsync(queueName);
        }

        public async Task<RedisValue> PopPush(RedisKey popFromQueueName, RedisKey pushToQueueName)
        {
            return await Redis.GetDatabase().ListRightPopLeftPushAsync(popFromQueueName, pushToQueueName);
        }

        public async Task<RedisValue> Peek(RedisKey queueName)
        {
            return await Redis.GetDatabase().ListGetByIndexAsync(queueName, -1);
        }
        
        public async Task<RedisValue> PeekTail(RedisKey queueName)
        {
            return await Redis.GetDatabase().ListGetByIndexAsync(queueName, 0); 
        }
        
        /// <summary>
        /// Removes the first inserted value from the queue. (Searches from head to tail of queue)
        /// </summary>
        /// <returns>false if no value was found to be removed, true otherwise.</returns>
        public async Task<bool> Remove(RedisKey queueName, RedisValue value)
        {
            var removedCount = await Redis.GetDatabase().ListRemoveAsync(queueName, value, -1); 

            return Math.Abs(removedCount) == 1;
        }

        /// <summary>
        /// Removes the last inserted value from the queue. (Searches from tail to head of queue)
        /// </summary>
        /// <returns>false if no value was found to be removed, true otherwise.</returns>
        public async Task<bool> RemoveTail(RedisKey queueName, RedisValue value)
        {
            var removedCount = await Redis.GetDatabase().ListRemoveAsync(queueName, value, 1); 

            return Math.Abs(removedCount) == 1;
        }

        public async Task<IEnumerable<RedisValue>> PopBatch(RedisKey queueName, int batchSize)
        {
            var db = Redis.GetDatabase();
            
            var transaction = db.CreateTransaction();
            
            // `LRANGE` fetches batch values, but in LIFO order
            var listValuesTask = transaction.ListRangeAsync(queueName, start: -batchSize, stop: -1);
            
            var listTrimTask = transaction.ListTrimAsync(queueName, start: 0, stop: -batchSize-1);
            await transaction.ExecuteAsync();

            var listValues = await listValuesTask;
            await listTrimTask;
            
            // Convert list values to FIFO orer
            return listValues.Reverse();
        }

        public async Task<IEnumerable<RedisValue>> PopAll(RedisKey queueName)
        {
            var db = Redis.GetDatabase();
            
            var transaction = db.CreateTransaction();
            
            // `LRANGE <list> 0 1` fetches all values, but in LIFO order
            var listValuesTask = transaction.ListRangeAsync(queueName, start: 0, stop: - 1);
            
            // `LTRIM <list> 1 0` removes all values
            var listTrimTask = transaction.ListTrimAsync(queueName, start: 1, stop: 0);
            await transaction.ExecuteAsync();

            var listValues = await listValuesTask;
            await listTrimTask;
            
            // Convert list values to FIFO orer
            return listValues.Reverse();
        }
    }
}