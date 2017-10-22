using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Thor.Tasks.Tests
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
        public void ItCapturesArgumentsPassedToEnqueuedDelegate()
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
                TC(() => ArrayFunc3(new int?[] {1, 2, 3, null, 5}, semaphoreFile), "1,2,3,null,5")
            };

            foreach (var tup in delgates)
            {
                var actionExpr = tup.Item1;
                var expectedString = tup.Item2;
                
                File.Delete(semaphoreFile);
                
                testFixture.TaskQueue.Enqueue(actionExpr); 
                testFixture.TaskQueue.ExecuteNext();

                File.ReadAllText(semaphoreFile).Should().Be(expectedString);
            }
            
            File.Delete(semaphoreFile);
        }
        
        [Fact]
        public void ItEnqueuesAndReceivesDelegatesThatAreRunnable()
        {
            var testFixture = new TaskQueueTestFixture(nameof(ItEnqueuesAndReceivesDelegatesThatAreRunnable));
            
            testFixture.EnsureSemaphoreDoesntExist();
            testFixture.PushPopExecuteWriteSemaphore();
            testFixture.EnsureSemaphore();
        }

        [Fact]
        public async Task ItsTasksAreConsumedOnlyOnceByMultipleConsumers()
        {
            var numberOfJobs = 4;
            
            var sharedTaskQueueName = nameof(ItsTasksAreConsumedOnlyOnceByMultipleConsumers);
            var consumers = new[]
            {
                new TaskQueueTestFixture(sharedTaskQueueName),
                new TaskQueueTestFixture(sharedTaskQueueName)
            };

            var semaphoreFiles = new List<string>();
            for(int i=0;i < numberOfJobs;++i)
            {
                var path = Path.GetTempFileName();
                File.Delete(path);
                semaphoreFiles.Add(path);
                
                var sharedTaskQueue = consumers[0].TaskQueue;
                sharedTaskQueue.Enqueue(() => TaskQueueTestFixture.WriteSempaphore(path));
            }

            var tasks = new List<Task>();

            for (int i = 0; i < numberOfJobs; i += consumers.Length)
            {
                foreach (var consumer in consumers)
                {
                    var task = Task.Run(() => consumer.TaskQueue.ExecuteNext());
                    tasks.Add(task);
                }
            }

            foreach (var task in tasks)
            {
                await task;
            }

            foreach (var semaphoreFile in semaphoreFiles)
            {
                File.ReadAllText(semaphoreFile).Should().Be(TaskQueueTestFixture.SemaphoreText);
            }
        }
        
        public void NullableTypeFunc(DateTime? dateTime, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, dateTime);
        }
        
        public void DateTimeFunc(DateTime dateTime, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, dateTime);
        }

        public void IntFunc(int num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void NullableIntFunc(int? num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num ?? -1);
        }
        
        public void LongFunc(long num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void FloatFunc(float num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void BoolFunc(bool num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void DoubleFunc(double num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void StringFunc(string num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void ObjectFunc(object num, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, num);
        }
        
        public void ArrayFunc1(string[] nums, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, string.Join(",", nums));
        }
        
        public void ArrayFunc2(int[] nums, string semaphoreFile)
        {
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, string.Join(",", nums));
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
            
            TaskQueueTestFixture.WriteSempaphoreValue(semaphoreFile, str);
        }
    }
}
