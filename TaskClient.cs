using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Gofer.NET.Errors;
using Gofer.NET.Utils;
using Newtonsoft.Json;

namespace Gofer.NET
{
    public class TaskClient
    {
        public static readonly object locker = new object();
        
        private const int PollDelay = 500;

        private bool IsCanceled { get; set; }
        
        public TaskQueue TaskQueue { get; }
        public TaskScheduler TaskScheduler { get; }

        public TaskClient(TaskQueue taskQueue, bool restoreScheduleFromBackup=true)
        {
            TaskQueue = taskQueue;
            TaskScheduler = new TaskScheduler(TaskQueue, restoreFromBackup: restoreScheduleFromBackup);
            IsCanceled = false;
        }

        public void Listen()
        {
            while (true)
            {
                if (IsCanceled)
                {
                    return;
                }
                
                // Tick the Task Scheduler
                TaskScheduler.Tick();
                
                // Execute Any Queued Tasks
                var (json, info) = TaskQueue.SafeDequeue();
                if (info != null)
                {
                    LogTaskStarted(info);

                    try
                    {
                        var now = DateTime.Now;
                        
                        info.ExecuteTask();
                        
                        var completionSeconds = (DateTime.Now - now).TotalSeconds;
                        LogTaskFinished(info, completionSeconds);
                    }
                    catch (Exception e)
                    {
                        LogTaskException(info, e);
                    }
                    finally
                    {
                        TaskQueue.Backend.RemoveBackup(json);
                    }
                }
                
                // Restore any expired backup tasks
                TaskQueue.RestoreExpiredBackupTasks();
                
                Thread.Sleep(PollDelay);
            }
        }

        private void LogTaskException(TaskInfo info, Exception exception)
        {
            var logMessage = Messages.TaskThrewException(info);
            Trace.Exception(logMessage, exception);
        }

        private void LogTaskStarted(TaskInfo info)
        {
            var logMessage = Messages.TaskStarted(info);
            Trace.Info(logMessage);
        }
        
        private void LogTaskFinished(TaskInfo info, double completionSeconds)
        {
            var logMessage = Messages.TaskFinished(info, completionSeconds);
            Trace.Info(logMessage);
        }

        public void CancelListen()
        {
            lock (locker)
            {
                IsCanceled = true;
            }
        }
    }
}