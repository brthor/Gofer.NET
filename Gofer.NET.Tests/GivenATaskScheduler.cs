using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Bson;
using Xunit;

namespace Gofer.NET.Tests
{
    public class GivenATaskScheduler
    {
        private int IntervalSeconds => 2;
        private string RandomTaskName = Guid.NewGuid().ToString();

        private async Task AssertTaskSchedulerWritesSemaphoreOnlyOnce(Action<string, TaskScheduler> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup: false);

            configureSchedulerAction(semaphoreFile, taskScheduler);
            await taskScheduler.Tick();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran ONLY Once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText);

            taskScheduler.FlushBackupStorage();
        }

        private async Task AssertTaskSchedulerWritesSemaphoreTwice(Action<string, TaskScheduler> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup: false);

            configureSchedulerAction(semaphoreFile, taskScheduler);
            await taskScheduler.Tick();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran Twice
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);

            taskScheduler.FlushBackupStorage();
        }

        [Fact]
        public async Task ItExecutesAScheduledTaskAtTheSpecifiedOffsetOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOffsetOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), TimeSpan.FromHours(0)), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesARecurringTaskAtTheSpecifiedInterval()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreTwice(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesARecurringTaskAtTheSpecifiedCrontabInterval()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    $"*/{IntervalSeconds} * * * * *", RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreTwice(configureSchedulerAction);
        }

        [Theory]
        [InlineData("scheduled")]
        [InlineData("recurring")]
        public async Task ItExecutesTasksOnlyOnceWhenUsingMultipleConsumers(string taskType)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            File.Create(semaphoreFile).Close();
            
            var intervalSeconds = 5;

            var taskSchedulers = new[]
            {
                new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup:false),
                new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup:false),
                new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup:false),
                new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup:false)
            };

            foreach (var taskScheduler in taskSchedulers)
            {
                if (taskType == "scheduled")
                {
                    taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                        TimeSpan.FromSeconds(intervalSeconds), RandomTaskName);
                }

                if (taskType == "recurring")
                {
                    taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                        TimeSpan.FromSeconds(intervalSeconds), RandomTaskName);
                }
            }
            
            Thread.Sleep(((intervalSeconds) * 1000) + 50);

            // Ran only once
            await Task.WhenAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(async () => await taskScheduler.Tick())));
            
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            
            
            // Ran only twice (or once if scheduled)
            Thread.Sleep(((intervalSeconds) * 1000) + 50);
            await Task.WhenAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(async () => await taskScheduler.Tick())));

            if (taskType == "recurring")
            {
                File.ReadAllText(semaphoreFile).Should()
                    .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);
            }
            if (taskType == "scheduled")
            {
                File.ReadAllText(semaphoreFile).Should()
                    .Be(TaskQueueTestFixture.SemaphoreText);
            }

            taskSchedulers[0].FlushBackupStorage();
        }
    }
}