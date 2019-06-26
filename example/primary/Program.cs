using System;
using System.Threading.Tasks;
using Gofer.NET;

namespace primary
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var redisConnectionString = "127.0.0.1:6379";
            
            // Create a Task Client connected to Redis
            var taskClient = new TaskClient(TaskQueue.Redis(redisConnectionString));
            
            // Queue up a Sample Fire-And-Forget Job
            await taskClient.TaskQueue.Enqueue(() => WriteValue("Hello World!"));

            // Queue up a job to be run in 30 seconds
            await taskClient.TaskScheduler
                .AddScheduledTask(() => WriteValue("Scheduled Task!"), TimeSpan.FromSeconds(30));

            // Queue up a Recurring Job for every 10 seconds
            await taskClient.TaskScheduler
                .AddRecurringTask(() => WriteDate(), TimeSpan.FromSeconds(10), "my-recurring-job");
        }
        
        private static void WriteValue(object value)
        {
            Console.WriteLine(value.ToString());
        }

        private static void WriteDate()
        {
            Console.WriteLine(DateTime.UtcNow.ToString());
        }
    }
}
