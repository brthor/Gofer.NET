using System.Collections;
using System.Collections.Generic;

namespace Gofer.NET
{
    public interface ITaskQueueBackend
    {
        void Enqueue(string jsonString);
        string Dequeue();

        string DequeueAndBackup();
        string PeekBackup();
        string RestoreTopBackup();
        void RemoveBackup(string jsonString);

        IBackendLock LockBlocking(string lockKey);
        void SetString(string key, string value);
        string GetString(string key);

        long AddToList(string key, string value);
        long RemoveFromList(string key, string value);

        IEnumerable<string> GetList(string key);
        void DeleteKey(string scheduleBackupKey);
    }
}