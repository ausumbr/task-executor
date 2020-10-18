using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace TaskExecutor.Core
{
    public class TaskExecutor : ITaskExecutor
    {
        private const int DISPOSE_WAIT_IN_SECONDS = 10;
        private bool _disposed;
        private readonly int _capacity;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<Task> _waitingTasks = new ConcurrentQueue<Task>();
        private int _isRunning;

        public TaskExecutor(int capacity, ILogger logger = null)
        {
            _capacity = capacity;
            _logger = logger;
        }
        
        public int Count => _isRunning + _waitingTasks.Count;

        public void Execute(Action action)
        {
            Add(new Task(action));
        }

        public void Add(Task task)
        {
            if (task.Status != TaskStatus.Created)
            {
                throw new InvalidOperationException("It's not possible to control a started task.");
            }
            
            task.ContinueWith(t =>
            {
                Interlocked.Decrement(ref _isRunning);
                if (t.IsFaulted) _logger?.Error(t.Exception?.InnerException);
                
                if (!_waitingTasks.TryDequeue(out var dequeuedTask)) return;
                
                StartTask(dequeuedTask);
            });

            if (_isRunning >= _capacity)
            {
                _waitingTasks.Enqueue(task);
                return;
            }
            
            StartTask(task);
        }

        [ExcludeFromCodeCoverage]
        ~TaskExecutor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                ExecuteAllTasks();
            }

            _disposed = true;
        }

        private void StartTask(Task task)
        {
            Interlocked.Increment(ref _isRunning);
            task.Start();
        }

        private void ExecuteAllTasks()
        {
            var tasks = new List<Task>(_waitingTasks.Count);
            while (_waitingTasks.TryDequeue(out var t))
            {
                t.Start();
                tasks.Add(t);
            }

            Task.WaitAny(
                Task.WhenAll(tasks),
                Task.Delay(TimeSpan.FromSeconds(DISPOSE_WAIT_IN_SECONDS)));
        }
    }
}
