using System;

namespace Common.Error
{
    public interface IErrorHandler
    {
        void LogError(Exception ex);
    }
}
