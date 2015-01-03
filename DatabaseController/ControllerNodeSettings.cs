using Database.Common;
using System.Xml;

namespace Database.Controller
{
    /// <summary>
    /// A class for managing and loading controller node settings.
    /// </summary>
    public class ControllerNodeSettings : Settings
    {
        /// <summary>
        /// The connection string.
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// The maximum number of items in a chunk before they are split.
        /// </summary>
        private int _maxChunkItemCount = 1000;

        /// <summary>
        /// The maximum chunk size before they are split.
        /// </summary>
        /// <remarks>Maximum size defaults to 64kb.</remarks>
        private int _maxChunkSize = 64 * 1024;

        /// <summary>
        /// The port of the node.
        /// </summary>
        private int _port = 12345;

        /// <summary>
        /// The number of redundant nodes per location.
        /// </summary>
        private int _redundantNodesPerLocation = 3;

        /// <summary>
        /// The port of the web interface.
        /// </summary>
        private int _webInterfacePort = 12346;

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
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Gets the maximum number of items in a chunk before they are split.
        /// </summary>
        public int MaxChunkItemCount
        {
            get { return _maxChunkItemCount; }
        }

        /// <summary>
        /// Gets the maximum chunk size before they are split.
        /// </summary>
        public int MaxChunkSize
        {
            get { return _maxChunkSize; }
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// Gets the number of redundant nodes per location.
        /// </summary>
        public int RedundantNodesPerLocation
        {
            get { return _redundantNodesPerLocation; }
        }

        /// <summary>
        /// Gets the port of the web interface.
        /// </summary>
        public int WebInterfacePort
        {
            get { return _webInterfacePort; }
        }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _port = ReadInt32(settings, "Port", _port);
            _webInterfacePort = ReadInt32(settings, "WebInterfacePort", _webInterfacePort);
            _maxChunkSize = ReadInt32(settings, "MaxChunkSize", _maxChunkSize);
            _maxChunkItemCount = ReadInt32(settings, "MaxChunkItemCount", _maxChunkItemCount);
            _redundantNodesPerLocation = ReadInt32(settings, "RedundantNodesPerLocation", _redundantNodesPerLocation);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteInt32(document, "Port", _port, root);
            WriteInt32(document, "WebInterfacePort", _webInterfacePort, root);
            WriteInt32(document, "MaxChunkSize", _maxChunkSize, root);
            WriteInt32(document, "MaxChunkItemCount", _maxChunkItemCount, root);
            WriteInt32(document, "RedundantNodesPerLocation", _redundantNodesPerLocation, root);
        }
    }
}