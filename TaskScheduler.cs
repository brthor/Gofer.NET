using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class TaskScheduler
    {
        private class TaskSchedule
        {
            private readonly ITaskQueueBackend _backend;
            private DateTime StartTime { get; }
            
            private TaskInfo TaskInfo { get; set; }
            
            private TimeSpan Interval { get; set; }

            public TaskSchedule(
                TaskInfo taskInfo, 
                TimeSpan interval,
                ITaskQueueBackend backend)
            {
                _backend = backend;
                TaskInfo = taskInfo;
                Interval = interval;
                StartTime = DateTime.Now;
            }

            public void RunIfIntervalReachedSinceLastRun(string taskId)
            {
                var lockKey = $"{nameof(TaskSchedule)}::{taskId}::ScheduleLock";
                var lastRunValueKey = $"{nameof(TaskSchedule)}::{taskId}::LastRunValue";
                
                var backendLock = _backend.LockBlocking(lockKey);
                try
                {
                    var lastRunTime = GetLastRunTime(lastRunValueKey); 

                    var difference = DateTime.Now - lastRunTime;
                    if (difference > Interval)
                    {
                        SetLastRunTime(lastRunValueKey);
                        LogScheduledTaskRun();
                    
                        TaskInfo.ExecuteTask();
                    }
                }
                finally
                {
                    backendLock.Release();
                }
            }

            /// <summary>
            /// Not Thread safe. Use External locking.
            /// </summary>
            private DateTime GetLastRunTime(string lastRunValueKey)
            {
                var jsonString = _backend.GetString(lastRunValueKey);

                if (string.IsNullOrEmpty(jsonString))
                {
                    return StartTime;
                }

                return JsonConvert.DeserializeObject<DateTime>(jsonString);
            }
            
            /// <summary>
            /// Not thread safe. Use external locking.
            /// </summary>
            private void SetLastRunTime(string lastRunValueKey)
            {
                _backend.SetString(lastRunValueKey, JsonConvert.SerializeObject(DateTime.Now));
            }

            private void LogScheduledTaskRun()
            {
                Console.WriteLine($"Running Scheduled Task with interval: {Interval.ToString()}");
            }
        }

        private readonly TaskQueue _taskQueue;
        private readonly Dictionary<string, TaskSchedule> _scheduledTasks;

        public TaskScheduler(TaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
            _scheduledTasks = new Dictionary<string, TaskSchedule>();
        }
        
        public void AddScheduledTask(Expression<Action> action, TimeSpan interval, string taskName)
        {
            AddScheduledTask(action.ToTaskInfo(), interval, taskName);
        }
        
        internal void AddScheduledTask(TaskInfo taskInfo, TimeSpan interval, string taskName)
        {
            _scheduledTasks[taskName] = new TaskSchedule(taskInfo, interval, _taskQueue.Backend);
        }

        public void Tick()
        {
            foreach (var taskKvp in _scheduledTasks)
            {
                var taskId = taskKvp.Key;
                var task = taskKvp.Value;
                task.RunIfIntervalReachedSinceLastRun(taskId);
            }
        }
    }
}
