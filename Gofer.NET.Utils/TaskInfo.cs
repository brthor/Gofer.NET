using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Gofer.NET.Utils.Errors;

namespace Gofer.NET.Utils
{
    public class TaskInfo
    {
        public string Id { get; set; }
        
        public string AssemblyName { get; set; }
        
        public string TypeName { get; set; }
        
        public string MethodName { get; set; }

        public object[] Args { get; set; }

        public Type[] ArgTypes { get; set; }
        
        public Type ReturnType { get; set; }
        
        public DateTime CreatedAtUtc { get; set; }

        public bool IsExpired(TimeSpan expirationSpan)
        {
            return CreatedAtUtc < (DateTime.UtcNow - expirationSpan);
        }

        public bool IsEquivalent(TaskInfo otherTaskInfo) 
        {
            if (otherTaskInfo == null)
            {
                throw new ArgumentException("otherTaskInfo must not be null");
            }
            
            if (Args.Length != otherTaskInfo.Args.Length) 
            {
                return false;
            }

            for (var i=0; i<Args.Length; ++i) {
                if (!Args[i].Equals(otherTaskInfo.Args[i]))
                {
                    return false;
                }
            }

            return string.Equals(AssemblyName, otherTaskInfo.AssemblyName, StringComparison.Ordinal)
                && string.Equals(TypeName, otherTaskInfo.TypeName, StringComparison.Ordinal)
                && string.Equals(MethodName, otherTaskInfo.MethodName, StringComparison.Ordinal)
                && ReturnType.Equals(otherTaskInfo.ReturnType);
        }

        public void ConvertTypeArgs()
        {
            for (var i=0;i<Args.Length;++i) {
                if (Args[i] == null)
                    continue;

                if (typeof(Type).IsInstanceOfType(Args[i]))
                {
                    Args[i] = new TypeWrapper { Type = (Type)Args[i] };
                }
            }
        }

        public void UnconvertTypeArgs() 
        {
            for (var i=0;i<Args.Length;++i) {
                if (Args[i] == null)
                    continue;

                var argType = Nullable.GetUnderlyingType(ArgTypes[i]) ?? ArgTypes[i];

                if (typeof(TypeWrapper).IsInstanceOfType(Args[i]))
                {
                    Args[i] = ((TypeWrapper)Args[i]).Type;
                }
                else if (typeof(TimeSpan).IsAssignableFrom(argType))
                {
                    Args[i] = TimeSpan.Parse((string)Args[i]);
                }
                else if (typeof(DateTime).IsAssignableFrom(argType) && Args[i] is DateTimeOffset dateTimeOffset)
                {
                    Args[i] = dateTimeOffset.DateTime;
                }
            }
        }

        private async Task<object> InvokeMethod(MethodInfo method, object instance)
        {
            if (method.IsAsync())
            {
                var result = method.Invoke(instance, Args);

                var task = (Task) result;
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                var resultValue = resultProperty.GetValue(task);
            
                // Return null if the method is a Task<void> equivalent
                var resultType = resultValue.GetType();
                if (resultType.Name.Equals("VoidTaskResult", StringComparison.Ordinal)
                    && resultType.Namespace.Equals("System.Threading.Tasks", StringComparison.Ordinal))
                {
                    return null;
                }
            
                return resultValue;
            }
            
            return method.Invoke(instance, Args);
        }

        public async Task<object> ExecuteTask()
        {
            var assembly = Assembly.Load(AssemblyName);
            var type = assembly.GetType(TypeName);
            
            var staticMethod = type.GetMethod(MethodName, 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, 
                null, 
                ArgTypes, 
                null);

            if (staticMethod != null)
            {
                return await InvokeMethod(staticMethod, null);
            }
            
            var instanceMethod = type.GetMethod(MethodName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                null, 
                ArgTypes, 
                null);
            
            if (instanceMethod == null)
            {
                throw new UnableToDeserializeDelegateException();
            }

            var instance = Activator.CreateInstance(type);
            
            return await InvokeMethod(instanceMethod, instance);
        }
    }
}