using System;
using System.Threading.Tasks;

namespace TaskExecutor.Core
{
    public interface ITaskExecutor : IDisposable
    {
        void Execute(Action action);
        void Add(Task task);
    }
}