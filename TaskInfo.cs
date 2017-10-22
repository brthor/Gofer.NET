using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using StackExchange.Redis;
using Thor.Tasks.Errors;

namespace Thor.Tasks
{
    [DataContract]
    public class TaskInfo
    {
        [DataMember]
        public string AssemblyName { get; set; }
        
        [DataMember]
        public string TypeName { get; set; }
        
        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public object[] Args { get; set; }

        public object[] ArgsWithPatchedTypes()
        {
            return Args.Select(a => a is long ? Convert.ToInt32(a) : a).ToArray();
        }

        public void ExecuteTask()
        {
            var assembly = Assembly.Load(AssemblyName);
            var type = assembly.GetType(TypeName);
            
            var staticMethod = type.GetMethod(MethodName, 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (staticMethod != null)
            {
                staticMethod.Invoke(null, Args);
                return;
            }
            
            var instanceMethod = type.GetMethod(MethodName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (instanceMethod == null)
            {
                throw new UnableToDeserializeDelegateException();
            }

            var instance = Activator.CreateInstance(type);
            
            instanceMethod.Invoke(instance, ArgsWithPatchedTypes());
        }
    }
}