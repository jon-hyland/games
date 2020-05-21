using Common.Standard.Logging;
using System;
using System.IO;
using System.Text;

namespace GameServer.Logging
{
    /// <summary>
    /// Game logger, writes to file (in debug mode only).
    /// </summary>
    public sealed class Logger : ILogger, IDisposable
    {
        //private
        private readonly StreamWriter _writer;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Logger(string file)
        {
            _writer = new StreamWriter(path: file, append: true, Encoding.UTF8);
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        public void Dispose()
        {
            _writer.Dispose();
        }

        /// <summary>
        /// Writes line to console and file.
        /// </summary>
        public void Write(string message)
        {
            try
            {
                lock (_writer)
                {
                    Console.WriteLine($"{Clean(message)}");
                    _writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\t{Clean(message)}");
                }
                _writer.Flush();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Writes error to console and file.
        /// </summary>
        public void Error(Exception ex)
        {
            try
            {
                lock (_writer)
                {
                    Console.WriteLine($"{Clean(ex?.ToString())}");
                    _writer.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\tError: {Clean(ex?.ToString())}");
                }
                _writer.Flush();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Strips breaking characters.
        /// </summary>
        private static string Clean(string value)
        {
            if (value == null)
                return String.Empty;
            return value
                .Replace("\r\n", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
        }
    }
}
