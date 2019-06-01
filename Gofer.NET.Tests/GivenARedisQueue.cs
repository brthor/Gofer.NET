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
