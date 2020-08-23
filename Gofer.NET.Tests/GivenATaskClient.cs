using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Xunit;

namespace Gofer.NET.Tests
{
    public class GivenATaskClient
    {
        [Fact]
        public async Task ItContinuesListeningWhenATaskThrowsAnException()
        {
            var waitTime = 5000;

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskClient = new TaskClient(taskQueue);
            var semaphoreFile = Path.GetTempFileName();

            await taskClient.TaskQueue.Enqueue(() => Throw());
            await taskClient.TaskQueue.Enqueue(() => TaskQueueTestFixture.WriteSemaphore(semaphoreFile));

            var task = Task.Run(async () => await taskClient.Listen());
            await Task.Delay(waitTime);

            taskClient.CancelListen();
            await task;

            TaskQueueTestFixture.EnsureSemaphore(semaphoreFile);
        }

        [Fact]
        public async Task ItStopsOnCancellation()
        {
            var semaphoreFile = Path.GetTempFileName();
            var timeout = TimeSpan.FromMinutes(1);

            var waitTime = TimeSpan.FromSeconds(2);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskClient = new TaskClient(taskQueue);
            var cancellation = new CancellationTokenSource();

            await taskClient.TaskQueue.Enqueue(() =>
                TaskQueueTestFixture.WaitForTaskClientCancellationAndWriteSemaphore(
                    semaphoreFile,
                    timeout));

            var task = Task.Run(async () => await taskClient.Listen(cancellation.Token), CancellationToken.None);
            await Task.Delay(waitTime, CancellationToken.None);
            cancellation.Cancel();
            await Task.Delay(waitTime, CancellationToken.None);
            await task;

            TaskQueueTestFixture.EnsureSemaphore(semaphoreFile);
        }

        [Fact]
        public async Task ItDoesNotDelayScheduledTaskPromotionWhenRunningLongTasks()
        {
            var waitTime = 4000;

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskClient = new TaskClient(taskQueue);

            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            File.Exists(semaphoreFile).Should().BeFalse();

            await taskClient.TaskQueue.Enqueue(() => Wait(waitTime));

            await taskClient.TaskScheduler.AddScheduledTask(
                () => TaskQueueTestFixture.WriteSemaphore(semaphoreFile),
                TimeSpan.FromMilliseconds(waitTime / 4));

            var task = Task.Run(async () => await taskClient.Listen());

            await Task.Delay(waitTime / 2);

            // Ensure we did not run the scheduled task
            File.Exists(semaphoreFile).Should().BeFalse();

            var dequeuedScheduledTask = await taskQueue.Dequeue();

            File.Exists(semaphoreFile).Should().BeFalse();
            dequeuedScheduledTask.Should().NotBeNull();
            dequeuedScheduledTask.MethodName.Should().Be(nameof(TaskQueueTestFixture.WriteSemaphore));

            taskClient.CancelListen();
            await task;
        }

        [Fact]
        public async Task ItExecutesImmediateAndScheduledTasksInOrder()
        {
            const int immediateTasks = 5;
            const int scheduledTasks = 20;

            const int scheduledTasksStart = -100;
            const int scheduledTasksIncrement = 50;

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskClient = new TaskClient(taskQueue);

            var semaphoreFile = Path.GetTempFileName();
            File.Delete(semaphoreFile);
            File.Exists(semaphoreFile).Should().BeFalse();

            for (var i = 0; i < immediateTasks; ++i)
            {
                await taskClient.TaskQueue.Enqueue(() =>
                    TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, (i + 1).ToString()));
            }

            for (var i = 0; i < scheduledTasks; ++i)
            {
                await taskClient.TaskScheduler.AddScheduledTask(
                    () => TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, (immediateTasks + i + 1).ToString()),
                    TimeSpan.FromMilliseconds(scheduledTasksStart + (scheduledTasksIncrement * i)));
            }

            var task = Task.Run(async () => await taskClient.Listen());
            Thread.Sleep(scheduledTasks * scheduledTasksIncrement + 2000);

            var expectedFileContents = Enumerable
                .Range(1, immediateTasks + scheduledTasks)
                .Aggregate("", (acc, v) => acc + v.ToString());

            File.Exists(semaphoreFile).Should().BeTrue();
            File.ReadAllText(semaphoreFile).Should().Be(expectedFileContents);

            taskClient.CancelListen();
            await task;
        }

        public static void Throw()
        {
            throw new Exception();
        }

        public static async Task Wait(int time)
        {
            // REVIEW: The ItDoesNotDelayScheduledTaskPromotionWhenRunningLongTasks 
            //     test fails when using Thread.Sleep rather than Task.Delay here.
            // Thread.Sleep(time);

            await Task.Delay(time);
        }
    }
}