using System.Xml;

namespace Database.Common
{
    /// <summary>
    /// A class for managing and loading query node settings.
    /// </summary>
    public class QueryNodeSettings : Settings
    {
        /// <summary>
        /// The default connection string.
        /// </summary>
        private const string ConnectionStringDefault = "";

        /// <summary>
        /// The default node name.
        /// </summary>
        private const string NodeNameDefault = "localhost";

        /// <summary>
        /// The default port.
        /// </summary>
        private const int PortDefault = 5200;

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
            ConnectionString = ConnectionStringDefault;
            NodeName = NodeNameDefault;
            Port = PortDefault;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the node name.
        /// </summary>
        public string NodeName { get; private set; }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; private set; }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            ConnectionString = ReadString(settings, "ConnectionString", ConnectionStringDefault);
            NodeName = ReadString(settings, "NodeName", NodeNameDefault);
            Port = ReadInt32(settings, "Port", PortDefault);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", ConnectionString, root);
            WriteString(document, "NodeName", NodeName, root);
            WriteInt32(document, "Port", Port, root);
        }
    }
}