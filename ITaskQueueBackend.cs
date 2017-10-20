namespace Thor.Tasks
{
    public interface ITaskQueueBackend
    {
        void Enqueue(string jsonString);
        string Dequeue();
    }
}