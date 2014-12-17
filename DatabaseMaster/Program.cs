using Database.Common;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace Database.Master
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Logger.Init(string.Empty, "master");

            MasterNodeSettings settings;
            if (File.Exists("masterconfig.xml"))
            {
                XmlDocument settingsDocument = new XmlDocument();
                settingsDocument.Load("masterconfig.xml");
                settings = new MasterNodeSettings(settingsDocument);
            }
            else
            {
                Logger.Log("\"masterconfig.xml\" not found, creating with the defaults.");
                settings = new MasterNodeSettings();
            }

            WebInterface.Start(settings.WebInterfacePort, WebInterfaceRequestReceived);

            var node = new MasterNode(settings);
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