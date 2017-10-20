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

        public void ExecuteTask()
        {
            var assembly = System.Reflection.Assembly.Load(AssemblyName);
            var type = assembly.GetType(TypeName);
            
            var method = type.GetMethod(MethodName, 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                throw new UnableToDeserializeDelegateException();
            }

            method.Invoke(null, Args);
        }
    }
}