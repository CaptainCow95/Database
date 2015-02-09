using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Database.Common
{
    /// <summary>
    /// Represents a node's connection information.
    /// </summary>
    public class NodeDefinition : IComparable<NodeDefinition>
    {
        /// <summary>
        /// The connection name of the node.
        /// </summary>
        private readonly string _connectionName;

        /// <summary>
        /// The hostname of the node.
        /// </summary>
        private readonly string _hostname;

        /// <summary>
        /// The port of the node.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeDefinition"/> class.
        /// </summary>
        /// <param name="hostname">The hostname of the node.</param>
        /// <param name="port">The port of the node.</param>
        public NodeDefinition(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            _connectionName = hostname + ":" + port;
        }

        /// <summary>
        /// Gets the connection name of the node.
        /// </summary>
        public string ConnectionName
        {
            get { return _connectionName; }
        }

        /// <summary>
        /// Gets the hostname of the node.
        /// </summary>
        public string Hostname
        {
            get { return _hostname; }
        }

        /// <summary>
        /// Gets the port of the node.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Parses the connection string and returns the nodes it defines.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The nodes defined in the connection string.</returns>
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

        /// <inheritdoc />
        public int CompareTo(NodeDefinition other)
        {
            if (other == null)
            {
                return 1;
            }

            return string.Compare(_connectionName, other.ConnectionName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            NodeDefinition def = obj as NodeDefinition;
            if (def == null)
            {
                return false;
            }

            return def.ConnectionName == ConnectionName;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ConnectionName.GetHashCode();
        }

        /// <summary>
        /// Determines whether this node definition is the current node.
        /// </summary>
        /// <param name="port">The current nodes port.</param>
        /// <returns>Returns true if this node definition is the current node, otherwise false.</returns>
        public bool IsSelf(int port)
        {
            if (port != _port)
            {
                return false;
            }

            try
            {
                IPAddress[] hostAddresses = Dns.GetHostAddresses(_hostname);
                IPAddress[] localAddresses = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (var hostAddress in hostAddresses)
                {
                    if (IPAddress.IsLoopback(hostAddress))
                    {
                        return true;
                    }

                    if (localAddresses.Contains(hostAddress))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Something probably went wrong with the networking to figure out the hostnames, just return false.
                Logger.Log("Could not get the host addresses of the host or the local machine.", LogLevel.Error);
            }

            return false;
        }
    }
}