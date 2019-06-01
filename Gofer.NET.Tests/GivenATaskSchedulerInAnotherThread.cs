using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Bson;
using Xunit;
using Xunit.Abstractions;
using Gofer.NET.Utils;

namespace Gofer.NET.Tests
{
    public class GivenATaskSchedulerInAnotherThread
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GivenATaskSchedulerInAnotherThread(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task ItExecutesScheduledTasksOnlyOnce()
        {
            const int numberOfTasks = 1000;
            const int tasksPerTimeInterval = 25;
            const int timeInterval = 1000;
            const int numberOfSchedulerThreads = 4;
            TimeSpan promotionFrequency = TimeSpan.FromMilliseconds(100);
            ThreadPool.SetMinThreads(numberOfSchedulerThreads * 4, 200);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue, promotionFrequency);

            var canceled = false;
            var taskSchedulerTasks = new List<Task>();
            var taskRunnerTasks = new List<Task>();
            
            for (var s=0; s<numberOfSchedulerThreads; ++s)
            {
                taskSchedulerTasks.Add(Task.Run(async () => {
                    var inThreadTaskScheduler = new TaskScheduler(taskQueue);
                    while (!canceled)
                    {
                        await inThreadTaskScheduler.Tick(forceRunPromotion: true);
                    }
                }));

                taskRunnerTasks.Add(Task.Run(async () => {
                    while (!canceled)
                    {
                        await taskQueue.ExecuteNext();
                    }
                }));
            }

            var tasks = new List<(ScheduledTask, string, string)>();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (var i=0; i<numberOfTasks/tasksPerTimeInterval; ++i)
            {
                for (var k=0; k<tasksPerTimeInterval; ++k)
                {
                    var semaphoreFile = Path.GetTempFileName();
                    File.Delete(semaphoreFile);
                    semaphoreFile = semaphoreFile + Guid.NewGuid().ToString();

                    var semaphoreValue = Guid.NewGuid().ToString();

                    var scheduledTask = await taskScheduler.AddScheduledTask(
                        () => TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, semaphoreValue), 
                        TimeSpan.FromMilliseconds((i+1) * timeInterval));
                    
                    tasks.Add((scheduledTask, semaphoreFile, semaphoreValue));
                }
            }

            _testOutputHelper.WriteLine(sw.ElapsedMilliseconds.ToString());
            
            var precisionAllowanceFactorMs = promotionFrequency.TotalMilliseconds + 10;

            long now;
            for (var i=0; i<numberOfTasks/tasksPerTimeInterval; ++i)
            {
                Thread.Sleep(timeInterval);

                // EACH TASK, 3 Cases
                // CASE 1: Scheduled Time should have run, allowing for precision loss
                // CASE 2: Scheduled Time should definitely not have run
                // CASE 3: Scheduled Time is past, but within precision allowance (may have run or not)
                foreach (var task in tasks)
                {
                    now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    if (task.Item1.ScheduledUnixTimeMilliseconds <= (now - precisionAllowanceFactorMs))
                    {
                        File.Exists(task.Item2).Should().BeTrue();
                        File.ReadAllText(task.Item2).Should().Be(task.Item3);
                    }
                    else if (task.Item1.ScheduledUnixTimeMilliseconds > now)
                    {
                        File.Exists(task.Item2).Should().BeFalse();
                    }
                }
            }

            Thread.Sleep((int) precisionAllowanceFactorMs);

            foreach (var task in tasks)
            {
                File.Exists(task.Item2).Should().BeTrue();
                File.ReadAllText(task.Item2).Should().Be(task.Item3);
            }

            canceled = true;
            await Task.WhenAll(taskSchedulerTasks);
            await Task.WhenAll(taskRunnerTasks);
        }

        [Fact]
        public async Task ItExecutesTimespanRecurringTasksAccordingToTheirSchedules()
        {
            const int numberOfTasks = 10;
            const int timeInterval = 1000;
            const int testIntervals = 10;
            const int numberOfSchedulerThreads = 4;
            const int precisionAllowanceFactorMs = 500;

            ThreadPool.SetMinThreads(numberOfSchedulerThreads * 4, 200);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            var canceled = false;
            var taskSchedulerTasks = new List<Task>();
            var taskRunnerTasks = new List<Task>();
            
            for (var s=0; s<numberOfSchedulerThreads; ++s)
            {
                taskSchedulerTasks.Add(Task.Run(async () => {
                    var inThreadTaskScheduler = new TaskScheduler(taskQueue);
                    while (!canceled)
                    {
                        await inThreadTaskScheduler.Tick(forceRunPromotion: true);
                    }
                }));

                taskRunnerTasks.Add(Task.Run(async () => {
                    while (!canceled)
                    {
                        await taskQueue.ExecuteNext();
                    }
                }));
            }

            var tasks = new List<(RecurringTask, string, string)>();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (var i=0; i<numberOfTasks; ++i)
            {
                var semaphoreFile = Path.GetTempFileName();
                File.Delete(semaphoreFile);
                semaphoreFile = semaphoreFile + Guid.NewGuid().ToString();

                var semaphoreValue = Guid.NewGuid().ToString();

                var recurringTask = await taskScheduler.AddRecurringTask(
                    () => TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, semaphoreValue), 
                    TimeSpan.FromMilliseconds(timeInterval), 
                    Guid.NewGuid().ToString());
                
                tasks.Add((recurringTask, semaphoreFile, semaphoreValue));
            }

            _testOutputHelper.WriteLine("Test Add: " + sw.ElapsedMilliseconds.ToString());

            Thread.Sleep(110);

            for (var i=0; i<testIntervals; ++i)
            {
                Thread.Sleep(timeInterval);

                foreach (var task in tasks)
                {
                    var diff = (DateTime.UtcNow.ToUnixTimeMilliseconds() 
                            - task.Item1.StartTime.ToUnixTimeMilliseconds());
                    var elapsedIntervalsMax = 
                        diff / timeInterval;

                    var elapsedIntervalsMin = (diff % timeInterval) <= precisionAllowanceFactorMs 
                        ? elapsedIntervalsMax - 1
                        : elapsedIntervalsMax;

                    _testOutputHelper.WriteLine(diff.ToString());

                    elapsedIntervalsMax.Should().BeGreaterThan(i);
                    
                    var expectedStringMax = "";
                    for (var e=0; e < elapsedIntervalsMax; ++e)
                    {
                        expectedStringMax += task.Item3;
                    }

                    var expectedStringMin = "";
                    for (var e=0; e < elapsedIntervalsMin; ++e)
                    {
                        expectedStringMin += task.Item3;
                    }

                    File.Exists(task.Item2).Should().BeTrue(elapsedIntervalsMax.ToString());

                    var fileText = File.ReadAllText(task.Item2);
                    if (fileText != expectedStringMax)
                    {
                        fileText.Should().Be(expectedStringMin);
                    }
                }
            }

            canceled = true;
            await Task.WhenAll(taskSchedulerTasks);
            await Task.WhenAll(taskRunnerTasks);
        }

        [Fact]
        public async Task ItExecutesCrontabRecurringTasksAccordingToTheirSchedules()
        {
            const int numberOfTasks = 10;
            const int timeInterval = 1000;
            const int testIntervals = 10;
            const int numberOfSchedulerThreads = 4;
            const int precisionAllowanceFactorMs = 800;

            ThreadPool.SetMinThreads(numberOfSchedulerThreads * 4, 200);

            var taskQueue = TaskQueueTestFixture.UniqueRedisTaskQueue();
            var taskScheduler = new TaskScheduler(taskQueue);

            var canceled = false;
            var taskSchedulerTasks = new List<Task>();
            var taskRunnerTasks = new List<Task>();
            
            for (var s=0; s<numberOfSchedulerThreads; ++s)
            {
                taskSchedulerTasks.Add(Task.Run(async () => {
                    var inThreadTaskScheduler = new TaskScheduler(taskQueue);
                    while (!canceled)
                    {
                        await inThreadTaskScheduler.Tick(forceRunPromotion: true);
                    }
                }));

                taskRunnerTasks.Add(Task.Run(async () => {
                    while (!canceled)
                    {
                        await taskQueue.ExecuteNext();
                    }
                }));
            }

            var tasks = new List<(RecurringTask, string, string)>();

            for (var i=0; i<numberOfTasks; ++i)
            {
                var semaphoreFile = Path.GetTempFileName();
                File.Delete(semaphoreFile);
                semaphoreFile = semaphoreFile + Guid.NewGuid().ToString();

                var semaphoreValue = Guid.NewGuid().ToString();

                var recurringTask = await taskScheduler.AddRecurringTask(
                    () => TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, semaphoreValue), 
                    $"*/{timeInterval / 1000} * * * * *", 
                    Guid.NewGuid().ToString());
                
                tasks.Add((recurringTask, semaphoreFile, semaphoreValue));
            }

            var synchronizationFactor = 
                DateTime.UtcNow.ToUnixTimeMilliseconds() - DateTime.UtcNow.ToUnixTimeSeconds() * 1000;
            Thread.Sleep((int) synchronizationFactor + 100);

            for (var i=0; i<testIntervals; ++i)
            {
                Thread.Sleep(timeInterval);

                foreach (var task in tasks)
                {
                    var diff = DateTime.UtcNow.ToUnixTimeMilliseconds() 
                            - (task.Item1.FirstRunTime.ToUnixTimeMilliseconds() - timeInterval);
                    var elapsedIntervalsMax = 
                        diff / timeInterval;

                    var elapsedIntervalsMin = (diff % timeInterval) <= precisionAllowanceFactorMs 
                        ? elapsedIntervalsMax - 1
                        : elapsedIntervalsMax;

                    _testOutputHelper.WriteLine(diff.ToString());

                    elapsedIntervalsMax.Should().BeGreaterThan(i);
                    
                    var expectedStringMax = "";
                    for (var e=0; e < elapsedIntervalsMax; ++e)
                    {
                        expectedStringMax += task.Item3;
                    }

                    var expectedStringMin = "";
                    for (var e=0; e < elapsedIntervalsMin; ++e)
                    {
                        expectedStringMin += task.Item3;
                    }

                    File.Exists(task.Item2).Should().BeTrue(elapsedIntervalsMax.ToString());

                    var fileText = File.ReadAllText(task.Item2);
                    if (fileText != expectedStringMax)
                    {
                        fileText.Should().Be(expectedStringMin);
                    }
                }
            }

            canceled = true;
            await Task.WhenAll(taskSchedulerTasks);
            await Task.WhenAll(taskRunnerTasks);
        }
    }
}