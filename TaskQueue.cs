using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Thor.Tasks
{
    public partial class TaskQueue
    {
        public ITaskQueueBackend Backend { get; }
        public TaskQueueConfiguration Config { get; }
        
        public TaskQueue(ITaskQueueBackend backend, TaskQueueConfiguration config=null)
        {
            Backend = backend;
            Config = config ?? new TaskQueueConfiguration();
        }
        
        public void Enqueue<T>(Action<T> action, object[] args=null)
        {
            Enqueue(action.ToTaskInfo(args));
        }

        public void Enqueue(Action action, object[] args=null)
        {
            Enqueue(action.ToTaskInfo(args));
        }

        public void Enqueue(TaskInfo taskInfo)
        {
            var jsonString = JsonNetAdapter.Serialize(taskInfo);

            Backend.Enqueue(jsonString);
        }
        
        public void ExecuteNext()
        {
            var taskInfo = Dequeue();
            taskInfo.ExecuteTask();
        }

        public TaskInfo Dequeue()
        {
            var jsonString = Backend.Dequeue();
            if (jsonString == null)
            {
                return null;
            }
            
            var taskInfo = JsonNetAdapter.Deserialize<TaskInfo>(jsonString);
            return taskInfo;
        }
    }
}