using Database.Common;
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
        /// The main method of the program.
        /// </summary>
        private static void Main()
        {
            Logger.Init(string.Empty, "Query");

            QueryNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new QueryNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.");
                settings = new QueryNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            _node = new QueryNode(settings);
            _node.Run();
        }
    }
}