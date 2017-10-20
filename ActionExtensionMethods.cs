using System;
using System.Reflection;

namespace Thor.Tasks
{
    public static class ActionExtensionMethods
    {
        public static TaskInfo ToTaskInfo(this MethodInfo method, object[] args)
        {
            var taskInfo = new TaskInfo
            {
                AssemblyName = method.DeclaringType.Assembly.FullName,
                TypeName = method.DeclaringType.FullName,
                MethodName = method.Name,
                Args = args
            };
            
            return taskInfo;
        }
        
        public static TaskInfo ToTaskInfo<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> action, object[] args)
        {
            return action.Method.ToTaskInfo(args);
        }
        
        public static TaskInfo ToTaskInfo<T1, T2, T3>(this Action<T1, T2, T3> action, object[] args)
        {
            return action.Method.ToTaskInfo(args);
        }
        
        public static TaskInfo ToTaskInfo<T1, T2>(this Action<T1, T2> action, object[] args)
        {
            return action.Method.ToTaskInfo(args);
        }
        
        public static TaskInfo ToTaskInfo<T>(this Action<T> action, object[] args)
        {
            return action.Method.ToTaskInfo(args);
        }
        
        public static TaskInfo ToTaskInfo(this Action action, object[] args)
        {
            return action.Method.ToTaskInfo(args);
        }
    }
}