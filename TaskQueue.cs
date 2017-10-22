using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

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

            /// Usage of the Task Queue in Parallel Threads, requires the thread pool size to be increased.
            /// 
            /// https://stackexchange.github.io/StackExchange.Redis/Timeouts#are-you-seeing-high-number-of-busyio-or-busyworker-threads-in-the-timeout-exception
            if (config.ThreadSafe)
            {
                ThreadPool.SetMinThreads(200, 200);
            }
        }

        public class MethodCallArgumentResolutionVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var argumentExpressions = new List<Expression>();
                foreach (var argument in node.Arguments)
                {
                    var argumentResolver = Expression.Lambda(argument);
                    var argumentValue = argumentResolver.Compile().DynamicInvoke();

                    var valueExpression = Expression.Constant(argumentValue, argument.Type);
                    argumentExpressions.Add(valueExpression);
                }
                
                return Expression.Call(node.Object, node.Method, argumentExpressions);
            }
        }

        public void Enqueue(Expression<Action> expression)
        {
            
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved =
                (Expression<Action>) methodCallArgumentResolutionVisitor.Visit(expression);

            var method = ((MethodCallExpression) expressionWithArgumentsResolved.Body);
            var m = method.Method;
            var args = method.Arguments
                .Select(a =>
                        {
                            var value = Expression.Lambda(a).Compile().DynamicInvoke();
                            if (a.Type == typeof(object))
                                return (object) value;
                            
                            return Convert.ChangeType(value, a.Type);
                        })
                .ToArray();

            var taskInfo = m.ToTaskInfo(args);
            Enqueue(taskInfo);
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
            var jsonString = JsonConvert.SerializeObject(taskInfo, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            Backend.Enqueue(jsonString);
        }
        
        public void ExecuteNext()
        {
            var taskInfo = Dequeue();

            taskInfo?.ExecuteTask();
        }

        public TaskInfo Dequeue()
        {
            var jsonString = Backend.Dequeue();
            if (jsonString == null)
            {
                return null;
            }
            
            var taskInfo = JsonConvert.DeserializeObject<TaskInfo>(jsonString, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
            return taskInfo;
        }
    }
}