using Database.Common;
using System.IO;

namespace Database.Query
{
    public class Program
    {
        private static QueryNode _node;

        private static void Main(string[] args)
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