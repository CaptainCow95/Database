using Database.Common;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Database.Controller
{
    public class Program
    {
        private static ControllerNode _node;

        private static string CreateMainPage()
        {
            if (_node != null)
            {
                var list = _node.GetConnectedNodes();
                var controllers = new List<NodeDefinition>();
                var storage = new List<NodeDefinition>();
                var query = new List<NodeDefinition>();

                foreach (var item in list)
                {
                    switch (item.Item2)
                    {
                        case NodeType.Controller:
                            controllers.Add(item.Item1);
                            break;

                        case NodeType.Storage:
                            storage.Add(item.Item1);
                            break;

                        case NodeType.Query:
                            query.Add(item.Item1);
                            break;
                    }
                }

                StringBuilder page = new StringBuilder();
                page.Append("<html><body><b>Controllers:</b><br /><ul>");
                controllers.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                page.Append("</ul><b>Storage:</b><br /><ul>");
                storage.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                page.Append("</ul><b>Query:</b><br /><ul>");
                query.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                page.Append("</ul></body></html>");

                return page.ToString();
            }

            return "<html><body>Node is not available at this time.</body></html>";
        }

        private static void Main()
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

            _node = new ControllerNode(settings);
            _node.Run();

            WebInterface.Stop();
        }

        private static string WebInterfaceRequestReceived(string page, NameValueCollection queryString)
        {
            switch (page)
            {
                case "":
                    return CreateMainPage();

                default:
                    return "<html><body>Unknown Page</body></html>";
            }
        }
    }
}