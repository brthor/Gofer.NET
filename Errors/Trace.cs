using System;

namespace Gofer.NET.Errors
{
    public static class Trace
    {
        private static object _locker = new object();
        
        public static void Exception(string message, Exception exception)
        {
            lock (_locker)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                
                Console.WriteLine(message);
                Console.WriteLine(exception);

                Console.ForegroundColor = oldColor;
            }
        }
        
        public static void Error(string message)
        {
            lock (_locker)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                
                Console.WriteLine(message);

                Console.ForegroundColor = oldColor;
            }
        }
        
        public static void Warning(string message)
        {
            lock (_locker)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                
                Console.WriteLine(message);

                Console.ForegroundColor = oldColor;
            }
        }
        
        public static void Info(string message)
        {
            lock (_locker)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                
                Console.WriteLine(message);

                Console.ForegroundColor = oldColor;
            }
        }
    }
}