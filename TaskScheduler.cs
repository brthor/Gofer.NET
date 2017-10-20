using System;
using System.Collections.Generic;

namespace Thor.Tasks
{
    // TODO: Distributed Task Scheduler, this assumes a single client
    public class TaskScheduler
    {
        private class TaskSchedule
        {
            private DateTime StartTime { get; }
            
            private DateTime? LastExecutedTime { get; set; }
            
            private TaskInfo TaskInfo { get; set; }
            
            private TimeSpan Interval { get; set; }

            public TaskSchedule(TaskInfo taskInfo, TimeSpan interval)
            {
                TaskInfo = taskInfo;
                Interval = interval;
                StartTime = DateTime.Now;
            }

            public void RunIfIntervalReachedSinceLastRun()
            {
                var lastRunTime = LastExecutedTime ?? StartTime;

                var difference = DateTime.Now - lastRunTime;
                if (difference > Interval)
                {
                    LogScheduledTaskRun();
                    
                    LastExecutedTime = DateTime.Now;
                    TaskInfo.ExecuteTask();
                }
            }

            private void LogScheduledTaskRun()
            {
                Console.WriteLine($"Running Scheduled Task with interval: {Interval.ToString()}");
            }
        }

        private readonly TaskQueue _taskQueue;
        private Dictionary<string, TaskSchedule> _scheduledTasks;

        public TaskScheduler(TaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
            _scheduledTasks = new Dictionary<string, TaskSchedule>();
        }
        
        public void AddScheduledTask(Action action, TimeSpan interval, string taskName)
        {
            AddScheduledTask(action.ToTaskInfo(new object[] {}), interval, taskName);
        }
        
        public void AddScheduledTask<T>(Action<T> action, TimeSpan interval, string taskName)
        {
            AddScheduledTask(action.ToTaskInfo(new object[] {}), interval, taskName);
        }

        internal void AddScheduledTask(TaskInfo taskInfo, TimeSpan interval, string taskName)
        {
            // TODO: Support Scheduled Tasks with args
            _scheduledTasks[taskName] = new TaskSchedule(taskInfo, interval);
        }

        public void Tick()
        {
            foreach (var taskKvp in _scheduledTasks)
            {
                var task = taskKvp.Value;
                task.RunIfIntervalReachedSinceLastRun();
            }
        }
    }
}