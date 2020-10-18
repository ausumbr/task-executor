using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace TaskExecutor.Core.Tests
{
    public class TaskExecutorTests
    {
        private const int MAX_PARALLEL_TAKS = 100;

        public TaskExecutorTests()
        {
            ThreadPool.SetMinThreads(250, 250);
        }
        
        [Fact]
        public void GivenTaskExecutorShouldExecuteCapacityInParallel()
        {
            var count = 0;
            var taskExecutor = new TaskExecutor(MAX_PARALLEL_TAKS);

            Action action = () =>
            {
                for (var i = 0; i < MAX_PARALLEL_TAKS; i++)
                {
                    taskExecutor.Execute(() =>
                    {
                        Interlocked.Increment(ref count);
                        Task.Delay(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
                    });
                }
            };
            
            action.ExecutionTime().Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(10));
            Task.Delay(100).Wait();
            count.Should().Be(MAX_PARALLEL_TAKS);
        }
        
        [Fact]
        public void GivenTaskExecutorWhenFullCapacityShouldEnqueueTask()
        {
            var taskExecutor = new TaskExecutor(MAX_PARALLEL_TAKS);
            for (var i = 0; i < MAX_PARALLEL_TAKS * 2; i++)
            {
                taskExecutor.Add(new Task(() => Task.Delay(100).Wait()));
            }
            taskExecutor.Count.Should().Be(MAX_PARALLEL_TAKS * 2);
        }

        [Fact]
        public void GivenTaskExecutorWhenDisposeShouldWaitTaskExecutorExecutions()
        {
            ThreadPool.SetMinThreads(MAX_PARALLEL_TAKS + 20, MAX_PARALLEL_TAKS + 20);
            var value = 0;
            var taskExecutor = new TaskExecutor(MAX_PARALLEL_TAKS);
            for (var i = 0; i < MAX_PARALLEL_TAKS * 2; i++)
            {
                taskExecutor.Add(new Task(() =>
                {
                    Interlocked.Increment(ref value);
                    Task.Delay(100).Wait();
                }));
            }
            taskExecutor.Dispose();
            value.Should().Be(MAX_PARALLEL_TAKS * 2);
        }

        [Fact]
        public void GivenTaskExecutorWhenCallDisposeTwiceShouldNotFail()
        {
            var taskExecutor = new TaskExecutor(0);
            taskExecutor.Dispose();
            taskExecutor.Invoking(t => t.Dispose()).Should().NotThrow();
        }

        [Fact]
        public void GivenTaskExecutorWhenAddWithRunningTaskShouldFail()
        {
            var taskExecutor = new TaskExecutor(10);
            var task = new Task(() => {});
            task.Start();
            
            Action action = () => taskExecutor.Add(task);
            
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GivenTaskExecutorWhenAnErrorHappensShouldLogError()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var logger = new Mock<ILogger>();
            logger.Setup(x => x.Error(It.IsAny<NotImplementedException>()))
                .Callback((Exception _) => autoResetEvent.Set())
                .Verifiable();
            
            var task = new Task(() => throw new NotImplementedException());
            var taskExecutor = new TaskExecutor(1, logger.Object);
            taskExecutor.Add(task);
            autoResetEvent.WaitOne();
            
            logger.VerifyAll();
        }
    }
}
