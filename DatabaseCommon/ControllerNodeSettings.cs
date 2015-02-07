using System.Xml;

namespace Database.Common
{
    /// <summary>
    /// A class for managing and loading controller node settings.
    /// </summary>
    public class ControllerNodeSettings : Settings
    {
        /// <summary>
        /// The default connection string.
        /// </summary>
        public const string ConnectionStringDefault = "";

        /// <summary>
        /// The default log level.
        /// </summary>
        public const LogLevel LogLevelDefault = LogLevel.Warning;

        /// <summary>
        /// The default maximum number of items in a chunk before they are split.
        /// </summary>
        public const int MaxChunkItemCountDefault = 1000;

        /// <summary>
        /// The default port.
        /// </summary>
        public const int PortDefault = 5000;

        /// <summary>
        /// The default number of redundant nodes per location.
        /// </summary>
        public const int RedundantNodesPerLocationDefault = 3;

        /// <summary>
        /// The default port of the web interface.
        /// </summary>
        public const int WebInterfacePortDefault = 5001;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNodeSettings"/> class.
        /// </summary>
        /// <param name="xml">The xml to load from.</param>
        public ControllerNodeSettings(string xml)
            : base(xml)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNodeSettings"/> class.
        /// </summary>
        public ControllerNodeSettings()
            : base()
        {
            ConnectionString = ConnectionStringDefault;
            MaxChunkItemCount = MaxChunkItemCountDefault;
            Port = PortDefault;
            RedundantNodesPerLocation = RedundantNodesPerLocationDefault;
            WebInterfacePort = WebInterfacePortDefault;
            LogLevel = LogLevelDefault;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        public LogLevel LogLevel { get; private set; }

        /// <summary>
        /// Gets the maximum number of items in a chunk before they are split.
        /// </summary>
        public int MaxChunkItemCount { get; private set; }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets the number of redundant nodes per location.
        /// </summary>
        public int RedundantNodesPerLocation { get; private set; }

        /// <summary>
        /// Gets the port of the web interface.
        /// </summary>
        public int WebInterfacePort { get; private set; }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            ConnectionString = ReadString(settings, "ConnectionString", ConnectionStringDefault);
            Port = ReadInt32(settings, "Port", PortDefault);
            WebInterfacePort = ReadInt32(settings, "WebInterfacePort", WebInterfacePortDefault);
            MaxChunkItemCount = ReadInt32(settings, "MaxChunkItemCount", MaxChunkItemCountDefault);
            RedundantNodesPerLocation = ReadInt32(settings, "RedundantNodesPerLocation", RedundantNodesPerLocationDefault);
            LogLevel = ReadEnum(settings, "LogLevel", LogLevelDefault);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", ConnectionString, root);
            WriteInt32(document, "Port", Port, root);
            WriteInt32(document, "WebInterfacePort", WebInterfacePort, root);
            WriteInt32(document, "MaxChunkItemCount", MaxChunkItemCount, root);
            WriteInt32(document, "RedundantNodesPerLocation", RedundantNodesPerLocation, root);
            WriteEnum(document, "LogLevel", LogLevel, root);
        }
    }
}