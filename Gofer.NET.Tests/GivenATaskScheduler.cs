using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Gofer.NET.Tests
{
    public class GivenATaskScheduler
    {
        [Fact]
        public void ItExecutesTasksAtTheSpecifiedInterval()
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            
            var intervalSeconds = 2;
            
            var taskScheduler = new TaskScheduler(TaskQueue.Redis("localhost:6379"));
            taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                TimeSpan.FromSeconds(intervalSeconds), "test");
            taskScheduler.Tick();

            File.Exists(semaphoreFile).Should().Be(false);
            
            // Confirm Scheduled Task Ran once
            Thread.Sleep(((intervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            
            // Confirm Ran TWICE
            Thread.Sleep(((intervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);
        }

        [Fact]
        public void ItExecutesTasksOnlyOnceWhenUsingMultipleConsumers()
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            File.Create(semaphoreFile).Close();
            
            var intervalSeconds = 5;

            var taskSchedulers = new[]
            {
                new TaskScheduler(TaskQueue.Redis("localhost:6379")),
                new TaskScheduler(TaskQueue.Redis("localhost:6379")),
                new TaskScheduler(TaskQueue.Redis("localhost:6379")),
                new TaskScheduler(TaskQueue.Redis("localhost:6379"))
            };

            foreach (var taskScheduler in taskSchedulers)
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                    TimeSpan.FromSeconds(intervalSeconds), "test");
            }
            
            Thread.Sleep(((intervalSeconds) * 1000) + 1000);

            // Ran only once
            Task.WaitAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(() => taskScheduler.Tick())).ToArray(), 1000);
            
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            
            
            // Ran only twice
            Thread.Sleep(((intervalSeconds) * 1000) + 1000);
            Task.WaitAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(() => taskScheduler.Tick())).ToArray(), 1000);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);
        }
    }
}