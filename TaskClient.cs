using System;
using System.Threading;
using System.Threading.Tasks;

using Gofer.NET.Utils;

namespace Gofer.NET
{
    public class TaskClient
    {
        private static readonly object Locker = new object();

        private const int PollDelay = 100;

        private bool IsCanceled { get; set; }

        public TaskQueue TaskQueue { get; }

        public Action<Exception> OnError { get; }

        public TaskScheduler TaskScheduler { get; }

        private Task TaskSchedulerThread { get; set; }

        private Task TaskRunnerThread { get; set; }

        private CancellationTokenSource ListenCancellationTokenSource { get; set; }

        public TaskClient(
            TaskQueue taskQueue,
            Action<Exception> onError = null)
        {
            TaskQueue = taskQueue;
            OnError = onError;
            TaskScheduler = new TaskScheduler(TaskQueue);
            IsCanceled = false;
        }

        public Task Listen()
        {
            return Listen(CancellationToken.None);
        }

        public async Task Listen(CancellationToken cancellation)
        {
            Start(cancellation);

            await Task.WhenAll(new[] {
                TaskRunnerThread,
                TaskSchedulerThread});
        }

        public CancellationTokenSource Start()
        {
            return Start(CancellationToken.None);
        }

        public CancellationTokenSource Start(CancellationToken cancellation)
        {
            if (TaskSchedulerThread != null || TaskRunnerThread != null)
            {
                throw new Exception("This TaskClient is already listening.");
            }


            ListenCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var token = ListenCancellationTokenSource.Token;

            TaskSchedulerThread = Task.Run(async () =>
            {
                var inThreadTaskScheduler = new TaskScheduler(TaskQueue);

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await inThreadTaskScheduler.Tick();
                }
            }, ListenCancellationTokenSource.Token);

            TaskRunnerThread = Task.Run(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await ExecuteQueuedTask(token);
                }
            }, ListenCancellationTokenSource.Token);

            return ListenCancellationTokenSource;
        }

        private async Task ExecuteQueuedTask(CancellationToken token)
        {
            var (json, info) = await TaskQueue.SafeDequeue();
            if (info != null)
            {
                LogTaskStarted(info);

                try
                {
                    var now = DateTime.Now;

                    await info.ExecuteTask(token);

                    var completionSeconds = (DateTime.Now - now).TotalSeconds;
                    LogTaskFinished(info, completionSeconds);
                }
                catch (Exception e)
                {
                    LogTaskException(info, e);
                }
            }
        }

        private void LogTaskException(TaskInfo info, Exception exception)
        {
            OnError?.Invoke(exception);

            var logMessage = Messages.TaskThrewException(info);
            ThreadSafeColoredConsole.Exception(logMessage, exception);
        }

        private void LogTaskStarted(TaskInfo info)
        {
            var logMessage = Messages.TaskStarted(info);
            ThreadSafeColoredConsole.Info(logMessage);
        }

        private void LogTaskFinished(TaskInfo info, double completionSeconds)
        {
            var logMessage = Messages.TaskFinished(info, completionSeconds);
            ThreadSafeColoredConsole.Info(logMessage);
        }

        public void CancelListen()
        {
            ListenCancellationTokenSource.Cancel();
        }
    }
}