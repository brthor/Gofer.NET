using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gofer.NET.Tests
{
    public class GivenATaskClient
    {
        [Fact]
        public async Task ItContinuesListeningWhenATaskThrowsAnException()
        {
            var waitTime = 5000;
            
            var taskClient = new TaskClient(
                TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString, nameof(ItContinuesListeningWhenATaskThrowsAnException)),
                restoreScheduleFromBackup:false);
            var semaphoreFile = Path.GetTempFileName();
            
            await taskClient.TaskQueue.Enqueue(() => Throw());
            await taskClient.TaskQueue.Enqueue(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile));

            var task = Task.Run(async () => await taskClient.Listen());
            await Task.Delay(waitTime);

            taskClient.CancelListen();
            await task;
            
            TaskQueueTestFixture.EnsureSemaphore(semaphoreFile);
        }

        public static void Throw()
        {
            throw new Exception();
        }
        
    }
}