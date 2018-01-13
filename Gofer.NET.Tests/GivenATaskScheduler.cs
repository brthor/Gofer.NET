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

        private void AssertTaskSchedulerWritesSemaphoreOnlyOnce(Action<string, TaskScheduler> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup: false);

            configureSchedulerAction(semaphoreFile, taskScheduler);
            taskScheduler.Tick();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran ONLY Once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText);

            taskScheduler.FlushBackupStorage();
        }

        private void AssertTaskSchedulerWritesSemaphoreTwice(Action<string, TaskScheduler> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskScheduler = new TaskScheduler(TaskQueue.Redis(TaskQueueTestFixture.RedisConnectionString), restoreFromBackup: false);

            configureSchedulerAction(semaphoreFile, taskScheduler);
            taskScheduler.Tick();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran Twice
            Thread.Sleep(((IntervalSeconds) * 1000) + 10);
            taskScheduler.Tick();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);

            taskScheduler.FlushBackupStorage();
        }

        [Fact]
        public void ItExecutesAScheduledTaskAtTheSpecifiedOffsetOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public void ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOffsetOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                    new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), TimeSpan.FromHours(0)), RandomTaskName);
            };

            AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public void ItExecutesAScheduledTaskAtTheSpecifiedDateTimeOnlyOnce()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                    DateTime.UtcNow + TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            AssertTaskSchedulerWritesSemaphoreOnlyOnce(configureSchedulerAction);
        }

        [Fact]
        public void ItExecutesARecurringTaskAtTheSpecifiedInterval()
        {
            Action<string, TaskScheduler> configureSchedulerAction = (semaphoreFile, taskScheduler) =>
            {
                taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                    TimeSpan.FromSeconds(IntervalSeconds), RandomTaskName);
            };

            AssertTaskSchedulerWritesSemaphoreTwice(configureSchedulerAction);
        }

        [Theory]
        [InlineData("scheduled")]
        [InlineData("recurring")]
        public void ItExecutesTasksOnlyOnceWhenUsingMultipleConsumers(string taskType)
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
                    taskScheduler.AddScheduledTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                        TimeSpan.FromSeconds(intervalSeconds), RandomTaskName);
                }

                if (taskType == "recurring")
                {
                    taskScheduler.AddRecurringTask(() => TaskQueueTestFixture.WriteSempaphore(semaphoreFile),
                        TimeSpan.FromSeconds(intervalSeconds), RandomTaskName);
                }
            }
            
            Thread.Sleep(((intervalSeconds) * 1000) + 1000);

            // Ran only once
            Task.WaitAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(() => taskScheduler.Tick())).ToArray(), 1000);
            
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            
            
            // Ran only twice (or once if scheduled)
            Thread.Sleep(((intervalSeconds) * 1000) + 1000);
            Task.WaitAll(
                taskSchedulers.Select(
                    taskScheduler =>
                        Task.Run(() => taskScheduler.Tick())).ToArray(), 1000);

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