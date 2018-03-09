using System;
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

        public void Release()
        {
            var success = _db.LockRelease(_key, _token);
            if (!success)
            {
                throw new ApplicationException("Unable to release redis lock.");
            }
        }
    }
}