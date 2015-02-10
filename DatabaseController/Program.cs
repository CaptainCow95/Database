using Database.Common;
using Database.Common.DataOperation;
using System;
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
        /// <param name="json">Whether to create a webpage or just plain JSON.</param>
        /// <returns>The html of the main page.</returns>
        private static string CreateMainPage(bool json)
        {
            try
            {
                if (_node != null)
                {
                    if (json)
                    {
                        var list = _node.GetConnectedNodes().OrderBy(e => e.Item1.ConnectionName);
                        Document nodes = new Document();
                        nodes["controllers"] = new DocumentEntry("controller", DocumentEntryType.Array, list.Where(e => e.Item2 == NodeType.Controller).Select(e => new DocumentEntry(string.Empty, DocumentEntryType.String, e.Item1.ConnectionName)).ToList());
                        nodes["storage"] = new DocumentEntry("storage", DocumentEntryType.Array, list.Where(e => e.Item2 == NodeType.Storage).Select(e => new DocumentEntry(string.Empty, DocumentEntryType.String, e.Item1.ConnectionName)).ToList());
                        nodes["query"] = new DocumentEntry("query", DocumentEntryType.Array, list.Where(e => e.Item2 == NodeType.Query).Select(e => new DocumentEntry(string.Empty, DocumentEntryType.String, e.Item1.ConnectionName)).ToList());
                        nodes["console"] = new DocumentEntry("console", DocumentEntryType.Array, list.Where(e => e.Item2 == NodeType.Console).Select(e => new DocumentEntry(string.Empty, DocumentEntryType.String, e.Item1.ConnectionName)).ToList());
                        nodes["api"] = new DocumentEntry("api", DocumentEntryType.Array, list.Where(e => e.Item2 == NodeType.Api).Select(e => new DocumentEntry(string.Empty, DocumentEntryType.String, e.Item1.ConnectionName)).ToList());

                        Document chunks = new Document();
                        var chunkList = _node.GetChunkList().ToList();
                        for (int i = 0; i < chunkList.Count; ++i)
                        {
                            Document chunk = new Document();
                            chunk["start"] = new DocumentEntry("start", DocumentEntryType.String, chunkList[i].Start.ToString());
                            chunk["end"] = new DocumentEntry("end", DocumentEntryType.String, chunkList[i].End.ToString());
                            chunk["node"] = new DocumentEntry("node", DocumentEntryType.String, chunkList[i].Node.ConnectionName);
                            chunks[i.ToString()] = new DocumentEntry(i.ToString(), DocumentEntryType.Document, chunk);
                        }

                        chunks["count"] = new DocumentEntry("count", DocumentEntryType.Integer, chunkList.Count);

                        Document document = new Document();
                        document["nodes"] = new DocumentEntry("nodes", DocumentEntryType.Document, nodes);
                        document["chunks"] = new DocumentEntry("chunks", DocumentEntryType.Document, chunks);
                        return document.ToJson();
                    }
                    else
                    {
                        var list = _node.GetConnectedNodes().OrderBy(e => e.Item1.ConnectionName);

                        StringBuilder page = new StringBuilder();
                        page.Append("<html><body><b>Controller:</b><br /><ul>");
                        foreach (
                            var item in
                                list.Where(e => e.Item2 == NodeType.Controller)
                                    .Select(e => e.Item1.ConnectionName))
                        {
                            page.Append("<li>" + item);
                            if (Equals(item, _node.Primary.ConnectionName))
                            {
                                page.Append(" <b>(Primary)</b>");
                            }

                            page.Append("</li>");
                        }

                        var chunks = _node.GetChunkList();
                        page.Append("</ul><b>Storage:</b><br /><ul>");
                        list.Where(e => e.Item2 == NodeType.Storage)
                            .Select(e => e.Item1.ConnectionName + " (" + chunks.Count(f => Equals(f.Node, e.Item1)) + ")")
                            .ToList()
                            .ForEach(e => page.Append("<li>" + e + "</li>"));
                        page.Append("</ul><b>Query:</b><br /><ul>");
                        list.Where(e => e.Item2 == NodeType.Query)
                            .Select(e => e.Item1.ConnectionName)
                            .ToList()
                            .ForEach(e => page.Append("<li>" + e + "</li>"));
                        if (list.Any(e => e.Item2 == NodeType.Console))
                        {
                            page.Append("</ul><b>Console:</b><br /><ul>");
                            list.Where(e => e.Item2 == NodeType.Console)
                                .Select(e => e.Item1.ConnectionName)
                                .ToList()
                                .ForEach(e => page.Append("<li>" + e + "</li>"));
                        }

                        if (list.Any(e => e.Item2 == NodeType.Api))
                        {
                            page.Append("</ul><b>API:</b><br /><ul>");
                            list.Where(e => e.Item2 == NodeType.Api)
                                .Select(e => e.Item1.ConnectionName)
                                .ToList()
                                .ForEach(e => page.Append("<li>" + e + "</li>"));
                        }

                        page.Append("</ul>");

                        page.Append("<br/><b>Chunks</b><br/>");

                        foreach (var item in chunks.OrderBy(e => e.Start))
                        {
                            page.Append(item.Start);
                            page.Append(" - ");
                            page.Append(item.End);
                            page.Append(" located on ");
                            page.Append(item.Node.ConnectionName);
                            page.Append("<br/>");
                        }

                        page.Append("</body></html>");

                        return page.ToString();
                    }
                }
            }
            catch
            {
                // Just continue on to display the general error message.
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

            Logger.Init(string.Empty, "controller", settings.LogLevel, true);

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
                    return CreateMainPage(queryString["json"] == "1");

                default:
                    return "<html><body>Unknown Page</body></html>";
            }
        }
    }
}