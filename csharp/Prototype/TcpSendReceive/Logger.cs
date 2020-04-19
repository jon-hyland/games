using Common.Logging;
using System;

namespace TcpSendReceive
{
    public class Logger : ILogger
    {
        public void Write(LogLevel level, string header, string message)
        {
            Console.WriteLine($"{level}\t{header}\t{message}");
        }
    }
}
