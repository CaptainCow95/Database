using System;
using System.Net.Sockets;

namespace Database.Common
{
    /// <summary>
    /// Represents a connection to another node.
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="client">The <see cref="TcpClient"/> that represents the node.</param>
        public Connection(TcpClient client)
        {
            Client = client;
            LastActiveTime = DateTime.UtcNow;
            Status = ConnectionStatus.ConfirmingConnection;
            NodeType = NodeType.Unknown;
        }

        /// <summary>
        /// Gets the <see cref="TcpClient"/> that represents the node.
        /// </summary>
        public TcpClient Client { get; private set; }

        /// <summary>
        /// Gets or sets the last time the connection was active.
        /// </summary>
        public DateTime LastActiveTime { get; private set; }

        public NodeType NodeType { get; private set; }

        /// <summary>
        /// Gets or sets the current status of the connection.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        public void ConnectionEstablished(NodeType type)
        {
            NodeType = type;
            Status = ConnectionStatus.Connected;
        }

        public void ResetLastActiveTime()
        {
            LastActiveTime = DateTime.UtcNow;
        }
    }
}