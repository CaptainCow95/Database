using System;
using System.Collections.Generic;
using System.Net;

namespace Database.Common
{
    public class NodeDefinition
    {
        private readonly string _connectionName;
        private readonly string _hostname;
        private readonly int _port;

        public NodeDefinition(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            _connectionName = hostname + ":" + port;
        }

        public string ConnectionName { get { return _connectionName; } }

        public string Hostname { get { return _hostname; } }

        public int Port { get { return _port; } }

        public static List<NodeDefinition> ParseConnectionString(string connectionString)
        {
            List<NodeDefinition> definitions = new List<NodeDefinition>();
            foreach (var node in connectionString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] nodeParts = node.Split(':');
                if (nodeParts.Length != 2)
                {
                    throw new Exception("Malformed connection string.");
                }

                definitions.Add(new NodeDefinition(nodeParts[0], int.Parse(nodeParts[1])));
            }

            return definitions;
        }

        public bool IsSelf(int port)
        {
            if (port != _port)
            {
                return false;
            }

            try
            {
                IPAddress[] hostIPs = Dns.GetHostAddresses(_hostname);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (var hostIP in hostIPs)
                {
                    if (IPAddress.IsLoopback(hostIP))
                    {
                        return true;
                    }

                    foreach (var localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}