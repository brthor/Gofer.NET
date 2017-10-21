namespace Thor.Tasks
{
    public class TaskQueueConfiguration
    {
        public string QueueName { get; private set; }
        
        
        /// <summary>
        /// Usage of the Task Queue in Parallel Threads, requires the thread pool size to be 
        /// increased. 
        /// 
        /// https://stackexchange.github.io/StackExchange.Redis/Timeouts#are-you-seeing-high-number-of-busyio-or-busyworker-threads-in-the-timeout-exception
        /// </summary>
        public bool ThreadSafe { get; private set; }
        
        public static TaskQueueConfiguration Default()
        {
            return new TaskQueueConfiguration
            {
                QueueName = "Thor.Tasks.Default",
                ThreadSafe = true
            };
        }
    }
}