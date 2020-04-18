using Common.Error;
using System;

namespace Bricker.Error
{
    public static class ErrorHandler
    {
        private static readonly ErrorHandlerInstance _instance;
        public static IErrorHandler Instance => _instance;

        static ErrorHandler()
        {
            _instance = new ErrorHandlerInstance();
        }

        public static void LogError(Exception ex)
        {
        }
    }

    public class ErrorHandlerInstance : IErrorHandler
    {
        public void LogError(Exception ex)
        {
            ErrorHandler.LogError(ex);
        }
    }
}
