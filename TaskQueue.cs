using System;
using System.Collections.Generic;
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
        public IBackend Backend { get; }

        public TaskQueueConfiguration Config { get; }
        
        public TaskQueue(IBackend backend, TaskQueueConfiguration config=null)
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
        
        internal async Task Enqueue(TaskInfo taskInfo)
        {
            taskInfo.ConvertTypeArgs();
            var jsonString = JsonTaskInfoSerializer.Serialize(taskInfo);

            await Backend.Enqueue(Config.QueueName, jsonString);
        }

        public async Task<long> GetQueueDepth()
        {
            return await Backend.GetQueueDepth(Config.QueueName);
        }
        
        public async Task<bool> ExecuteNext()
        {
            var (taskJsonString, taskInfo) = await SafeDequeue();

            if (taskInfo == null)
            {
                return false;
            }

            try
            {
                await taskInfo.ExecuteTask();
            }
            finally
            {
//                Backend.RemoveBackup(taskJsonString);
            }

            return true;
        }
        
        /// <summary>
        /// Returns the serialized TaskInfo as well as deserialized so that the serialized value can later
        /// be removed from the backing queue.
        /// </summary>
        /// <returns></returns>
        public async Task<(string, TaskInfo)> SafeDequeue()
        {
            var jsonString = await Backend.Dequeue(Config.QueueName);
            if (jsonString == null)
            {
                return (null, null);
            }
            
            var taskInfo = JsonTaskInfoSerializer.Deserialize(jsonString);
            taskInfo.UnconvertTypeArgs();
            return (jsonString, taskInfo);
        }
        
        internal async Task<TaskInfo> Dequeue()
        {
            var jsonString = await Backend.Dequeue(Config.QueueName);
            if (jsonString == null)
            {
                return null;
            }
            
            var taskInfo = JsonTaskInfoSerializer.Deserialize(jsonString);
            taskInfo.UnconvertTypeArgs();
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