using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace Thor.Tasks
{
    public partial class TaskQueue
    {
        public ITaskQueueBackend Backend { get; }
        public TaskQueueConfiguration Config { get; }
        
        public TaskQueue(ITaskQueueBackend backend, TaskQueueConfiguration config=null)
        {
            Backend = backend;
            Config = config ?? new TaskQueueConfiguration();

            /// Usage of the Task Queue in Parallel Threads, requires the thread pool size to be increased.
            /// 
            /// https://stackexchange.github.io/StackExchange.Redis/Timeouts#are-you-seeing-high-number-of-busyio-or-busyworker-threads-in-the-timeout-exception
            if (config.ThreadSafe)
            {
                ThreadPool.SetMinThreads(200, 200);
            }
        }
        
        public void Enqueue<T>(Action<T> action, object[] args=null)
        {
            Enqueue(action.ToTaskInfo(args));
        }

        public void Enqueue(Action action, object[] args=null)
        {
            Enqueue(action.ToTaskInfo(args));
        }

        public void Enqueue(TaskInfo taskInfo)
        {
            var jsonString = JsonConvert.SerializeObject(taskInfo);

            Backend.Enqueue(jsonString);
        }
        
        public void ExecuteNext()
        {
            var taskInfo = Dequeue();

            taskInfo?.ExecuteTask();
        }

        public TaskInfo Dequeue()
        {
            var jsonString = Backend.Dequeue();
            if (jsonString == null)
            {
                return null;
            }
            
            var taskInfo = JsonConvert.DeserializeObject<TaskInfo>(jsonString);
            return taskInfo;
        }
    }
}