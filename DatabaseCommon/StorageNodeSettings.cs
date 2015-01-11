using System.Xml;

namespace Database.Common
{
    /// <summary>
    /// A class for managing and loading storage node settings.
    /// </summary>
    public class StorageNodeSettings : Settings
    {
        /// <summary>
        /// The default value indicating whether this node can become a primary storage node.
        /// </summary>
        private const bool CanBecomePrimaryDefault = true;

        /// <summary>
        /// The default connection string.
        /// </summary>
        private const string ConnectionStringDefault = "";

        /// <summary>
        /// The default location.
        /// </summary>
        private const string LocationDefault = "";

        /// <summary>
        /// The default log level.
        /// </summary>
        private const LogLevel LogLevelDefault = LogLevel.Warning;

        /// <summary>
        /// The default node name.
        /// </summary>
        private const string NodeNameDefault = "localhost";

        /// <summary>
        /// The default port.
        /// </summary>
        private const int PortDefault = 5100;

        /// <summary>
        /// The default weight.
        /// </summary>
        private const int WeightDefault = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNodeSettings"/> class.
        /// </summary>
        /// <param name="xml">The xml to load from.</param>
        public StorageNodeSettings(string xml)
            : base(xml)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageNodeSettings"/> class.
        /// </summary>
        public StorageNodeSettings()
            : base()
        {
            CanBecomePrimary = CanBecomePrimaryDefault;
            ConnectionString = ConnectionStringDefault;
            Location = LocationDefault;
            NodeName = NodeNameDefault;
            Port = PortDefault;
            Weight = WeightDefault;
            LogLevel = LogLevelDefault;
        }

        /// <summary>
        /// Gets a value indicating whether this node can become a primary storage node.
        /// </summary>
        public bool CanBecomePrimary { get; private set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        public LogLevel LogLevel { get; private set; }

        /// <summary>
        /// Gets the node name.
        /// </summary>
        public string NodeName { get; private set; }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the weight.
        /// </summary>
        /// <remarks>This determines how much this node stores in relation to other nodes.</remarks>
        public int Weight { get; private set; }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            ConnectionString = ReadString(settings, "ConnectionString", ConnectionStringDefault);
            NodeName = ReadString(settings, "NodeName", NodeNameDefault);
            Port = ReadInt32(settings, "Port", PortDefault);
            Location = ReadString(settings, "Location", LocationDefault);
            CanBecomePrimary = ReadBoolean(settings, "CanBecomePrimary", CanBecomePrimaryDefault);
            Weight = ReadInt32(settings, "Weight", WeightDefault);
            LogLevel = ReadEnum(settings, "LogLevel", LogLevelDefault);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", ConnectionString, root);
            WriteString(document, "NodeName", NodeName, root);
            WriteInt32(document, "Port", Port, root);
            WriteString(document, "Location", Location, root);
            WriteBoolean(document, "CanBecomePrimary", CanBecomePrimary, root);
            WriteInt32(document, "Weight", Weight, root);
            WriteEnum(document, "LogLevel", LogLevel, root);
        }
    }
}