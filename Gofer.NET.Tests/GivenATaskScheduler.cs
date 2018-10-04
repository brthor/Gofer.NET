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

        private async Task AssertTaskSchedulerWritesSemaphoreOnlyOnce(Func<string, TaskScheduler, Task> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup: false);

            await configureSchedulerAction(semaphoreFile, taskScheduler);
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

        private async Task AssertTaskSchedulerWritesSemaphoreTwice(Func<string, TaskScheduler, Task> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), 
                restoreFromBackup: false);

            await configureSchedulerAction(semaphoreFile, taskScheduler);
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
            Func<string, TaskScheduler, Task> configureSchedulerAction = async (semaphoreFile, taskScheduler) =>
            {
                await taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOffsetOnlyOnce()
        {
            Func<string, TaskScheduler, Task> configureSchedulerAction = async (semaphoreFile, taskScheduler) =>
            {
                await taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), TimeSpan.FromHours(0)), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOnlyOnce()
        {
            Func<string, TaskScheduler, Task> configureSchedulerAction = async (semaphoreFile, taskScheduler) =>
            {
                await taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesARecurringTaskAtTheSpecifiedInterval()
        {
            Func<string, TaskScheduler, Task> configureSchedulerAction = async (semaphoreFile, taskScheduler) =>
            {
                await taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            await AssertTaskSchedulerWritesSemaphoreTwice(configureSchedulerAction);
        }

        [Fact]
        public async Task ItExecutesARecurringTaskAtTheSpecifiedCrontabInterval()
        {
            Func<string, TaskScheduler, Task> configureSchedulerAction = async (semaphoreFile, taskScheduler) =>
            {
                await taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
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
                    await taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                        TimeSpan.FromSeconds(intervalSeconds), RandomTaskName);
                }

                if (taskType == "recurring")
                {
                    await taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
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