using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Gofer.NET.Utils;
using StackExchange.Redis;

namespace Gofer.NET
{
    public class TaskScheduler
    {
        private string ScheduleBackupKey => $"{nameof(TaskSchedule)}::ScheduleBackupList";

        private readonly TaskQueue _taskQueue;
        private readonly Dictionary<string, TaskSchedule> _scheduledTasks;

        public TaskScheduler(TaskQueue taskQueue, bool restoreFromBackup=true)
        {
            _taskQueue = taskQueue;
            _scheduledTasks = new Dictionary<string, TaskSchedule>();

            if (restoreFromBackup)
            {
                RestoreScheduledTasksFromStorage();
            }
        }

        public void Tick()
        {
            var tasks = _scheduledTasks.Values.ToList();
            foreach (var task in tasks)
            {
                var taskDidRun = task.RunIfScheduleReached();

                if (taskDidRun && !task.IsRecurring)
                {
                    RemoveTaskFromSchedule(task);
                }
            }
        }

        public void AddScheduledTask(Expression<Action> action, TimeSpan offsetFromNow, string taskName)
        {
            AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), offsetFromNow, _taskQueue.Backend, false, taskName));
        }

        public void AddScheduledTask(Expression<Action> action, DateTimeOffset offsetFromNow, string taskName)
        {
            AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), offsetFromNow, _taskQueue.Backend, false, taskName));
        }

        public void AddScheduledTask(Expression<Action> action, DateTime scheduledTime, string taskName)
        {
            AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), scheduledTime, _taskQueue.Backend, false, taskName));
        }

        public void AddRecurringTask(Expression<Action> action, TimeSpan interval, string taskName)
        {
            AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), interval, _taskQueue.Backend, true, taskName));
        }

        public void AddRecurringTask(Expression<Action> action, string crontab, string taskName)
        {
            AddTaskToSchedule(new TaskSchedule(action.ToTaskInfo(), crontab, _taskQueue.Backend, taskName));
        }

        public void RemoveScheduledTask(string taskName)
        {
            if (!_scheduledTasks.ContainsKey(taskName))
            {
                throw new ArgumentException($"Scheduled or Recurring Task not found: {taskName}");
            }

            RemoveTaskFromSchedule(_scheduledTasks[taskName]);
        }

        private void AddTaskToSchedule(TaskSchedule taskSchedule)
        {
            _scheduledTasks[taskSchedule.TaskKey] = taskSchedule;
            taskSchedule.ClearLastRunTime();

            var jsonTaskSchedule = new JsonTaskInfoSerializer().Serialize(taskSchedule);
            _taskQueue.Backend.AddToList(ScheduleBackupKey, jsonTaskSchedule);
        }

        private void RemoveTaskFromSchedule(TaskSchedule taskSchedule)
        {
            var jsonTaskSchedule = new JsonTaskInfoSerializer().Serialize(taskSchedule);
            _taskQueue.Backend.RemoveFromList(ScheduleBackupKey, jsonTaskSchedule);

            _scheduledTasks.Remove(taskSchedule.TaskKey);
        }

        private void RestoreScheduledTasksFromStorage()
        {
            var serializer = new JsonTaskInfoSerializer();
            var scheduledTasks = _taskQueue.Backend.GetList(ScheduleBackupKey)
                .Select(s => serializer.Deserialize<TaskSchedule>(s)).ToList();

            foreach (var scheduledTask in scheduledTasks)
            {
                _scheduledTasks[scheduledTask.TaskKey] = scheduledTask;
            }
        }

        public void FlushBackupStorage()
        {
            _taskQueue.Backend.DeleteKey(ScheduleBackupKey);
        }
    }
}
