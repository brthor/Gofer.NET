using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Thor.Tasks
{
    public class TaskClient
    {
        public static readonly object locker = new object();
        
        private const int PollDelay = 500;

        private bool IsCanceled { get; set; }
        
        public TaskQueue TaskQueue { get; }
        public TaskScheduler TaskScheduler { get; }

        public TaskClient(TaskQueue taskQueue)
        {
            TaskQueue = taskQueue;
            TaskScheduler = new TaskScheduler(TaskQueue);
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
                    LogTask(info);

                    try
                    {
                        info.ExecuteTask();
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
            var logMessage = $"Task Failed: {info.AssemblyName}.{info.MethodName} \n" +
                             $"Error: {exception.Message}";
            
            Console.WriteLine(logMessage);
        }

        private void LogTask(TaskInfo info)
        {
            var logMessage = $"Task Received: {info.AssemblyName}.{info.MethodName}";
                
            Console.WriteLine(logMessage);
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