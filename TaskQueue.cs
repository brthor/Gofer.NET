using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gofer.NET.Utils;
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
            // REVIEW: This should no longer be necessary now that we are using the redis async api.
            if (Config.ThreadSafe)
            {
                ThreadPool.SetMinThreads(200, 200);
            }
        }

        public async Task Enqueue(Expression<Action> expression)
        {
            var taskInfo = expression.ToTaskInfo();
            await Enqueue(taskInfo);
        }
        
        private async Task Enqueue(TaskInfo taskInfo)
        {
            var jsonString = JsonTaskInfoSerializer.Serialize(taskInfo);

            await Backend.Enqueue(jsonString);
        }
        
        public async Task ExecuteNext()
        {
            var (taskJsonString, taskInfo) = await SafeDequeue();

            try
            {
                taskInfo?.ExecuteTask();
            }
            finally
            {
//                Backend.RemoveBackup(taskJsonString);
            }
        }
        
        /// <summary>
        /// Returns the serialized TaskInfo as well as deserialized so that the serialized value can later
        /// be removed from the backing queue.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<string, TaskInfo>> SafeDequeue()
        {
            var jsonString = await Backend.Dequeue();
            if (jsonString == null)
            {
                return Tuple.Create<string, TaskInfo>(null, null);
            }
            
            var taskInfo = JsonTaskInfoSerializer.Deserialize(jsonString);
            return Tuple.Create(jsonString, taskInfo);
        }
        
        public async Task<TaskInfo> Dequeue()
        {
            var jsonString = await Backend.Dequeue();
            if (jsonString == null)
            {
                return null;
            }
            
            var taskInfo = JsonTaskInfoSerializer.Deserialize(jsonString);
            return taskInfo;
        }

        public void RestoreExpiredBackupTasks()
        {
//            TaskInfo taskInfo;
//
//            while (true)
//            {
//                taskInfo = JsonTaskInfoSerializer.Deserialize(Backend.PeekBackup());
//                if (taskInfo?.IsExpired(Config.MessageRetryTimeSpan) ?? true)
//                {
//                    break;
//                }
//                
//                var lockKey = nameof(RestoreExpiredBackupTasks) + "::" + taskInfo.Id;
//                var backupLock = Backend.LockBlocking(lockKey);
//
//                try
//                {
//                    var currentTopStr = Backend.PeekBackup();
//                    if (!string.IsNullOrEmpty(currentTopStr))
//                    {
//                        var currentTop = JsonTaskInfoSerializer.Deserialize(currentTopStr);
//                        if (currentTop?.Id.Equals(taskInfo.Id) ?? false)
//                        {
//                            Backend.RestoreTopBackup();
//                        }
//                    }
//                }
//                finally
//                {
//                    backupLock.Release();
//                }
//            }
        }
    }
}