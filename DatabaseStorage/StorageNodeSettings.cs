using Database.Common;
using System.Xml;

namespace Database.Storage
{
    /// <summary>
    /// A class for managing and loading storage node settings.
    /// </summary>
    public class StorageNodeSettings : Settings
    {
        /// <summary>
        /// A value indicating whether this node can become a primary storage node.
        /// </summary>
        private bool _canBecomePrimary = true;

        /// <summary>
        /// The connection string.
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// The location.
        /// </summary>
        private string _location = string.Empty;

        /// <summary>
        /// The node name.
        /// </summary>
        private string _nodeName = "localhost";

        /// <summary>
        /// The port of the node.
        /// </summary>
        private int _port = 12345;

        /// <summary>
        /// The weight.
        /// </summary>
        private int _weight = 1;

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
        }

        /// <summary>
        /// Gets a value indicating whether this node can become a primary storage node.
        /// </summary>
        public bool CanBecomePrimary
        {
            get { return _canBecomePrimary; }
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public string Location
        {
            get { return _location; }
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

        /// <summary>
        /// Gets the weight.
        /// </summary>
        /// <remarks>This determines how much this node stores in relation to other nodes.</remarks>
        public int Weight
        {
            get { return _weight; }
        }

        /// <inheritdoc />
        protected override void Load(XmlNode settings)
        {
            _connectionString = ReadString(settings, "ConnectionString", _connectionString);
            _nodeName = ReadString(settings, "NodeName", _nodeName);
            _port = ReadInt32(settings, "Port", _port);
            _location = ReadString(settings, "Location", _location);
            _canBecomePrimary = ReadBoolean(settings, "CanBecomePrimary", _canBecomePrimary);
            _weight = ReadInt32(settings, "Weight", _weight);
        }

        /// <inheritdoc />
        protected override void Save(XmlDocument document, XmlNode root)
        {
            WriteString(document, "ConnectionString", _connectionString, root);
            WriteString(document, "NodeName", _nodeName, root);
            WriteInt32(document, "Port", _port, root);
            WriteString(document, "Location", _location, root);
            WriteBoolean(document, "CanBecomePrimary", _canBecomePrimary, root);
            WriteInt32(document, "Weight", _weight, root);
        }
    }
}