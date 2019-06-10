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

                if (typeof(Type).IsAssignableFrom(Args[i].GetType())) {
                    Args[i] = new TypeWrapper {Type=(Type)Args[i]};
                }
            }
        }

        public void UnconvertTypeArgs() 
        {
            for (var i=0;i<Args.Length;++i) {
                if (Args[i] == null)
                    continue;
                    
                if (typeof(TypeWrapper).IsAssignableFrom(Args[i].GetType())) {
                    Args[i] = ((TypeWrapper) Args[i]).Type;
                }
            }
        }

        public async Task<object> ExecuteTask()
        {
            var assembly = Assembly.Load(AssemblyName);
            var type = assembly.GetType(TypeName);
            
            var staticMethod = type.GetMethod(MethodName, 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (staticMethod != null)
            {
                if (staticMethod.IsAsync())
                {
                    var result = staticMethod.Invoke(null, Args);

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
                
                return staticMethod.Invoke(null, Args);
            }
            
            var instanceMethod = type.GetMethod(MethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (instanceMethod == null)
            {
                throw new UnableToDeserializeDelegateException();
            }

            var instance = Activator.CreateInstance(type);
            
            if (instanceMethod.IsAsync())
            {
                var result = instanceMethod.Invoke(instance, Args);
                
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
            
            return instanceMethod.Invoke(instance, Args);
        }
    }
}