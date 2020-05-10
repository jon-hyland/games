using Common.Standard.Error;
using Common.Standard.Logging;
using System;

namespace GameServer.Error
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
            Log.Error(ex);
        }

        private class ErrorHandlerInstance : IErrorHandler
        {
            public void LogError(Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }
    }
}
