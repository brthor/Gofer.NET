using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Gofer.NET;
using StackExchange.Redis;

[assembly: InternalsVisibleTo("Gofer.NET.Tests")]
namespace Gofer.NET
{
    public class RedisBackend : IBackend
    {
        private ConnectionMultiplexer Redis { get; }
        
        internal RedisQueue RedisQueue { get; }

        public RedisBackend(string redisConnectionString)
        {
            Redis = ConnectionMultiplexer.Connect(redisConnectionString);
            RedisQueue = new RedisQueue(Redis);
        }

        public async Task Enqueue(string queueKey, string jsonString)
        {
            await RedisQueue.Push(queueKey, jsonString);
        }

        public async Task<string> Dequeue(string queueKey)
        {
            var jsonString = await RedisQueue.Pop(queueKey);
            return jsonString;
        }

        public async Task<IEnumerable<string>> DequeueBatch(string queueKey, int batchSize=100)
        {
            var jsonStrings = await RedisQueue.PopBatch(queueKey, batchSize);
            return jsonStrings.Select(v => (string) v);
        }

        public async Task<long> QueueCount(string queueKey)
        {
            return await RedisQueue.Count(queueKey);
        }

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

        public async Task<IEnumerable<string>> GetStrings(IEnumerable<string> keys)
        {
            var values = await Redis.GetDatabase().StringGetAsync(keys.Select(k => (RedisKey) k).ToArray());

            return values.Select(v => (string) v);
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

        public async Task<ISet<string>> GetSet(string key)
        {
            var setMembers = await Redis.GetDatabase().SetMembersAsync(key);
            var setMembersEnumerable = setMembers.Select(v => (string)v);

            var set = new HashSet<string>(setMembersEnumerable);

            return set;
        }

        public async Task AddToSet(string key, string value)
        {
            await Redis.GetDatabase().SetAddAsync(key, value);
        }

        public async Task RemoveFromSet(string key, string value)
        {
            await Redis.GetDatabase().SetRemoveAsync(key, value);
        }

        public async Task<LoadedLuaScript> LoadLuaScript(string scriptContents)
        {
            var server = Redis.GetServer(Redis.GetEndPoints().First());
            var prepared = LuaScript.Prepare(scriptContents);
            return await prepared.LoadAsync(server);
        }

        public async Task<RedisResult> RunLuaScript(LoadedLuaScript script, RedisKey[] keys, RedisValue[] values)
        {
            return await Redis.GetDatabase().ScriptEvaluateAsync(script.Hash, keys, values);
        }

        public async Task AddToOrderedSet(string orderedSetKey, long score, string value)
        {
            await Redis.GetDatabase().SortedSetAddAsync(orderedSetKey, value, score);
        }

        public async Task<bool> RemoveFromOrderedSet(string orderedSetKey, string value)
        {
            return await Redis.GetDatabase().SortedSetRemoveAsync(orderedSetKey, value);
        }

        public async Task SetMapField(string mapKey, string mapField, RedisValue value)
        {
            await Redis.GetDatabase().HashSetAsync(mapKey, mapField, value);
        }

        public async Task SetMapFields(string mapKey, params (RedisValue, RedisValue)[] mapFieldValuePairs)
        {
            var hashEntries = mapFieldValuePairs
                .Select(t => new HashEntry(t.Item1, t.Item2))
                .ToArray();

            await Redis.GetDatabase().HashSetAsync(mapKey, hashEntries);
        }

        public async Task<bool> DeleteMapFields(string mapKey, params RedisValue[] mapFields)
        {
            return await Redis.GetDatabase().HashDeleteAsync(mapKey, mapFields) > 0;
        }

        public async Task<RedisValue> GetMapField(string mapKey, string mapField)
        {
            return await Redis.GetDatabase().HashGetAsync(mapKey, mapField);
        }
    }
}