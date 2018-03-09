using System;
using Gofer.NET.Utils;
using NCrontab;
using Newtonsoft.Json;

namespace Gofer.NET
{
    public class TaskSchedule
    {

        public string LockKey => $"{nameof(TaskSchedule)}::{TaskKey}::ScheduleLock";

        private string LastRunValueKey => $"{nameof(TaskSchedule)}::{TaskKey}::LastRunValue";
        public bool IsRecurring { get; }
        public string TaskKey { get; }

        private readonly DateTime _startTime;
        private readonly TaskInfo _taskInfo;
        private readonly ITaskQueueBackend _backend;
        private readonly TimeSpan? _intervalOrOffsetFromNow;
        private readonly DateTimeOffset? _scheduledTimeAsDateTimeOffset;
        private readonly DateTime? _scheduledTime;
        private readonly string _crontab;

        public TaskSchedule() { }

        public TaskSchedule(
            TaskInfo taskInfo,
            TimeSpan interval,
            ITaskQueueBackend backend,
            bool isRecurring, string taskId) : this(taskInfo, backend, isRecurring, taskId)
        {
            _intervalOrOffsetFromNow = interval;
        }

        public TaskSchedule(
            TaskInfo taskInfo,
            DateTimeOffset scheduledTimeAsDateTimeOffset,
            ITaskQueueBackend backend,
            bool isRecurring, string taskId) : this(taskInfo, backend, isRecurring, taskId)
        {
            _scheduledTimeAsDateTimeOffset = scheduledTimeAsDateTimeOffset;
        }

        public TaskSchedule(
            TaskInfo taskInfo,
            DateTime scheduledTime,
            ITaskQueueBackend backend,
            bool isRecurring, string taskId) : this(taskInfo, backend, isRecurring, taskId)
        {
            _scheduledTime =  scheduledTime;
        }

        public TaskSchedule(
            TaskInfo taskInfo,
            string crontab,
            ITaskQueueBackend backend, string taskKey) : this(taskInfo, backend, true, taskKey)
        {
            ValidateCrontab(crontab);
            _crontab = crontab;
        }

        private TaskSchedule(TaskInfo taskInfo, ITaskQueueBackend backend, bool isRecurring, string taskKey)
        {
            _backend = backend;
            _taskInfo = taskInfo;
            _startTime = DateTime.UtcNow;

            TaskKey = taskKey;
            IsRecurring = isRecurring;
        }

        /// <summary>
        /// Returns true if the task is run.
        /// </summary>
        public bool RunIfScheduleReached()
        {
            var lastRunTime = GetLastRunTime(LastRunValueKey);

            // If we've already run before, and aren't recurring, dont run again.
            if (lastRunTime.HasValue && !IsRecurring)
            {
                return true;
            }

            if (TaskShouldExecuteBasedOnSchedule(lastRunTime ?? _startTime))
            {
                SetLastRunTime();
                LogScheduledTaskRun();

                _taskInfo.ExecuteTask();
                return true;
            }

            return false;
        }

        private bool TaskShouldExecuteBasedOnSchedule(DateTime lastRunTime)
        {
            if (_intervalOrOffsetFromNow.HasValue)
            {
                var difference = DateTime.UtcNow - lastRunTime;

                return difference >= _intervalOrOffsetFromNow;
            }

            if (_scheduledTimeAsDateTimeOffset.HasValue)
            {
                var utcScheduledTime = _scheduledTimeAsDateTimeOffset.Value.ToUniversalTime();

                return DateTime.UtcNow >= utcScheduledTime;
            }

            if (_scheduledTime.HasValue)
            {
                var utcScheduledTime = _scheduledTime.Value.ToUniversalTime();

                return DateTime.UtcNow >= utcScheduledTime;
            }

            if (_crontab != null)
            {
                var crontabSchedule = CrontabSchedule.Parse(_crontab, new CrontabSchedule.ParseOptions() { IncludingSeconds = true });

                var nextOccurence = crontabSchedule.GetNextOccurrence(lastRunTime);
                return DateTime.UtcNow >= nextOccurence;
            }

            throw new Exception("Invalid scheduling mechanism used. This is a code bug, should not happen.");

        }

        /// <summary>
        /// Not Thread safe. Use External locking.
        /// </summary>
        private DateTime? GetLastRunTime(string lastRunValueKey)
        {
            var jsonString = _backend.GetString(lastRunValueKey);

            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<DateTime>(jsonString);
        }
            
        /// <summary>
        /// Not thread safe. Use external locking.
        /// </summary>
        private void SetLastRunTime()
        {
            _backend.SetString(LastRunValueKey, JsonConvert.SerializeObject(DateTime.UtcNow));
        }

        private void LogScheduledTaskRun()
        {
            var intervalString = _intervalOrOffsetFromNow?.ToString() ??
                                 _scheduledTimeAsDateTimeOffset?.ToString() ?? _scheduledTime?.ToString() ?? _crontab;

            Console.WriteLine($"Running Scheduled Task with interval: {intervalString}");
        }

        private void ValidateCrontab(string crontab)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(crontab, new CrontabSchedule.ParseOptions(){IncludingSeconds = true});
                schedule.GetNextOccurrence(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new Exception("Crontab is invalid. See the inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Used to prevent overlap between tasks added at different times but sharing a name.
        /// </summary>
        public void ClearLastRunTime()
        {
            _backend.DeleteKey(LastRunValueKey);
        }
    }
}