namespace Thor.Tasks
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
    }
}