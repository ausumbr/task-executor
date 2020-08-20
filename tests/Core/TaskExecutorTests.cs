using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace TaskExecutor.Core.Tests
{
    public class TaskExecutorTests
    {
        [Fact]
        public void GivenTaskExecutorShouldNotWaitForFreeSlot()
        {
            Action action = () =>
            {
                using var taskExecutor = new TaskExecutor(1)
                {
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(100).Wait()),
                    new Task(() => Task.Delay(1000).Wait())
                };

            };

            action.ExecutionTimeOf(s => s.Invoke()).Should().BeLessThan(TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void GivenTaskExecutorShouldFailWhenAddARunningTask()
        {
            var taskExecutor = new TaskExecutor(1);
            
            Action action = () => taskExecutor.Add(Task.Delay(10));
            
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GivenTaskExecutorShouldExecuteAnActionInParallel()
        {
            using var taskExecutor = new TaskExecutor(1);
            taskExecutor.ExecutionTimeOf(t => t.Execute(() => Task.Delay(1000).Wait()))
                .Should()
                .BeLessThan(TimeSpan.FromMilliseconds(10));
            
        }

        [Fact]
        public async Task GivenTaskExecutorShouldAddTaskExecutor()
        {
            var taskExecutor = new TaskExecutor(5);
            var addTaskExecutor = new List<Task>(); 
            for (var i = 0; i < 10; i++)
            {
                addTaskExecutor.Add(Task.Factory.StartNew(() => taskExecutor.Add(new Task(() => Task.Delay(100).Wait()))));
            }
            await Task.WhenAll(addTaskExecutor);
            await Task.Delay(500);
            taskExecutor.Count().Should().Be(0);
        }

        [Fact]
        public void GivenTaskExecutorWhenDisposeShouldWaitTaskExecutorExecutions()
        {
            var value = 0;
            var taskExecutor = new TaskExecutor(500);
            for (var i = 0; i < 1000; i++)
            {
                taskExecutor.Add(new Task(() => Interlocked.Increment(ref value)));
            }
            taskExecutor.Dispose();
            value.Should().Be(1000);
        }

        [Fact]
        public void GivenTaskExecutorWhenCallDisposeTwiceShouldNotFail()
        {
            var taskExecutor = new TaskExecutor(0);
            taskExecutor.Dispose();
            taskExecutor.Invoking(t => t.Dispose()).Should().NotThrow();
        }

        [Fact]
        public void GivenTaskExecutorShouldAbleToGetEnumerator()
        {
            IEnumerable taskExecutor = new TaskExecutor(0);
            taskExecutor.GetEnumerator().Should().NotBeNull();
        }
    }
}
