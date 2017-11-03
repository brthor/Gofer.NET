using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace Gofer.NET
{
    public partial class TaskQueue
    {
        public ITaskQueueBackend Backend { get; }
        public TaskQueueConfiguration Config { get; }
        
        public TaskQueue(ITaskQueueBackend backend, TaskQueueConfiguration config=null)
        {
            Backend = backend;
            Config = config ?? TaskQueueConfiguration.Default();

            // Usage of the Task Queue in Parallel Threads, requires the thread pool size to be increased.
            // https://stackexchange.github.io/StackExchange.Redis/Timeouts#are-you-seeing-high-number-of-busyio-or-busyworker-threads-in-the-timeout-exception
            if (config.ThreadSafe)
            {
                ThreadPool.SetMinThreads(200, 200);
            }
        }

        public void Enqueue(Expression<Action> expression)
        {
            var taskInfo = expression.ToTaskInfo();
            Enqueue(taskInfo);
        }
        
        private void Enqueue(TaskInfo taskInfo)
        {
            var jsonString = Config.TaskInfoSerializer.Serialize(taskInfo);

            Backend.Enqueue(jsonString);
        }
        
        public void ExecuteNext()
        {
            var (taskJsonString, taskInfo) = SafeDequeue();

            try
            {
                taskInfo?.ExecuteTask();
            }
            finally
            {
                Backend.RemoveBackup(taskJsonString);
            }
        }

        public Tuple<string, TaskInfo> SafeDequeue()
        {
            var jsonString = Backend.DequeueAndBackup();
            var taskInfo = Config.TaskInfoSerializer.Deserialize(jsonString);
            return Tuple.Create(jsonString, taskInfo);
        }
        
        public TaskInfo Dequeue()
        {
            var jsonString = Backend.Dequeue();
            var taskInfo = Config.TaskInfoSerializer.Deserialize(jsonString);
            return taskInfo;
        }

        public void RestoreExpiredBackupTasks()
        {
            TaskInfo taskInfo;

            while (true)
            {
                taskInfo = Config.TaskInfoSerializer.Deserialize(Backend.PeekBackup());
                if (taskInfo?.IsExpired(Config.MessageRetryTimeSpan) ?? false)
                {
                    break;
                }
                
                var lockKey = nameof(RestoreExpiredBackupTasks) + "::" + taskInfo.Id;
                var backupLock = Backend.LockBlocking(lockKey);

                try
                {
                    var currentTop = Config.TaskInfoSerializer.Deserialize(Backend.PeekBackup());
                    if (currentTop.Id.Equals(taskInfo.Id))
                    {
                        Backend.RestoreTopBackup();
                    }
                }
                finally
                {
                    backupLock.Release();
                }
            }
        }
    }
}