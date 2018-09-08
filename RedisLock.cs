using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class RedisLock : IBackendLock
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLock(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async Task Release()
        {
            var success = await _db.LockReleaseAsync(_key, _token);
            if (!success)
            {
                throw new Exception("Unable to release redis lock. " +
                                    "Usually this means that the lock itself timed out and was auto-released.");
            }
        }
    }
}