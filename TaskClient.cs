using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Thor.Tasks
{
    public class TaskClient
    {
        private const int PollDelay = 5000;
        
        public TaskQueue TaskQueue { get; }
        public TaskScheduler TaskScheduler { get; }

        public TaskClient(TaskQueue taskQueue)
        {
            TaskQueue = taskQueue;
            TaskScheduler = new TaskScheduler(TaskQueue);
        }

        public void Listen()
        {
            while (true)
            {
                // Tick the Task Scheduler
                TaskScheduler.Tick();
                
                // Execute Any Queued Tasks
                var info = TaskQueue.Dequeue();
                if (info != null)
                {
                    LogTask(info);
                    info.ExecuteTask();
                }
                
                Thread.Sleep(PollDelay);
            }
        }

        private void LogTask(TaskInfo info)
        {
            var logMessage = $"Task Received: {info.AssemblyName}.{info.MethodName}";
                
            Console.WriteLine(logMessage);
        }
    }
}