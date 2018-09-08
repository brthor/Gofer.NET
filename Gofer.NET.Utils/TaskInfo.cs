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
                    return await (Task<object>) staticMethod.Invoke(null, Args);
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
                return await (Task<object>) instanceMethod.Invoke(instance, Args);
            }
            
            return instanceMethod.Invoke(instance, Args);
        }
    }
}