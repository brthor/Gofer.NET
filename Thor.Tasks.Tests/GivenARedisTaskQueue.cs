using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Thor.Tasks.Tests
{
    public class GivenARedisTaskQueue
    {
        [Fact]
        public void ItEnqueuesAndReceivesDelegatesThatAreRunnable()
        {
            var testFixture = new TaskQueueTestFixture(nameof(ItEnqueuesAndReceivesDelegatesThatAreRunnable));
            
            testFixture.EnsureSemaphoreDoesntExist();
            testFixture.PushPopExecuteWriteSemaphore();
            testFixture.EnsureSemaphore();
        }

        [Fact]
        public async Task ItsTasksAreConsumedOnlyOnceByMultipleConsumers()
        {
            var numberOfJobs = 4;
            
            var sharedTaskQueueName = nameof(ItsTasksAreConsumedOnlyOnceByMultipleConsumers);
            var consumers = new[]
            {
                new TaskQueueTestFixture(sharedTaskQueueName),
                new TaskQueueTestFixture(sharedTaskQueueName)
            };

            var semaphoreFiles = new List<string>();
            for(int i=0;i < numberOfJobs;++i)
            {
                var path = Path.GetTempFileName();
                File.Delete(path);
                semaphoreFiles.Add(path);
                
                var sharedTaskQueue = consumers[0].TaskQueue;
                sharedTaskQueue.Enqueue<string>(TaskQueueTestFixture.WriteSempaphore, new object[] {path});
            }

            var tasks = new List<Task>();

            for (int i = 0; i < numberOfJobs; i += consumers.Length)
            {
                foreach (var consumer in consumers)
                {
                    var task = Task.Run(() => consumer.TaskQueue.ExecuteNext());
                    tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }

            foreach (var semaphoreFile in semaphoreFiles)
            {
                File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            }
        }
    }
}
