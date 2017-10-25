using StackExchange.Redis;

namespace Gofer.NET
{
    public partial class TaskQueue
    {
        public static TaskQueue Redis(string redisConnectionString, string queueName=null)
        {
            var config = TaskQueueConfiguration.Default();
            var backend = new RedisTaskQueueBackend(redisConnectionString, 
                queueName ?? config.QueueName, 
                config.BackupQueueName);
            
            var taskQueue = new TaskQueue(backend, config);

            return taskQueue;
        }
    }
}