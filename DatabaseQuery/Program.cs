using Database.Common;
using System;
using System.IO;

namespace Database.Query
{
    /// <summary>
    /// The main query node program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The query node this program runs.
        /// </summary>
        private static QueryNode _node;

        /// <summary>
        /// Called when an unhandled exception occurs in order to log it.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log("Unhandled exception: " + ((Exception)e.ExceptionObject).Message + "\nStacktrace: " + ((Exception)e.ExceptionObject).StackTrace, LogLevel.Error);
        }

        /// <summary>
        /// The main method of the program.
        /// </summary>
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            QueryNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new QueryNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.", LogLevel.Warning);
                settings = new QueryNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            Logger.Init(string.Empty, "Query", settings.LogLevel, true);

            _node = new QueryNode(settings);
            _node.Run();

            Logger.Shutdown();
        }
    }
}