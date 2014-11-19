using Logging;
using System.Collections.Specialized;
using WebServer;

namespace DatabaseMaster
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Logger.Init(string.Empty, "master");
            WebInterface.Start(12345, WebInterfaceRequestReceived);

            // Rest of code goes here.

            WebInterface.Stop();
        }

        private static string WebInterfaceRequestReceived(string page, NameValueCollection queryString)
        {
            switch (page)
            {
                case "":
                    return "<html><body>Main Page\n" + queryString.ToString() + "</body></html>";

                case "status":
                    return "<html><body>Status Page\n" + queryString.ToString() + "</body></html>";

                default:
                    return "<html><body>Unknown Page</body></html>";
            }
        }
    }
}