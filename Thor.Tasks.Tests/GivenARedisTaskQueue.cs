using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Thor.Tasks.Tests
{
    public class GivenARedisTaskQueue
    {
        private const string TestRedisConnectionString = "localhost:6379";
        private const string TestQueueName = nameof(GivenARedisTaskQueue) + "::TestQueue";
        
        [Fact]
        public void ItEnqueuesAndReceivesDelegatesThatAreRunnable()
        {
            var semaphoreFile = Path.Combine(AppContext.BaseDirectory,
                nameof(ItEnqueuesAndReceivesDelegatesThatAreRunnable));
            
            File.Delete(semaphoreFile);
            
            var taskQueue = TaskQueue.Redis(TestRedisConnectionString, TestQueueName);
            
            taskQueue.Enqueue<string>(WriteCompletedSemaphore, new object[] {semaphoreFile});

            var dequeuedTaskInfo = taskQueue.Dequeue();
            dequeuedTaskInfo.ExecuteTask();

            var contents = File.ReadAllText(semaphoreFile);
            contents.Should().Be("Complete");
        }

        private static void WriteCompletedSemaphore(string semaphoreFile)
        {
            File.WriteAllText(semaphoreFile, "Complete");
        }
    }
}
