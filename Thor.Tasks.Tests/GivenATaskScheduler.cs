using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace Thor.Tasks.Tests
{
    public class GivenATaskScheduler
    {
        private static string SemaphoreFile =>  Path.Combine(AppContext.BaseDirectory,
            nameof(ItExecutesTasksAtTheSpecifiedInterval));
        
        [Fact]
        public void ItExecutesTasksAtTheSpecifiedInterval()
        {
            File.Delete(SemaphoreFile);
            
            var intervalSeconds = 5;
            
            var taskScheduler = new TaskScheduler(TaskQueue.Redis("localhost:6379"));
            taskScheduler.AddScheduledTask(WriteCompletedSemaphore, TimeSpan.FromSeconds(intervalSeconds), "test");
            taskScheduler.Tick();

            File.Exists(SemaphoreFile).Should().Be(false);
            
            Thread.Sleep((intervalSeconds + 1) * 1000);
            
            taskScheduler.Tick();
            File.Exists(SemaphoreFile).Should().Be(true);
            File.ReadAllText(SemaphoreFile).Should().Be("Complete");
        }
        
        private static void WriteCompletedSemaphore()
        {
            File.WriteAllText(SemaphoreFile, "Complete");
        }
    }
}