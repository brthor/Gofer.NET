using StackExchange.Redis;

namespace Thor.Tasks
{
    public partial class TaskQueue
    {
        public static TaskQueue Redis(string redisConnectionString, string queueName=null)
        {
            var config = TaskQueueConfiguration.Default();
            var backend = new RedisTaskQueueBackend(redisConnectionString, queueName ?? config.QueueName);
            
            var taskQueue = new TaskQueue(backend, config);

            return taskQueue;
        }
    }
}