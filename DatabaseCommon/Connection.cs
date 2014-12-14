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
        /// <param name="lastActiveTime">The last time the connection was active.</param>
        /// <param name="status">The current status of the connection.</param>
        public Connection(TcpClient client, DateTime lastActiveTime, ConnectionStatus status)
        {
            Client = client;
            LastActiveTime = lastActiveTime;
            Status = status;
        }

        /// <summary>
        /// Gets the <see cref="TcpClient"/> that represents the node.
        /// </summary>
        public TcpClient Client { get; private set; }

        /// <summary>
        /// Gets or sets the last time the connection was active.
        /// </summary>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Gets or sets the current status of the connection.
        /// </summary>
        public ConnectionStatus Status { get; set; }
    }
}