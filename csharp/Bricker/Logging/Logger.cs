﻿using Common.Standard.Logging;
using Common.Windows.Rendering;
using System;
using System.IO;
using System.Text;

namespace Bricker.Logging
{
    /// <summary>
    /// Game logger, writes to file (in debug mode only).
    /// </summary>
    public sealed class Logger : ILogger, IDisposable
    {
        //private
        private readonly string _file;
        private readonly StreamWriter _writer;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Logger(string file)
        {
            _file = file;
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
        /// Writes line to file.
        /// </summary>
        public void Write(LogLevel level, string header, string message)
        {
            try
            {
                if (!RenderProps.Debug)
                    return;
                string line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\t{Clean(level.ToString())}\t{Clean(header)}\t{Clean(message)}";
                lock (_writer)
                {
                    _writer.WriteLine(line);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Writes error to file.
        /// </summary>
        public void Error(Exception ex)
        {
            try
            {
                if (!RenderProps.Debug)
                    return;
                string line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\t{LogLevel.Error}\t{"Error"}\t{Clean(ex?.ToString())}";
                lock (_writer)
                {
                    _writer.WriteLine(line);
                }
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
