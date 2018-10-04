using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Gofer.NET.Utils;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class TaskScheduler
    {
//        private string ScheduleBackupKey => $"{nameof(TaskSchedule)}::ScheduleBackupList";

        private readonly TaskQueue _taskQueue;
        private readonly Dictionary<string, TaskSchedule> _scheduledTasks;

        public TaskScheduler(TaskQueue taskQueue, bool restoreFromBackup=false)
        {
            _taskQueue = taskQueue;
            
            _scheduledTasks = new Dictionary<string, TaskSchedule>();

//            if (restoreFromBackup)
//            {
//                RestoreScheduledTasksFromStorage();
//            }
        }

        public async Task Tick()
        {
            var tasks = _scheduledTasks.Values.ToList();
            foreach (var task in tasks)
            {
                try
                {
                    // Ensure only one worker processes the scheduled task at a time.
                    var backendLock = await _taskQueue.Backend.LockNonBlocking(task.LockKey);
                    if (backendLock == null)
                        continue;

                    try
                    {
                        var taskDidRun = await task.RunIfScheduleReached();
                        if (taskDidRun && !task.IsRecurring)
                        {
                            RemoveTaskFromSchedule(task);
                        }
                    }
                    finally
                    {
                        await backendLock.Release();
                    }
                }
                catch (Exception e)
                {
                    ThreadSafeColoredConsole.Exception("Error processing Schedule Tick.", e);
                }
            }
        }

        public async Task AddScheduledTask(Expression<Action> action, TimeSpan offsetFromNow, string taskName)
        {
            await AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), offsetFromNow, _taskQueue.Backend, false, taskName));
        }

        public async Task AddScheduledTask(Expression<Action> action, DateTimeOffset offsetFromNow, string taskName)
        {
            await AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), offsetFromNow, _taskQueue.Backend, false, taskName));
        }

        public async Task AddScheduledTask(Expression<Action> action, DateTime scheduledTime, string taskName)
        {
            await AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), scheduledTime, _taskQueue.Backend, false, taskName));
        }

        public async Task AddRecurringTask(Expression<Action> action, TimeSpan interval, string taskName)
        {
            await AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), interval, _taskQueue.Backend, true, taskName));
        }

        public async Task AddRecurringTask(Expression<Action> action, string crontab, string taskName)
        {
            await AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), crontab, _taskQueue.Backend, taskName));
        }

        public void RemoveScheduledTask(string taskName)
        {
            if (!_scheduledTasks.ContainsKey(taskName))
            {
                throw new ArgumentException($"Scheduled or Recurring Task not found: {taskName}");
            }

            RemoveTaskFromSchedule(_scheduledTasks[taskName]);
        }

        private async Task AddTaskToSchedule(TaskSchedule taskSchedule)
        {
            _scheduledTasks[taskSchedule.TaskKey] = taskSchedule;
            
            await taskSchedule.ClearLastRunTime();

//            var jsonTaskSchedule = JsonTaskInfoSerializer.Serialize(taskSchedule);
//            _taskQueue.Backend.AddToList(ScheduleBackupKey, jsonTaskSchedule);
        }

        private void RemoveTaskFromSchedule(TaskSchedule taskSchedule)
        {
//            var jsonTaskSchedule = JsonTaskInfoSerializer.Serialize(taskSchedule);
//            _taskQueue.Backend.RemoveFromList(ScheduleBackupKey, jsonTaskSchedule);

            _scheduledTasks.Remove(taskSchedule.TaskKey);
        }

        private void RestoreScheduledTasksFromStorage()
        {
//            var scheduledTasks = _taskQueue.Backend.GetList(ScheduleBackupKey)
//                .Select(s => JsonTaskInfoSerializer.Deserialize<TaskSchedule>(s)).ToList();
//
//            foreach (var scheduledTask in scheduledTasks)
//            {
//                _scheduledTasks[scheduledTask.TaskKey] = scheduledTask;
//            }
        }

        public void FlushBackupStorage()
        {
//            _taskQueue.Backend.DeleteKey(ScheduleBackupKey);
        }
    }
}
