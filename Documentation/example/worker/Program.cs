using System;
using System.Threading.Tasks;
using Gofer.NET;

namespace worker
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var redisConnectionString = "127.0.0.1:6379";
            
            // Create a Task Client connected to Redis
            var taskClient = new TaskClient(TaskQueue.Redis(redisConnectionString));

            Console.WriteLine("Listening for Jobs...");
            
            // Endlessly listen for jobs
            await taskClient.Listen();
        }
    }
}
