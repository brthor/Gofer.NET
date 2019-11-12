using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gofer.NET.Utils
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Special placeholder that allows compiler warnings on non-awaited tasks to be bypassed.
        /// </summary>
        public static Task T(this Task task)
        {
            return task;
        }

        public static Task<TG> T<TG>(this Task<TG> task)
        {
            return task;
        }
    }
}