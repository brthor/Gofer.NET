using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gofer.NET.Utils
{
    public static class MethodInfoExtensions
    {
        public static bool IsAsync(this MethodInfo m)
        {
            return m?
                       .GetCustomAttribute<AsyncStateMachineAttribute>()?
                       .StateMachineType?
                       .GetTypeInfo()
                       .GetCustomAttribute<CompilerGeneratedAttribute>()
                   != null;
        }
    }
}