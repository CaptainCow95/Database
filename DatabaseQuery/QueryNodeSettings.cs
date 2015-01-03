using Database.Common;
using System.Xml;

namespace Database.Query
{
    /// <summary>
    /// A class for managing and loading query node settings.
    /// </summary>
    public class QueryNodeSettings : Settings
    {
        /// <summary>
        /// The connection string.
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// The name of the node.
        /// </summary>
        private string _nodeName = "localhost";

        /// <summary>
        /// The port of the node.
        /// </summary>
        private int _port = 12345;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryNodeSettings"/> class.
        /// </summary>
        /// <param name="xml">The xml to load from.</param>
        public QueryNodeSettings(string xml)
            : base(xml)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryNodeSettings"/> class.
        /// </summary>
        public QueryNodeSettings()
            : base()
        {
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Gets the node name.
        /// </summary>
        public string NodeName
        {
            get { return _nodeName; }
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _nodeName = ReadString(settings, "NodeName", _nodeName);
            _port = ReadInt32(settings, "Port", _port);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteString(document, "NodeName", _nodeName, root);
            WriteInt32(document, "Port", _port, root);
        }
    }
}