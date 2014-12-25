using Database.Common;
using System.Collections.Specialized;
using System.IO;

namespace Database.Controller
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Logger.Init(string.Empty, "controller");

            ControllerNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new ControllerNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.");
                settings = new ControllerNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            WebInterface.Start(settings.WebInterfacePort, WebInterfaceRequestReceived);

            var node = new ControllerNode(settings);
            node.Run();

            WebInterface.Stop();
        }

        private static string WebInterfaceRequestReceived(string page, NameValueCollection queryString)
        {
            switch (page)
            {
                case "":
                    return "<html><body>Main Page\n" + queryString + "</body></html>";

                case "status":
                    return "<html><body>Status Page\n" + queryString + "</body></html>";

                default:
                    return "<html><body>Unknown Page</body></html>";
            }
        }
    }
}