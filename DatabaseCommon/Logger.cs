using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Database.Common
{
    /// <summary>
    /// A class that handles logging.
    /// </summary>
    public static class Logger
    {
        private static LogLevel _logLevel;

        /// <summary>
        /// The location to write the log file to.
        /// </summary>
        private static string _logLocation;

        /// <summary>
        /// The name of the log file minus the extension.
        /// </summary>
        private static string _logPrefix;

        private static Thread _logThread;
        private static Queue<Tuple<string, LogLevel, DateTime>> _messages = new Queue<Tuple<string, LogLevel, DateTime>>();
        private static bool _running = true;

        /// <summary>
        /// Initializes the logging class.
        /// </summary>
        /// <param name="logLocation">The location to write the log file to.</param>
        /// <param name="logPrefix">The name of the log file minus the extension.</param>
        public static void Init(string logLocation, string logPrefix, LogLevel logLevel)
        {
            _logLocation = logLocation;
            _logPrefix = logPrefix;
            _logLevel = logLevel;

            _logThread = new Thread(LogThreadRun);
            _logThread.Start();
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message, LogLevel logLevel)
        {
            lock (_messages)
            {
                _messages.Enqueue(new Tuple<string, LogLevel, DateTime>(message, logLevel, DateTime.UtcNow));
            }
        }

        public static void Shutdown()
        {
            _running = false;
        }

        private static void LogThreadRun()
        {
            while (_running)
            {
                lock (_messages)
                {
                    StringBuilder text = new StringBuilder();
                    while (_messages.Count > 0)
                    {
                        var item = _messages.Dequeue();
                        if (item.Item2 <= _logLevel)
                        {
                            text.Append("[");
                            text.Append(item.Item3.ToShortDateString());
                            text.Append(" ");
                            text.Append(item.Item3.ToLongTimeString());
                            text.Append("] ");
                            text.AppendLine(item.Item1);
                        }
                    }

                    File.AppendAllText(Path.Combine(_logLocation, _logPrefix + ".log"), text.ToString());
                }

                Thread.Sleep(100);
            }
        }
    }
}