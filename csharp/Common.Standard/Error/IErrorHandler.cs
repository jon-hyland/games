using System;

namespace Common.Standard.Error
{
    public interface IErrorHandler
    {
        void LogError(Exception ex);
    }
}
