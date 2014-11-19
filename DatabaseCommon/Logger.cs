using System;
using System.IO;

namespace Database.Common
{
    public static class Logger
    {
        private static readonly object LockObject = new object();
        private static string _logLocation;
        private static string _logPrefix;

        public static void Init(string logLocation, string logPrefix)
        {
            _logLocation = logLocation;
            _logPrefix = logPrefix;
        }

        public static void Log(string message)
        {
            lock (LockObject)
            {
                DateTime now = DateTime.UtcNow;
                string data = "[" + now.ToShortDateString() + " " + now.ToShortTimeString() + "] " + message;
                File.AppendAllText(Path.Combine(_logLocation, _logPrefix + ".log"), data);
            }
        }
    }
}