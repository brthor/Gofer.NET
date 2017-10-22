using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Thor.Tasks
{
    public static class Retry
    {
        public static void OnException(Action action, Type[] handledExceptions, int retryCount=10, int startingDelay=100, int backoffFactor=2)
        {
            var pseudoFunc = new Func<object>(() =>
            {
                action();
                return null;
            });
            
            OnException(pseudoFunc, handledExceptions, retryCount);
        }
        
        public static T OnException<T>(Func<T> func, Type[] handledExceptions, 
            int retryCount=10, int startingDelay=100, int backoffFactor=2, double backoffVariance=0.2)
        {
            var pseudoFunc = new Func<T>(() =>
            {
                T ret;
                try
                {
                    ret = func();
                }
                catch (Exception e)
                {
                    if (handledExceptions.Contains(e.GetType()))
                    {
                        return default(T);
                    }
                    
                    throw;
                }

                return ret;
            });

            return OnValue(pseudoFunc, default(T), retryCount, startingDelay, backoffFactor, backoffVariance);
        }
        
        public static T OnValue<T>(Func<T> func, T value, 
            int retryCount=10, int startingDelay=100, int backoffFactor=2, double backoffVariance=0.2)
        {
            var delay = startingDelay;
            
            for (var retry = 1; retry <= retryCount; ++retry)
            {
                var ret = func();
                
                if (ret.Equals(value))
                {
                    if (retry == retryCount)
                        throw new Exception("Invalid value");

                    Console.WriteLine("Retrying... ");
                    Thread.Sleep(delay); // TODO: Task.Delay, async api

                    var variedBackoffFactor = backoffFactor + (new Random().NextDouble() * backoffVariance);
                    delay = (int) (delay * variedBackoffFactor);
                    continue;
                }

                return ret;
            }

            throw new Exception("Should be unreachable");
        }
    }
    
    
}