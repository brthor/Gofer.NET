namespace Thor.Tasks
{
    public interface ITaskQueueBackend
    {
        void Enqueue(string jsonString);
        string Dequeue();

        IBackendLock LockBlocking(string lockKey);
        void SetString(string key, string value);
        string GetString(string key);
    }
}