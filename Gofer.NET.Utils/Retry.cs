using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Gofer.NET.Utils
{
    public static class Retry
    {
        /// <summary>
        /// Retry the specified action if any of the handled exceptions occur.
        /// </summary>
        /// <param name="action">Action which should be retried on specified exceptions.</param>
        /// <param name="handledExceptions">List of handled exception types.</param>
        /// <param name="retryCount">How many times to retry.</param>
        /// <param name="initialDelayBetweenRetries">Number of milliseconds to wait between retries.</param>
        /// <param name="backoffFactor">initialDelayBetweenRetries multiplied by this amount each retry</param>
        /// <param name="backoffVariance">A random number between 0 and backoffVariance is added to backoffFactor when calculating delay.</param>
        public static void OnException(Action action, Type[] handledExceptions, int retryCount = 10,
            int initialDelayBetweenRetries = 100, int backoffFactor = 2, double backoffVariance=0.2)
        {
            var pseudoFunc = new Func<object>(() =>
            {
                action();
                return null;
            });
            
            OnException(pseudoFunc, handledExceptions, retryCount);
        }
        
        /// <summary>
        /// Retry the specified action if any of the handled exceptions occur.
        /// </summary>
        /// <param name="func">Action which should be retried on specified exceptions.</param>
        /// <param name="handledExceptions">List of handled exception types.</param>
        /// <param name="retryCount">How many times to retry.</param>
        /// <param name="initialDelayBetweenRetries">Number of milliseconds to wait between retries.</param>
        /// <param name="backoffFactor">initialDelayBetweenRetries multiplied by this amount each retry</param>
        /// <param name="backoffVariance">A random number between 0 and backoffVariance is added to backoffFactor when calculating delay.</param>
        public static T OnException<T>(Func<T> func, Type[] handledExceptions, int retryCount=10, 
            int initialDelayBetweenRetries=100, int backoffFactor=2, double backoffVariance=0.2)
        {
            var delay = initialDelayBetweenRetries;
            
            for (var retry = 1; retry <= retryCount; ++retry)
            {
                T ret;
                try
                {
                    ret = func();
                }
                catch (Exception e)
                {
                    if (retry == retryCount)
                        throw;

                    if (!handledExceptions.Contains(e.GetType()))
                        throw;
                    
                    Console.WriteLine(e.Message);
                    
                    Thread.Sleep(delay); // TODO: Task.Delay, async api

                    var variedBackoffFactor = backoffFactor + (new Random().NextDouble() * backoffVariance);
                    delay = (int) (delay * variedBackoffFactor);
                    continue;
                }

                return ret;
            }

            throw new Exception("Should be unreachable");
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