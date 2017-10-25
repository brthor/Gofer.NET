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
            Config = config ?? new TaskQueueConfiguration();

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
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());
            
            var jsonString = JsonConvert.SerializeObject(taskInfo, settings);

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
            var taskInfo = JsonToTaskInfo(jsonString);
            return Tuple.Create(jsonString, taskInfo);
        }
        
        public TaskInfo Dequeue()
        {
            var jsonString = Backend.Dequeue();
            var taskInfo = JsonToTaskInfo(jsonString);
            return taskInfo;
        }

        public void RestoreExpiredBackupTasks()
        {
            TaskInfo taskInfo;

            while ((taskInfo = JsonToTaskInfo(Backend.PeekBackup()))?.IsExpired(Config.MessageRetryTimeSpan) ?? false)
            {
                var lockKey = nameof(RestoreExpiredBackupTasks) + "::" + taskInfo.Id;
                var backupLock = Backend.LockBlocking(lockKey);

                try
                {
                    var currentTop = JsonToTaskInfo(Backend.PeekBackup());
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

        private TaskInfo JsonToTaskInfo(string jsonString)
        {
            if (jsonString == null)
            {
                return null;
            }
            
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Insert(0, new JsonPrimitiveConverter());

            var taskInfo = JsonConvert.DeserializeObject<TaskInfo>(jsonString, settings);
            return taskInfo;
        }
    }
}