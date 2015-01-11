using Database.Common;
using System.IO;

namespace Database.Storage
{
    /// <summary>
    /// The main storage node program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The storage node this program runs.
        /// </summary>
        private static StorageNode _node;

        /// <summary>
        /// The main method of the program.
        /// </summary>
        private static void Main()
        {
            StorageNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new StorageNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.", LogLevel.Warning);
                settings = new StorageNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            Logger.Init(string.Empty, "Storage", settings.LogLevel);

            _node = new StorageNode(settings);
            _node.Run();

            Logger.Shutdown();
        }
    }
}