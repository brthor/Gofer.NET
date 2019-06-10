using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NCrontab;
using Newtonsoft.Json.Bson;
using Xunit;

namespace Gofer.NET.Tests
{
    public class TaskSchedulerTestHelpers
    {
        internal static void SynchronizeToCrontabNextStart(string crontab)
        {
                var crontabSchedule = CrontabSchedule.Parse(crontab, new CrontabSchedule.ParseOptions() { IncludingSeconds = true });
                var nextOccurence = crontabSchedule.GetNextOccurrence(DateTime.UtcNow);

                var synchronizationFactor = nextOccurence - DateTime.UtcNow;

                Thread.Sleep((int)synchronizationFactor.TotalMilliseconds);
        }

        internal static async Task AssertTaskSchedulerDoesNotWriteSemaphore(
            int intervalSeconds, 
            Func<string, TaskScheduler, Task> configureSchedulerAction,
            Func<TaskScheduler, Task> cancelTaskAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            await configureSchedulerAction(semaphoreFile, taskScheduler);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();

            File.Exists(semaphoreFile).Should().Be(false);

            await cancelTaskAction(taskScheduler);

            // Confirm Task did not run
            Thread.Sleep(((intervalSeconds) * 1000) + 100);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm AGAIN Task did not run
            Thread.Sleep(((intervalSeconds) * 1000) + 100);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(false);
        }

        internal static async Task AssertTaskSchedulerWritesSemaphoreOnlyOnce(
            int intervalSeconds, 
            Func<string, TaskScheduler, Task> configureSchedulerAction,
            Func<string, TaskScheduler, Task> cancelRecurringTaskAction=null)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            await configureSchedulerAction(semaphoreFile, taskScheduler);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((intervalSeconds) * 1000) + 100);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            if (cancelRecurringTaskAction != null) {
                await cancelRecurringTaskAction(semaphoreFile, taskScheduler);
            }

            // Confirm Ran ONLY Once
            Thread.Sleep(((intervalSeconds) * 1000) + 100);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText);
        }

        internal static async Task AssertTaskSchedulerWritesSemaphoreTwice(int intervalSeconds, Func<string, TaskScheduler, Task> configureSchedulerAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            
            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            await configureSchedulerAction(semaphoreFile, taskScheduler);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep(((intervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Recurring Task does not run again before interval
            await taskScheduler.Tick(forceRunPromotion: true);
            await taskScheduler.Tick(forceRunPromotion: true);
            await taskQueue.ExecuteNext();
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran Twice
            Thread.Sleep(((intervalSeconds) * 1000) + 10);
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);
        }

        internal static async Task AssertTaskSchedulerWritesSemaphoreTwiceAfterReconfiguringInterval(
            int intervalSeconds, 
            Func<string, TaskScheduler, Task> configureSchedulerAction,
            Func<string, TaskScheduler, Task> reconfigureRecurringTaskIntervalAction)
        {
            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            
            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            await configureSchedulerAction(semaphoreFile, taskScheduler);

            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();

            File.Exists(semaphoreFile).Should().Be(false);

            // Confirm Scheduled Task Ran once
            Thread.Sleep((((intervalSeconds) * 1000) + 10) );
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm does not run before interval elapsed
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Reconfigure interval to 2 * IntervalSeconds
            await reconfigureRecurringTaskIntervalAction(semaphoreFile, taskScheduler);

            // Confirm does not run before interval elapsed
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Sleep previous interval & confirm does not run before interval elapsed
            Thread.Sleep((((intervalSeconds) * 1000) + 10) ); 
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);

            // Confirm Ran Twice
            Thread.Sleep((((intervalSeconds) * 1000) + 10) );
            await taskScheduler.Tick();
            await taskQueue.ExecuteNext();
            File.Exists(semaphoreFile).Should().Be(true);
            File.ReadAllText(semaphoreFile).Should()
                .Be(TaskQueueTestFixture.SemaphoreText + TaskQueueTestFixture.SemaphoreText);
        }
    }
}