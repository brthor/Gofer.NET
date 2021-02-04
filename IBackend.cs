using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Gofer.NET
{
    public interface IBackend
    {
        Task Enqueue(string queueKey, string jsonString);

        Task<string> Dequeue(string queueKey);

        Task<IEnumerable<string>> DequeueBatch(string queueKey, int batchSize=100);

        Task<long> GetQueueDepth(string key);

        Task<IBackendLock> LockBlocking(string lockKey);

        Task<IBackendLock> LockNonBlocking(string lockKey);
        
        Task SetString(string key, string value);

        Task<string> GetString(string key);

        Task<IEnumerable<string>> GetStrings(IEnumerable<string> key);

        Task<ISet<string>> GetSet(string key);

        Task AddToSet(string key, string value);

        Task RemoveFromSet(string key, string value);

        Task<IEnumerable<string>> GetList(string key);

        Task<long> AddToList(string key, string value);

        Task<long> RemoveFromList(string key, string value);

        Task DeleteKey(string key);

        Task<LoadedLuaScript> LoadLuaScript(string scriptContents);

        Task<RedisResult> RunLuaScript(LoadedLuaScript script, RedisKey[] keys, RedisValue[] values);

        Task AddToOrderedSet(string setKey, long score, string value);
 
        Task<bool> RemoveFromOrderedSet(string setKey, string value);

        Task SetMapField(string mapKey, string mapField, RedisValue value);

        Task SetMapFields(string mapKey, params (RedisValue, RedisValue)[] mapFieldValuePairs);

        Task<bool> DeleteMapFields(string mapKey, params RedisValue[] mapFields);

        Task<RedisValue> GetMapField(string mapKey, string mapField);
    }
}