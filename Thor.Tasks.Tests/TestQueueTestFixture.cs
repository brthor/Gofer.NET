using System;
using System.IO;
using System.Threading;
using FluentAssertions;

namespace Thor.Tasks.Tests
{
    public class TaskQueueTestFixture
    {
        public static string SemaphoreText => "completed";
        
        public TaskQueue TaskQueue { get; }
            
        public static string RedisConnectionString => "localhost:6379";

        private readonly string _semaphoreFile;

        public TaskQueueTestFixture(string uniqueId, TaskQueue taskQueue=null)
        {
            _semaphoreFile = Path.Combine(AppContext.BaseDirectory, uniqueId, Path.GetTempFileName());
            
            var testQueueName = uniqueId + "::TestQueue";
            TaskQueue = taskQueue ?? TaskQueue.Redis(RedisConnectionString, testQueueName);
            
            // Clear out the queue
            while(TaskQueue.Dequeue() != null) { }
        }

        public void PushPopExecuteWriteSemaphore()
        {
            TaskQueue.Enqueue<string>(WriteSempaphore, new object[] {_semaphoreFile});
            var dequeuedTaskInfo = TaskQueue.Dequeue();
            dequeuedTaskInfo.ExecuteTask();
        }

        public void EnsureSemaphoreDoesntExist()
        {
            File.Delete(_semaphoreFile);
            File.Exists(_semaphoreFile).Should().Be(false);
        }

        public void EnsureSemaphore()
        {
            File.ReadAllText(_semaphoreFile).Should().Be(SemaphoreText);
        }

        public static void WriteSempaphore(string semaphoreFile)
        {
            File.AppendAllText(semaphoreFile, SemaphoreText);
        }
    }
}