using Database.Common;
using System.IO;

namespace Database.Storage
{
    public class Program
    {
        private static StorageNode _node;

        private static void Main(string[] args)
        {
            Logger.Init(string.Empty, "Storage");

            StorageNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new StorageNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.");
                settings = new StorageNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            _node = new StorageNode(settings);
            _node.Run();
        }
    }
}