using Common.Error;
using System;

namespace TcpSendReceive
{
    public class ErrorHandler : IErrorHandler
    {
        public void LogError(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
