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
    public class GivenARedisQueue
    {
        [Fact]
        public async Task ItPopsFromHeadToTailWhenCallingPopPush()
        {
            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();
            var queueName2 = Guid.NewGuid().ToString();

            await queue.Push(queueName, 1);
            await queue.Push(queueName, 2);

            await queue.Push(queueName2, 10);
            await queue.Push(queueName2, 20);

            ((int)await queue.PeekTail(queueName2)).Should().Be(20);

            ((int) await queue.PopPush(queueName, queueName2)).Should().Be(1);

            ((int)await queue.PeekTail(queueName2)).Should().Be(1);
            ((int)await queue.Peek(queueName)).Should().Be(2);
        }

        [Fact]
        public async Task ItDequeuesItemsInFIFOOrder()
        {
            const int numberOfItems = 100;

            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();

            for (var i=0;i<numberOfItems; ++i)
            {
                await queue.Push(queueName, i);
            }

            ((int)(await queue.Peek(queueName))).Should().Be(0);
            ((int)(await queue.PeekTail(queueName))).Should().Be(numberOfItems-1);

            for (var i=0;i<numberOfItems; ++i)
            {
                ((int)(await queue.Peek(queueName))).Should().Be(i);
                var item = (int) (await queue.Pop(queueName));
                item.Should().Be(i);
            }  
        }

        [Fact]
        public async Task ItRemovesFromHeadFirst()
        {
            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();

            await queue.Push(queueName, 1);
            await queue.Push(queueName, 2);
            await queue.Push(queueName, 1);
            await queue.Push(queueName, 3);

            (await queue.Remove(queueName, 1)).Should().BeTrue();
            (await queue.Remove(queueName, 5)).Should().BeFalse();

            ((int)await queue.Peek(queueName)).Should().Be(2);

            var allItems = (await queue.PopAll(queueName)).Select(v => (int)v).ToArray();
            allItems.ShouldAllBeEquivalentTo(new int[] {2,1,3});
        }

        [Fact]
        public async Task ItRemovesFromTailFirstWhenCallingRemoveTail()
        {
            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();

            await queue.Push(queueName, 1);
            await queue.Push(queueName, 2);
            await queue.Push(queueName, 1);
            await queue.Push(queueName, 3);

            (await queue.RemoveTail(queueName, 1)).Should().BeTrue();
            (await queue.RemoveTail(queueName, 5)).Should().BeFalse();

            ((int)await queue.Peek(queueName)).Should().Be(1);

            var allItems = (await queue.PopAll(queueName)).Select(v => (int)v).ToArray();
            allItems.ShouldAllBeEquivalentTo(new int[] {1,2,3});
        }

        [Fact]
        public async Task ItPopsAllItemsInFIFOOrder()
        {
            const int numberOfItems = 100;

            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();

            for (var i=0;i<numberOfItems; ++i)
            {
                await queue.Push(queueName, i);
            }

            var allItems = (await queue.PopAll(queueName)).ToArray();
            allItems.Length.Should().Be(numberOfItems);

            for (var i=0;i<numberOfItems; ++i)
            {
                ((int) allItems[i]).Should().Be(i);
            }  
        }

        [Fact]
        public async Task ItPerformsBatchPopProperly() 
        {
            var backend = (RedisBackend) TaskQueueTestFixture.UniqueRedisTaskQueue().Backend;
            var queue = backend.RedisQueue;

            var queueName = Guid.NewGuid().ToString();

            for (var i=0; i<50; ++i)
            {
                await queue.Push(queueName, i);
            }

            var batchValues = (await queue.PopBatch(queueName, 20)).ToArray();

            for (var i=0; i<20; ++i)
            {
                ((int) batchValues[i]).Should().Be(i);
            }

            for (var i=20; i<50; ++i)
            {
                ((int)(await queue.Pop(queueName))).Should().Be(i);
            }
        }
    }
}
