using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gofer.NET.Utils
{
    public static class ActionExtensionMethods
    {
        public static TaskInfo ToTaskInfo(this Expression<Action> expression)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved =
                (Expression<Action>) methodCallArgumentResolutionVisitor.Visit(expression);

            var method = ((MethodCallExpression) expressionWithArgumentsResolved.Body);
            var m = method.Method;
            var args = method.Arguments
                .Select(a =>
                {
                    var value = ((ConstantExpression) a).Value;
                    return value;
                })
                .ToArray();

            var taskInfo = m.ToTaskInfo(args);

            return taskInfo;
        }
        
        public static TaskInfo ToTaskInfo<T>(this Expression<Func<T>> expression)
        {
            var methodCallArgumentResolutionVisitor = new MethodCallArgumentResolutionVisitor();
            var expressionWithArgumentsResolved =
                (Expression<Func<T>>) methodCallArgumentResolutionVisitor.Visit(expression);

            var method = ((MethodCallExpression) expressionWithArgumentsResolved.Body);
            var m = method.Method;
            var args = method.Arguments
                .Select(a =>
                {
                    var value = ((ConstantExpression) a).Value;
                    return value;
                })
                .ToArray();

            var taskInfo = m.ToTaskInfo(args);

            return taskInfo;
        }
        
        public static TaskInfo ToTaskInfo(this MethodInfo method, object[] args)
        {
            var taskInfo = new TaskInfo
            {
                AssemblyName = method.DeclaringType.Assembly.FullName,
                TypeName = method.DeclaringType.FullName,
                MethodName = method.Name,
                Args = args,
                Id = Guid.NewGuid().ToString(),
                CreatedAtUtc = DateTime.UtcNow,
                ReturnType = method.ReturnType
            };
            
            return taskInfo;
        }
    }
}