using Database.Common;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Database.Master
{
    public class Program
    {
        private static void Main(string[] args)
        {
            int port = 12345;
            OptionSet optionSet = new OptionSet()
            {
                {"p|port=", "The port the program should run on.", (int e) => port = e}
            };

            List<string> extras;
            try
            {
                extras = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                extras = new List<string>();
                Console.WriteLine(e.Message);
            }

            if (extras.Count > 0)
            {
                Console.Write("Unknown command line options: ");
                bool first = true;
                foreach (var item in extras)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        Console.Write(", ");
                    }

                    Console.Write(item);
                }
            }

            Logger.Init(string.Empty, "master");
            MasterNode.Start(port);
            WebInterface.Start(port + 1, WebInterfaceRequestReceived);

            // Rest of code goes here.

            WebInterface.Stop();
            MasterNode.Stop();
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