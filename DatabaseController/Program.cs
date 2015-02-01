using Database.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace Database.Controller
{
    /// <summary>
    /// The main controller node program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The controller node this program runs.
        /// </summary>
        private static ControllerNode _node;

        /// <summary>
        /// Creates the main page.
        /// </summary>
        /// <returns>The html of the main page.</returns>
        private static string CreateMainPage()
        {
            if (_node != null)
            {
                var list = _node.GetConnectedNodes();
                var controllers = new List<NodeDefinition>();
                var storage = new List<NodeDefinition>();
                var query = new List<NodeDefinition>();
                var console = new List<NodeDefinition>();
                var api = new List<NodeDefinition>();

                var self = _node.Self;
                if (_node.Self != null)
                {
                    controllers.Add(self);
                }

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

                        case NodeType.Console:
                            console.Add(item.Item1);
                            break;

                        case NodeType.Api:
                            api.Add(item.Item1);
                            break;
                    }
                }

                controllers = controllers.OrderBy(e => e.ConnectionName).ToList();
                storage = storage.OrderBy(e => e.ConnectionName).ToList();
                query = query.OrderBy(e => e.ConnectionName).ToList();
                console = console.OrderBy(e => e.ConnectionName).ToList();
                api = console.OrderBy(e => e.ConnectionName).ToList();

                StringBuilder page = new StringBuilder();
                page.Append("<html><body><b>Controllers:</b><br /><ul>");
                foreach (var controller in controllers)
                {
                    page.Append("<li>" + controller.ConnectionName);
                    if (Equals(controller, _node.Primary))
                    {
                        page.Append(" <b>(PRIMARY)</b>");
                    }

                    page.Append("</li>");
                }

                page.Append("</ul><b>Storage:</b><br /><ul>");
                storage.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                page.Append("</ul><b>Query:</b><br /><ul>");
                query.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                if (console.Count > 0)
                {
                    page.Append("</ul><b>Console:</b><br /><ul>");
                    console.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                }

                if (api.Count > 0)
                {
                    page.Append("</ul><b>API:</b><br /><ul>");
                    api.ForEach(e => page.Append("<li>" + e.ConnectionName + "</li>"));
                }

                page.Append("</ul></body></html>");

                return page.ToString();
            }

            return "<html><body>Node is not available at this time.</body></html>";
        }

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

            ControllerNodeSettings settings;
            if (File.Exists("config.xml"))
            {
                settings = new ControllerNodeSettings(File.ReadAllText("config.xml"));
            }
            else
            {
                Logger.Log("\"config.xml\" not found, creating with the defaults.", LogLevel.Warning);
                settings = new ControllerNodeSettings();
                File.WriteAllText("config.xml", settings.ToString());
            }

            Logger.Init(string.Empty, "controller", settings.LogLevel);

            WebInterface.Start(settings.WebInterfacePort, WebInterfaceRequestReceived);

            _node = new ControllerNode(settings);
            _node.Run();

            WebInterface.Stop();

            Logger.Shutdown();
        }

        /// <summary>
        /// Called when a request is made to the web interface.
        /// </summary>
        /// <param name="page">The page that was requested.</param>
        /// <param name="queryString">The query string of the request.</param>
        /// <returns>The webpage that was requested.</returns>
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