using System;
using Gofer.NET.Utils;

namespace Gofer.NET
{
    public class TaskQueueConfiguration
    {
        public string QueueName { get; set; }
        
        /// <summary>
        /// If a task is not removed from the backup list after this long, it is assumed to have been abandoned or its
        /// consumer died. It will be reinserted into the processing queue.
        /// Default: 1 hour
        /// </summary>
        public TimeSpan MessageRetryTimeSpan { get; set; }
        
        
        /// <summary>
        /// Usage of the Task Queue in Parallel Threads, requires the thread pool size to be 
        /// increased. 
        /// 
        /// https://stackexchange.github.io/StackExchange.Redis/Timeouts#are-you-seeing-high-number-of-busyio-or-busyworker-threads-in-the-timeout-exception
        /// </summary>
        public bool ThreadSafe { get; set; }

        public int BatchSize { get; set; }
        
        public static TaskQueueConfiguration Default()
        {
            return new TaskQueueConfiguration
            {
                QueueName = "Gofer.NET.Default",
                ThreadSafe = true,
                MessageRetryTimeSpan = TimeSpan.FromHours(1),
                BatchSize = 20
            };
        }
    }
}