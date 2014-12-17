using System;
using System.IO;

namespace Database.Common
{
    /// <summary>
    /// A class that handles logging.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// An object to use when locking for file writing.
        /// </summary>
        private static readonly object LockObject = new object();

        /// <summary>
        /// The location to write the log file to.
        /// </summary>
        private static string _logLocation;

        /// <summary>
        /// The name of the log file minus the extension.
        /// </summary>
        private static string _logPrefix;

        /// <summary>
        /// Initializes the logging class.
        /// </summary>
        /// <param name="logLocation">The location to write the log file to.</param>
        /// <param name="logPrefix">The name of the log file minus the extension.</param>
        public static void Init(string logLocation, string logPrefix)
        {
            _logLocation = logLocation;
            _logPrefix = logPrefix;
        }

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Log(string message)
        {
            lock (LockObject)
            {
                DateTime now = DateTime.UtcNow;
                string data = "[" + now.ToShortDateString() + " " + now.ToShortTimeString() + "] " + message + "\n";
                File.AppendAllText(Path.Combine(_logLocation, _logPrefix + ".log"), data);
            }
        }
    }
}