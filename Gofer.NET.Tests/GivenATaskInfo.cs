using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gofer.NET.Utils;
using Xunit;
using FluentAssertions;
using System.Reflection;

namespace Gofer.NET.Tests
{
    public class GivenATaskInfo
    {
        [Fact]
        public void ItPersistsPropertiesWhenSerializedAndDeserialized()
        {  
            var taskInfos = new[] {
                GetTestTask(() => TestMethod1("hello world")),
                GetTestTask(() => TestMethod2())
            };

            foreach (var taskInfo in taskInfos) {
                var serializedTaskInfo = JsonTaskInfoSerializer.Serialize(taskInfo);
                var deserializedTaskInfo = JsonTaskInfoSerializer.Deserialize<TaskInfo>(serializedTaskInfo);

                deserializedTaskInfo.Id.Should().Be(taskInfo.Id);
                deserializedTaskInfo.AssemblyName.Should().Be(taskInfo.AssemblyName);
                deserializedTaskInfo.TypeName.Should().Be(taskInfo.TypeName);
                deserializedTaskInfo.MethodName.Should().Be(taskInfo.MethodName);
                deserializedTaskInfo.ReturnType.Should().Be(taskInfo.ReturnType);
                deserializedTaskInfo.Args.ShouldAllBeEquivalentTo(taskInfo.Args);
                deserializedTaskInfo.ArgTypes.ShouldAllBeEquivalentTo(taskInfo.ArgTypes);
                deserializedTaskInfo.CreatedAtUtc.Should().Be(taskInfo.CreatedAtUtc);

                deserializedTaskInfo.IsEquivalent(taskInfo).Should().BeTrue();
            }
        }

        [Fact]
        public void ItProperlyDeterminesEquivalence()
        {
            var taskInfo1a = GetTestTask(() => TestMethod1("hello world"));
            var taskInfo1b = GetTestTask(() => TestMethod1("hello world"));
            var taskInfo1c = GetTestTask(() => TestMethod1("hello "));

            var taskInfo2a = GetTestTask(() => TestMethod2());
            var taskInfo2b = GetTestTask(() => TestMethod2());

            var taskInfo3a = GetTestTask(() => Console.WriteLine("hello"));
            var taskInfo3b = GetTestTask(() => Console.WriteLine("hello world"));

            taskInfo1a.IsEquivalent(taskInfo1a).Should().BeTrue();
            taskInfo1a.IsEquivalent(taskInfo1b).Should().BeTrue();
            taskInfo1a.IsEquivalent(taskInfo1c).Should().BeFalse();

            taskInfo1a.IsEquivalent(taskInfo2a).Should().BeFalse();
            taskInfo1a.IsEquivalent(taskInfo3a).Should().BeFalse();

            taskInfo2a.IsEquivalent(taskInfo2a).Should().BeTrue();
            taskInfo2a.IsEquivalent(taskInfo2b).Should().BeTrue();

            taskInfo2a.IsEquivalent(taskInfo3a).Should().BeFalse();

            taskInfo3a.IsEquivalent(taskInfo3a).Should().BeTrue();
            taskInfo3a.IsEquivalent(taskInfo3b).Should().BeFalse();
        }

        private TaskInfo GetTestTask(Expression<Action> action) 
        {
            return action.ToTaskInfo();
        }

        private string TestMethod1(string arg)
        {
            return arg;
        }

        private void TestMethod2()
        {
            Console.WriteLine(nameof(TestMethod2));
        }
    }
}