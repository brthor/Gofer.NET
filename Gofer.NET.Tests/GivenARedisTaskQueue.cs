using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Gofer.NET.Tests
{
    public class GivenARedisTaskQueue
    {
        private class TestDataHolder
        {
            public string Value { get; set; }

            public override string ToString()
            {
                return Value;
            }
        }

        [Fact]
        public async Task ItCapturesArgumentsPassedToEnqueuedDelegate()
        {
            var testFixture = new TaskQueueTestFixture(nameof(ItCapturesArgumentsPassedToEnqueuedDelegate));

            string variableToExtract = "extracted";

            var semaphoreFile = Path.GetTempFileName();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            Func<Expression<Action>, string, Tuple<Expression<Action>, string>> TC = (actionExp, str) =>
                Tuple.Create<Expression<Action>, string>(
                    actionExp,
                    str);
            
            // Action to expected result
            var delgates = new Tuple<Expression<Action>, string>[]
            {
                // Integer Arguments
                TC(() => IntFunc(int.MaxValue, semaphoreFile), int.MaxValue.ToString()),
                TC(() => IntFunc(int.MinValue, semaphoreFile), int.MinValue.ToString()),
                TC(() => NullableIntFunc(null, semaphoreFile), "-1"),
                TC(() => NullableIntFunc(int.MinValue, semaphoreFile), int.MinValue.ToString()),
                TC(() => NullableIntFunc(int.MaxValue, semaphoreFile), int.MaxValue.ToString()),

                // Float Arguments
                TC(() => FloatFunc(float.MaxValue, semaphoreFile), float.MaxValue.ToString()),
                TC(() => FloatFunc(float.MinValue, semaphoreFile), float.MinValue.ToString()),

                // Double Arguments
                TC(() => DoubleFunc(double.MaxValue, semaphoreFile), double.MaxValue.ToString()),
                TC(() => DoubleFunc(double.MinValue, semaphoreFile), double.MinValue.ToString()),

                // Long Arguments
                TC(() => LongFunc(long.MaxValue, semaphoreFile), long.MaxValue.ToString()),
                TC(() => LongFunc(long.MinValue, semaphoreFile), long.MinValue.ToString()),

                TC(() => BoolFunc(true, semaphoreFile), true.ToString()),
                TC(() => BoolFunc(false, semaphoreFile), false.ToString()),

                TC(() => StringFunc("astring", semaphoreFile), "astring"),
                TC(() => StringFunc(variableToExtract, semaphoreFile), variableToExtract),

                TC(() => ObjectFunc(new TestDataHolder {Value = "astring"}, semaphoreFile), "astring"),

                TC(() => DateTimeFunc(now, semaphoreFile), now.ToString()),
                TC(() => DateTimeFunc(utcNow, semaphoreFile), utcNow.ToString()),

                TC(() => NullableTypeFunc(null, semaphoreFile), "null"),
                TC(() => NullableTypeFunc(now, semaphoreFile), now.ToString()),
                TC(() => ArrayFunc1(new[] {"this", "string", "is"}, semaphoreFile), "this,string,is"),
                TC(() => ArrayFunc2(new[] {1, 2, 3, 4}, semaphoreFile), "1,2,3,4"),
                TC(() => ArrayFunc3(new int?[] {1, 2, 3, null, 5}, semaphoreFile), "1,2,3,null,5"),
                
                // Awaiting inside the lambda is unnecessary, as the method is extracted and serialized.
                TC(() => AsyncFunc(semaphoreFile).Wait(), "async"),
#pragma warning disable 4014
                TC(() => AsyncFunc(semaphoreFile), "async"),
                TC(() => AsyncFuncThatReturnsString(semaphoreFile), "async")
#pragma warning restore 4014
            };
            

            foreach (var tup in delgates)
            {
                var actionExpr = tup.Item1;
                var expectedString = tup.Item2;
                
                File.Delete(semaphoreFile);
                
                await testFixture.TaskQueue.Enqueue(actionExpr); 
                await testFixture.TaskQueue.ExecuteNext();

                File.ReadAllText(semaphoreFile).Should().Be(expectedString);
            }
            
            File.Delete(semaphoreFile);
        }
        
        [Fact]
        public async Task ItEnqueuesAndReceivesDelegatesThatAreRunnable()
        {
            var testFixture = new TaskQueueTestFixture(nameof(ItEnqueuesAndReceivesDelegatesThatAreRunnable));
            
            testFixture.EnsureSemaphoreDoesntExist();
            await testFixture.PushPopExecuteWriteSemaphore();
            testFixture.EnsureSemaphore();
        }

        [Fact]
        public async Task ItsTasksAreConsumedOnlyOnceByMultipleConsumers()
        {
            // Higher numbers here increase confidence
            var numberOfJobs = 16;
            var numberOfConsumers = 4;
            
            var sharedTaskQueueName = nameof(ItsTasksAreConsumedOnlyOnceByMultipleConsumers);
            var consumers = Enumerable.Range(0, numberOfConsumers)
                .Select(_ => new TaskQueueTestFixture(sharedTaskQueueName)).ToList();

            var semaphoreFiles = new List<string>();
            for(int i=0;i < numberOfJobs;++i)
            {
                var path = Path.GetTempFileName();
                File.Delete(path);
                semaphoreFiles.Add(path);
                
                var sharedTaskQueue = consumers[0].TaskQueue;
                await sharedTaskQueue.Enqueue(() => TaskQueueTestFixture.WriteSemaphore(path));
            }

            var tasks = new List<Task>();

            // Purposely executing more times than the number of tasks we have
            // Specifically numberOfJobs * numberOfConsumers times.
            for (var i = 0; i < numberOfJobs; i += 1)
            {
                foreach (var consumer in consumers)
                {
                    var task = Task.Run(() => consumer.TaskQueue.ExecuteNext());
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            foreach (var semaphoreFile in semaphoreFiles)
            {
                File.ReadAllText(semaphoreFile).Should()
                    .Be(TaskQueueTestFixture.SemaphoreText);
            }
        }

        public async Task AsyncFunc(string semaphoreFile)
        {
            // Wait to ensure async waiting is happening.
            await Task.Delay(1000);
            
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, "async");
        }
        
        public async Task<string> AsyncFuncThatReturnsString(string semaphoreFile)
        {
            // Wait to ensure async waiting is happening.
            await Task.Delay(1000);
            
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, "async");

            return "async";
        }
        
        public void NullableTypeFunc(DateTime? dateTime, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, dateTime);
        }
        
        public void DateTimeFunc(DateTime dateTime, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, dateTime);
        }

        public void IntFunc(int num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void NullableIntFunc(int? num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num ?? -1);
        }
        
        public void LongFunc(long num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void FloatFunc(float num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void BoolFunc(bool num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void DoubleFunc(double num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void StringFunc(string num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void ObjectFunc(object num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, num);
        }
        
        public void ArrayFunc1(string[] nums, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, string.Join(",", nums));
        }
        
        public void ArrayFunc2(int[] nums, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, string.Join(",", nums));
        }
        
        public void ArrayFunc3(int?[] nums, string semaphoreFile)
        {
            var str = "";
            var first = true;
            foreach (var num in nums)
            {
                if (!first) str += ",";
                str += num?.ToString() ?? "null";
                first = false;
            }
            
            TaskQueueTestFixture.WriteSemaphoreValue(semaphoreFile, str);
        }
    }
}
