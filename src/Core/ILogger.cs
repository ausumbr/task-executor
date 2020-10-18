using System;

namespace TaskExecutor.Core
{
    public interface ILogger
    {
        void Error(Exception exception);
    }
}