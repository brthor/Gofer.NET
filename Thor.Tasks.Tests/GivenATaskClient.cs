using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Thor.Tasks.Tests
{
    public class GivenATaskClient
    {
        [Fact]
        public void ItContinuesListeningWhenATaskThrowsAnException()
        {
            var waitTime = 5000;
            
            var taskClient = new TaskClient(TaskQueue.Redis("localhost:6379", nameof(ItContinuesListeningWhenATaskThrowsAnException)));
            var semaphoreFile = Path.GetTempFileName();
            
            taskClient.TaskQueue.Enqueue(Throw);
            taskClient.TaskQueue.Enqueue<string>(TaskQueueTestFixture.WriteSempaphore, new object[] {semaphoreFile});

            var task = Task.Run(() => taskClient.Listen());
            Thread.Sleep(waitTime);
x
            taskClient.CancelListen();
            task.Wait();
            
            TaskQueueTestFixture.EnsureSemaphore(semaphoreFile);
        }

        public static void Throw()
        {
            throw new Exception();
        }
        
    }
}