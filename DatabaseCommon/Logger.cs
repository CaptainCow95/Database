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
        /// <summary>
        /// A queue of the log messages to be written.
        /// </summary>
        private static readonly Queue<Tuple<string, LogLevel, DateTime>> Messages = new Queue<Tuple<string, LogLevel, DateTime>>();

        /// <summary>
        /// A value indicating whether the logger is actually logging.
        /// </summary>
        private static bool _logging = true;

        /// <summary>
        /// The level at which messages will be logged.
        /// </summary>
        private static LogLevel _logLevel;

        /// <summary>
        /// The location to write the log file to.
        /// </summary>
        private static string _logLocation;

        /// <summary>
        /// The name of the log file minus the extension.
        /// </summary>
        private static string _logPrefix;

        /// <summary>
        /// The thread writing the messages to the log files.
        /// </summary>
        private static Thread _logThread;

        /// <summary>
        /// A value indicating whether the logging system is running.
        /// </summary>
        private static bool _running = true;

        /// <summary>
        /// Disables the logging mechanism.
        /// </summary>
        public static void Disable()
        {
            lock (Messages)
            {
                Messages.Clear();
                _logging = false;
            }
        }

        /// <summary>
        /// Initializes the logging class.
        /// </summary>
        /// <param name="logLocation">The location to write the log file to.</param>
        /// <param name="logPrefix">The name of the log file minus the extension.</param>
        /// <param name="logLevel">The level at which messages will be logged.</param>
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
        /// <param name="logLevel">The level at which messages will be logged.</param>
        public static void Log(string message, LogLevel logLevel)
        {
            lock (Messages)
            {
                if (_logging)
                {
                    Messages.Enqueue(new Tuple<string, LogLevel, DateTime>(message, logLevel, DateTime.UtcNow));
                }
            }
        }

        /// <summary>
        /// Shuts down the logging system.
        /// </summary>
        public static void Shutdown()
        {
            _running = false;
        }

        /// <summary>
        /// Flushes the messages to the log file.
        /// </summary>
        private static void FlushMessages()
        {
            StringBuilder text = new StringBuilder();
            lock (Messages)
            {
                while (Messages.Count > 0)
                {
                    var item = Messages.Dequeue();
                    if (item.Item2 <= _logLevel)
                    {
                        text.AppendFormat("[{0} {1} {2}] {3}\n", item.Item3.ToShortDateString(), item.Item3.ToLongTimeString(), Enum.GetName(typeof(LogLevel), item.Item2), item.Item1);
                    }
                }
            }

            File.AppendAllText(Path.Combine(_logLocation, _logPrefix + ".log"), text.ToString());
        }

        /// <summary>
        /// The run function for the logging thread.
        /// </summary>
        private static void LogThreadRun()
        {
            while (_running)
            {
                FlushMessages();

                Thread.Sleep(100);
            }

            FlushMessages();
        }
    }
}