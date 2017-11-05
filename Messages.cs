using System;
using Gofer.NET.Utils;

namespace Gofer.NET
{
    public static class Messages
    {
        public static string TaskThrewException(TaskInfo info) => 
            $"[{DateTime.Now.ToShortTimeString()}] Task Error: {info.AssemblyName}.{info.MethodName} threw an exception. (Task Id: {info.Id})";
        
        public static string TaskStarted(TaskInfo info) => 
            $"[{DateTime.Now.ToShortTimeString()}] Task Received: {info.AssemblyName}.{info.MethodName} (Task Id: {info.Id})";
        
        public static string TaskFinished(TaskInfo info, double completionSeconds) => 
            $"[{DateTime.Now.ToShortTimeString()}] Task Finished ({completionSeconds}s): {info.AssemblyName}.{info.MethodName} (Task Id: {info.Id})";
    }
}
