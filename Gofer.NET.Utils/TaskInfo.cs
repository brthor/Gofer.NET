using System;
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