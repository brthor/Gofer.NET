using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

namespace Gofer.NET.Tests
{
    public class TaskQueueTestFixture
    {
        private static readonly ReaderWriterLock Locker = new ReaderWriterLock();

        public static string SemaphoreText => "completed";

        public TaskQueue TaskQueue { get; }

        public static string RedisConnectionString => "localhost:6379";

        private readonly string _semaphoreFile;

        public static TaskQueue UniqueRedisTaskQueue(string prefix = null)
        {
            var taskQueueName = $"{prefix ?? nameof(TaskQueueTestFixture)}::{Guid.NewGuid().ToString()}";
            return TaskQueue.Redis(RedisConnectionString, taskQueueName);
        }

        public TaskQueueTestFixture(string uniqueId, TaskQueue taskQueue = null)
        {
            _semaphoreFile = Path.Combine(AppContext.BaseDirectory, uniqueId, Path.GetTempFileName());

            var testQueueName = uniqueId + "::TestQueue";
            TaskQueue = taskQueue ?? TaskQueueTestFixture.UniqueRedisTaskQueue(uniqueId);

            // Clear out the queue
            while (TaskQueue.Dequeue().Result != null) { }
        }

        public async Task PushPopExecuteWriteSemaphore()
        {
            await TaskQueue.Enqueue(() => WriteSemaphore(_semaphoreFile));
            var dequeuedTaskInfo = await TaskQueue.Dequeue();
            await dequeuedTaskInfo.ExecuteTask();
        }

        public void EnsureSemaphoreDoesntExist()
        {
            File.Delete(_semaphoreFile);
            File.Exists(_semaphoreFile).Should().Be(false);
        }

        public void EnsureSemaphore()
        {
            EnsureSemaphore(_semaphoreFile);
        }

        public static void EnsureSemaphore(string semaphoreFile)
        {
            try
            {
                Locker.AcquireReaderLock(30000);
                File.ReadAllText(semaphoreFile).Should().Be(SemaphoreText);
            }
            finally
            {
                Locker.ReleaseReaderLock();
            }
        }

        public static void WriteSemaphore(string semaphoreFile)
        {
            WriteSemaphoreValue(semaphoreFile, SemaphoreText);
        }

        public static async Task WaitForTaskClientCancellationAndWriteSemaphore(string semaphoreFile)
        {
            var token = TaskClient.GetListenCancellation();
            if (!token.CanBeCanceled)
                throw new InvalidOperationException("This method must be called from a task client callback");
            try
            {
                await Task.Delay(-1, token);
            }
            catch (OperationCanceledException)
            {
            }
            WriteSemaphore(semaphoreFile);
        }

        public static void WriteSemaphoreValue(string semaphoreFile, object value)
        {
            try
            {
                Locker.AcquireWriterLock(30000);
                File.AppendAllText(semaphoreFile, value?.ToString() ?? "null");
            }
            finally
            {
                Locker.ReleaseWriterLock();
            }
        }
    }
}