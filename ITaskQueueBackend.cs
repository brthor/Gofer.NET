using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gofer.NET
{
    public interface ITaskQueueBackend
    {
        Task Enqueue(string jsonString);
        Task<string> Dequeue();

//        string DequeueAndBackup();
//        string PeekBackup();
//        string RestoreTopBackup();
//        void RemoveBackup(string jsonString);

        Task<IBackendLock> LockBlocking(string lockKey);
        Task<IBackendLock> LockNonBlocking(string lockKey);
        
        Task SetString(string key, string value);
        Task<string> GetString(string key);

        Task<long> AddToList(string key, string value);
        Task<long> RemoveFromList(string key, string value);

        Task<IEnumerable<string>> GetList(string key);
        Task DeleteKey(string scheduleBackupKey);
    }
}