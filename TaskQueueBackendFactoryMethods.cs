using StackExchange.Redis;

namespace Gofer.NET
{
    public partial class TaskQueue
    {
        public static TaskQueue Redis(string redisConnectionString, string queueName=null)
        {
            var config = TaskQueueConfiguration.Default();
            if (queueName != null) {
                config.QueueName = queueName;
            }
            
            var backend = new RedisBackend(redisConnectionString);
            
            var taskQueue = new TaskQueue(backend, config);

            return taskQueue;
        }
    }
}