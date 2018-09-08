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
        private static readonly object Locker = new object();
        
        private const int PollDelay = 100;

        private bool IsCanceled { get; set; }
        
        public TaskQueue TaskQueue { get; }
        public Action<Exception> OnError { get; }
        public TaskScheduler TaskScheduler { get; }

        public TaskClient(
            TaskQueue taskQueue, 
            bool restoreScheduleFromBackup=false,
            Action<Exception> onError=null)
        {
            TaskQueue = taskQueue;
            OnError = onError;
            TaskScheduler = new TaskScheduler(TaskQueue, restoreFromBackup: restoreScheduleFromBackup);
            IsCanceled = false;
            
        }

        public async Task Listen()
        {
            while (true)
            {
                lock (Locker)
                {
                    if (IsCanceled)
                    {
                        return;
                    }
                }
                
                // Tick the Task Scheduler
                await TaskScheduler.Tick();
                
                // Execute Any Queued Tasks
                var (json, info) = await TaskQueue.SafeDequeue();
                if (info != null)
                {
                    LogTaskStarted(info);

                    try
                    {
                        var now = DateTime.Now;
                        
                        await info.ExecuteTask();
                        
                        var completionSeconds = (DateTime.Now - now).TotalSeconds;
                        LogTaskFinished(info, completionSeconds);
                    }
                    catch (Exception e)
                    {
                        LogTaskException(info, e);
                    }
                    finally
                    {
//                        TaskQueue.Backend.RemoveBackup(json);
                    }
                }
                
                // Restore any expired backup tasks
//                TaskQueue.RestoreExpiredBackupTasks();
                
                Thread.Sleep(PollDelay);
            }
        }

        private void LogTaskException(TaskInfo info, Exception exception)
        {
            OnError?.Invoke(exception);

            var logMessage = Messages.TaskThrewException(info);
            ThreadSafeColoredConsole.Exception(logMessage, exception);
        }

        private void LogTaskStarted(TaskInfo info)
        {
            var logMessage = Messages.TaskStarted(info);
            ThreadSafeColoredConsole.Info(logMessage);
        }
        
        private void LogTaskFinished(TaskInfo info, double completionSeconds)
        {
            var logMessage = Messages.TaskFinished(info, completionSeconds);
            ThreadSafeColoredConsole.Info(logMessage);
        }

        public void CancelListen()
        {
            lock (Locker)
            {
                IsCanceled = true;
            }
        }
    }
}