using System;
using StackExchange.Redis;

namespace Thor.Tasks
{
    public static class RedisExtensions
    {
        public static RedisLock LockBlocking(this ConnectionMultiplexer redis, string key, TimeSpan? duration=null)
        {
            var rDuration = duration ?? TimeSpan.FromMinutes(5);
            
            RedisValue token = Guid.NewGuid().ToString();
            var db = redis.GetDatabase();


            Retry.OnValue(() => db.LockTake(key, token, rDuration), false);
            
            return new RedisLock(db, key, token);
        }
    }
}