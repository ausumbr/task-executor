using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaskExecutor.Core
{
    public class TaskExecutor : IEnumerable<Task>, IDisposable
    {
        const int DISPOSE_WAIT_IN_SECONDS = 10;
        private bool _disposed;
        private readonly int _capacity;
        private ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        private int _isRunningCount;
        

        public TaskExecutor(int capacity)
        {
            _capacity = capacity;
        }

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
                Interlocked.Decrement(ref _isRunningCount);
                if (!_tasks.TryDequeue(out var dequeuedTask)) return;
                
                StartTask(dequeuedTask);
            });

            if (_isRunningCount >= _capacity)
            {
                _tasks.Enqueue(task);
                return;
            }
            
            StartTask(task);
        }

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
                _tasks = null;
            }

            _disposed = true;
        }

        public IEnumerator<Task> GetEnumerator()
        {
            return _tasks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void StartTask(Task task)
        {
            task.Start();
            Interlocked.Increment(ref _isRunningCount);
        }

        private void ExecuteAllTasks()
        {
            var tasks = new List<Task>(_tasks.Count);
            while (!_tasks.IsEmpty)
            {
                if (_tasks.TryDequeue(out var t))
                {
                    t.Start();
                    tasks.Add(t);
                }
            }

            Task.WaitAny(
                Task.WhenAll(tasks),
                Task.Delay(TimeSpan.FromSeconds(DISPOSE_WAIT_IN_SECONDS)));
        }
    }
}
