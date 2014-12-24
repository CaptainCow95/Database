using Database.Common;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace Database.Controller
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Logger.Init(string.Empty, "master");

            ControllerNodeSettings settings;
            if (File.Exists("masterconfig.xml"))
            {
                XmlDocument settingsDocument = new XmlDocument();
                settingsDocument.Load("masterconfig.xml");
                settings = new ControllerNodeSettings(settingsDocument);
            }
            else
            {
                Logger.Log("\"masterconfig.xml\" not found, creating with the defaults.");
                settings = new ControllerNodeSettings();
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